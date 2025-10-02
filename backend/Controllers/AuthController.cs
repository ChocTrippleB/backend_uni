using backend.Data;
using backend.DTO;
using backend.Model;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Net.Mail;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using MailKit.Net.Smtp;
using MimeKit;
using MimeKit.Text;
using SmtpClient = MailKit.Net.Smtp.SmtpClient;
using Microsoft.AspNetCore.Authorization;

namespace backend.Controllers;

[ApiController]
[Route("api/auth")]
public class AuthController : ControllerBase
{
    private readonly AppDbContext _context;
    private readonly IConfiguration _config;

    public AuthController(AppDbContext context, IConfiguration config)
    {
        _context = context;
        _config = config;
    }

    [HttpPost("register")]
    public async Task<IActionResult> Register([FromBody] UserRegisterDTO dto)
    {
        //if (!dto.Email.EndsWith("@mandela.ac.za", StringComparison.OrdinalIgnoreCase))
        //    return BadRequest("Only @mandela.ac.za email addresses are allowed.");

        if (await _context.Users.AnyAsync(u => u.Email == dto.Email))
            return BadRequest("Email already registered.");

        // Hash password
        CreatePasswordHash(dto.Password, out byte[] hash, out byte[] salt);

        var user = new User
        {
            Username = dto.Username,
            Email = dto.Email,
            FullName = dto.FullName,
            PasswordHash = hash,
            PasswordSalt = salt,
            RoleId = 1,
            CreatedAt = DateTime.UtcNow,

            // ✅ Add this line
            EmailConfirmationToken = Guid.NewGuid().ToString()
        };

        _context.Users.Add(user);
        await _context.SaveChangesAsync();

        var confirmationLink = $"https://localhost:7255/api/auth/confirm?token={user.EmailConfirmationToken}";
        await SendEmailConfirmationAsync(user.Email, confirmationLink);

        return Ok("User registered.");
    }

    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] UserLoginDto dto)
    {
        
        var user = await _context.Users.Include(u => u.Role).FirstOrDefaultAsync(u => u.Email == dto.Email);
        if (user == null)
            return Unauthorized("Invalid email or password.");

        if (!user.EmailConfirmed)
        {
            return Unauthorized("Please confirm your email before logging in.");
        }

        if (!VerifyPasswordHash(dto.Password, user.PasswordHash, user.PasswordSalt))
            return Unauthorized("Invalid email or password.");

        var token = CreateJwtToken(user);

        return Ok(new
        {
            token,
            user = new
            {
                user.Id,
                user.Username,
                user.FullName,
                user.Email,
                Role = user.Role.Name
            }
        });
        Console.WriteLine("Hello World");
    }

    [HttpGet("confirm")]
    public async Task<IActionResult> ConfirmEmail([FromQuery] string token)
    {
        var user = await _context.Users.FirstOrDefaultAsync(u => u.EmailConfirmationToken == token);
        if (user == null)
            return NotFound("Invalid or expired confirmation token.");

        user.EmailConfirmed = true;
        user.EmailConfirmationToken = null;
        await _context.SaveChangesAsync();

        return Redirect("http://localhost:5173/email-confirmed");
    }

    [Authorize]
    [HttpGet("me")]
    public async Task<IActionResult> GetCurrentUser()
    {
        var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");

        var user = await _context.Users
            .Include(u => u.Role)
            .Include(u => u.Followed)
            .Include(u => u.Followers)// if you have a followers table
            .Include(u => u.Items) // assuming Items is navigation property for listings
            .FirstOrDefaultAsync(u => u.Id == userId);

        if (user == null) return NotFound();

        return Ok(new
        {
            user.Id,
            user.Username,
            user.Email,
            user.FullName,
            FollowersCount = user.Followers?.Count ?? 0,
            FollowingCount= user.Followed?.Count ?? 0,
            ListingsCount = user.Items?.Count ?? 0,
            Role = user.Role.Name
        });
    }


    private async Task SendEmailConfirmationAsync(string toEmail, string confirmationLink)
    {
        var email = new MimeMessage();
        email.From.Add(MailboxAddress.Parse(_config["Smtp:Email"]));
        email.To.Add(MailboxAddress.Parse(toEmail));
        email.Subject = "Confirm your email - Student Marketplace";

        email.Body = new TextPart(TextFormat.Html)
        {
            Text = $"<h3>Welcome to Student Marketplace</h3>" +
                   $"<p>Click below to confirm your email:</p>" +
                   $"<a href='{confirmationLink}'>Confirm Email</a>"
        };

        using var smtp = new SmtpClient();
        await smtp.ConnectAsync("smtp.gmail.com", 587, MailKit.Security.SecureSocketOptions.StartTls);
        await smtp.AuthenticateAsync(_config["Smtp:Email"], _config["Smtp:Password"]);
        await smtp.SendAsync(email);
        await smtp.DisconnectAsync(true);
    }

    // 🔐 Helpers

    private void CreatePasswordHash(string password, out byte[] hash, out byte[] salt)
    {
        using var hmac = new HMACSHA512();
        salt = hmac.Key;
        hash = hmac.ComputeHash(Encoding.UTF8.GetBytes(password));
    }

    private bool VerifyPasswordHash(string password, byte[] hash, byte[] salt)
    {
        using var hmac = new HMACSHA512(salt);
        var computed = hmac.ComputeHash(Encoding.UTF8.GetBytes(password));
        return computed.SequenceEqual(hash);
    }

    private string CreateJwtToken(User user)
    {
        var claims = new List<Claim>
        {
            new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new Claim(ClaimTypes.Name, user.Username),
            new Claim(ClaimTypes.Role, user.Role.Name)
        };

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_config["Jwt:Key"]!));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha512Signature);

        var token = new JwtSecurityToken(
            issuer: _config["Jwt:Issuer"],
            audience: _config["Jwt:Audience"],
            claims: claims,
            expires: DateTime.UtcNow.AddDays(7),
            signingCredentials: creds
        );

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}
