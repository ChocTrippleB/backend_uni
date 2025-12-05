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
        var userIdStr = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userIdStr) || !Guid.TryParse(userIdStr, out var userId))
            return Unauthorized("Invalid user token");


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
            user.Bio,
            user.Faculty,
            user.Course,
            user.PhoneNumber,
            user.ProfilePictureUrl,
            user.InstitutionId,
            user.CreatedAt,
            FollowersCount = user.Followers?.Count ?? 0,
            FollowingCount= user.Followed?.Count ?? 0,
            ListingsCount = user.Items?.Count ?? 0,
            Role = user.Role.Name
        });
    }

    [HttpGet("user/{id}")]
    public async Task<IActionResult> GetUserById(Guid id)
    {
        var user = await _context.Users
            .Include(u => u.Role)
            .Include(u => u.Followed)
            .Include(u => u.Followers)
            .Include(u => u.Items)
            .FirstOrDefaultAsync(u => u.Id == id);

        if (user == null) return NotFound("User not found");

        return Ok(new
        {
            user.Id,
            user.Username,
            user.FullName,
            user.Bio,
            user.Faculty,
            user.Course,
            user.ProfilePictureUrl,
            user.CreatedAt,
            FollowersCount = user.Followers?.Count ?? 0,
            FollowingCount = user.Followed?.Count ?? 0,
            ListingsCount = user.Items?.Count ?? 0
            // Note: Email and PhoneNumber are private, not exposed in public view
        });
    }

    /// <summary>
    /// Request a password reset token to be sent to the user's email
    /// </summary>
    [HttpPost("forgot-password")]
    public async Task<IActionResult> ForgotPassword([FromBody] ForgotPasswordDto dto)
    {
        var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == dto.Email);

        // Don't reveal if user exists for security (prevent email enumeration)
        if (user == null)
            return Ok("If the email exists, a password reset link has been sent.");

        // Generate password reset token
        user.PasswordResetToken = Guid.NewGuid().ToString();
        user.PasswordResetTokenExpiry = DateTime.UtcNow.AddHours(1); // Token valid for 1 hour

        await _context.SaveChangesAsync();

        // Send reset email
        var resetLink = $"http://localhost:5173/reset-password?token={user.PasswordResetToken}";
        await SendPasswordResetEmailAsync(user.Email, resetLink, user.FullName);

        return Ok("If the email exists, a password reset link has been sent.");
    }

    /// <summary>
    /// Reset password using the token sent to email
    /// </summary>
    [HttpPost("reset-password")]
    public async Task<IActionResult> ResetPassword([FromBody] ResetPasswordDto dto)
    {
        var user = await _context.Users.FirstOrDefaultAsync(u => u.PasswordResetToken == dto.Token);

        if (user == null)
            return BadRequest("Invalid or expired reset token.");

        // Check if token has expired
        if (user.PasswordResetTokenExpiry < DateTime.UtcNow)
            return BadRequest("Reset token has expired. Please request a new one.");

        // Hash new password
        CreatePasswordHash(dto.NewPassword, out byte[] hash, out byte[] salt);

        user.PasswordHash = hash;
        user.PasswordSalt = salt;
        user.PasswordResetToken = null;
        user.PasswordResetTokenExpiry = null;

        await _context.SaveChangesAsync();

        return Ok("Password has been reset successfully. You can now log in with your new password.");
    }

    /// <summary>
    /// Change password for authenticated user
    /// </summary>
    [Authorize]
    [HttpPost("change-password")]
    public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordDto dto)
    {
        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (userId == null)
            return Unauthorized();

        var user = await _context.Users.FindAsync(Guid.Parse(userId));
        if (user == null)
            return NotFound("User not found.");

        // Verify current password
        if (!VerifyPasswordHash(dto.CurrentPassword, user.PasswordHash, user.PasswordSalt))
            return BadRequest("Current password is incorrect.");

        // Validate new password
        if (dto.NewPassword.Length < 6)
            return BadRequest("New password must be at least 6 characters long.");

        if (dto.NewPassword != dto.ConfirmPassword)
            return BadRequest("New password and confirmation do not match.");

        // Hash new password
        CreatePasswordHash(dto.NewPassword, out byte[] hash, out byte[] salt);

        user.PasswordHash = hash;
        user.PasswordSalt = salt;

        await _context.SaveChangesAsync();

        return Ok("Password changed successfully.");
    }

    /// <summary>
    /// Create an admin user (protected - requires Admin role)
    /// </summary>
    [Authorize(Roles = "Admin")]
    [HttpPost("create-admin")]
    public async Task<IActionResult> CreateAdmin([FromBody] UserRegisterDTO dto)
    {
        if (await _context.Users.AnyAsync(u => u.Email == dto.Email))
            return BadRequest("Email already registered.");

        // Hash password
        CreatePasswordHash(dto.Password, out byte[] hash, out byte[] salt);

        var adminUser = new User
        {
            Username = dto.Username,
            Email = dto.Email,
            FullName = dto.FullName,
            PasswordHash = hash,
            PasswordSalt = salt,
            RoleId = 10, // Admin role
            CreatedAt = DateTime.UtcNow,
            EmailConfirmed = true, // Auto-confirm admin accounts
            EmailConfirmationToken = null
        };

        _context.Users.Add(adminUser);
        await _context.SaveChangesAsync();

        return Ok(new
        {
            message = "Admin user created successfully",
            user = new
            {
                adminUser.Id,
                adminUser.Username,
                adminUser.Email,
                adminUser.FullName,
                Role = "Admin"
            }
        });
    }

    /// <summary>
    /// Bootstrap endpoint - creates the first admin user (unprotected, run once)
    /// IMPORTANT: This should be disabled/removed in production or after first admin is created
    /// </summary>
    [HttpPost("bootstrap-admin")]
    public async Task<IActionResult> BootstrapAdmin([FromBody] UserRegisterDTO dto)
    {
        // Check if any admin already exists
        var existingAdmin = await _context.Users
            .Include(u => u.Role)
            .AnyAsync(u => u.RoleId == 10);

        if (existingAdmin)
            return BadRequest("An admin user already exists. Use the /create-admin endpoint with admin credentials.");

        if (await _context.Users.AnyAsync(u => u.Email == dto.Email))
            return BadRequest("Email already registered.");

        // Hash password
        CreatePasswordHash(dto.Password, out byte[] hash, out byte[] salt);

        var adminUser = new User
        {
            Username = dto.Username,
            Email = dto.Email,
            FullName = dto.FullName,
            PasswordHash = hash,
            PasswordSalt = salt,
            RoleId = 10, // Admin role
            CreatedAt = DateTime.UtcNow,
            EmailConfirmed = true, // Auto-confirm admin accounts
            EmailConfirmationToken = null
        };

        _context.Users.Add(adminUser);
        await _context.SaveChangesAsync();

        return Ok(new
        {
            message = "Bootstrap admin created successfully. This endpoint should now be disabled.",
            user = new
            {
                adminUser.Id,
                adminUser.Username,
                adminUser.Email,
                adminUser.FullName,
                Role = "Admin"
            }
        });
    }


    private async Task SendEmailConfirmationAsync(string toEmail, string confirmationLink)
    {
        var email = new MimeMessage();
        email.From.Add(MailboxAddress.Parse(_config["Smtp:Email"]));
        email.To.Add(MailboxAddress.Parse(toEmail));
        email.Subject = "Confirm your email - Campus Swap";

        email.Body = new TextPart(TextFormat.Html)
        {
            Text = $@"
                <!DOCTYPE html>
                <html>
                <head>
                    <meta charset='utf-8'>
                    <meta name='viewport' content='width=device-width, initial-scale=1.0'>
                </head>
                <body style='margin: 0; padding: 0; font-family: Arial, Helvetica, sans-serif; background-color: #f5f5f5;'>
                    <div style='max-width: 600px; margin: 0 auto; background-color: #ffffff;'>
                        <!-- Header -->
                        <div style='background: linear-gradient(135deg, #ce1750 0%, #111 100%); padding: 40px 20px; text-align: center;'>
                            <img src='http://localhost:5173/logo.png' alt='Campus Swap Logo' style='max-width: 120px; height: auto; margin-bottom: 20px;' />
                            <h1 style='color: #ffffff; margin: 0; font-size: 28px; font-weight: bold;'>Welcome to Campus Swap!</h1>
                        </div>

                        <!-- Content -->
                        <div style='padding: 40px 30px;'>
                            <p style='color: #333333; font-size: 16px; line-height: 1.6; margin-bottom: 20px;'>
                                Thank you for joining Campus Swap - your campus marketplace for buying and selling with fellow students!
                            </p>

                            <p style='color: #333333; font-size: 16px; line-height: 1.6; margin-bottom: 30px;'>
                                To get started, please confirm your email address by clicking the button below:
                            </p>

                            <!-- Button -->
                            <div style='text-align: center; margin: 40px 0;'>
                                <a href='{confirmationLink}'
                                   style='background-color: #ce1750; color: #ffffff; padding: 16px 40px;
                                          text-decoration: none; border-radius: 8px; font-size: 16px;
                                          font-weight: bold; display: inline-block; box-shadow: 0 4px 6px rgba(206, 23, 80, 0.3);'>
                                    Confirm Email Address
                                </a>
                            </div>

                            <p style='color: #666666; font-size: 14px; line-height: 1.6; margin-top: 30px;'>
                                If the button doesn't work, copy and paste this link into your browser:
                            </p>
                            <p style='color: #ce1750; font-size: 14px; word-break: break-all; background-color: #f9f9f9; padding: 10px; border-radius: 4px;'>
                                {confirmationLink}
                            </p>

                            <div style='margin-top: 40px; padding: 20px; background-color: #f9f9f9; border-radius: 8px; border-left: 4px solid #ce1750;'>
                                <p style='color: #333333; font-size: 14px; margin: 0; line-height: 1.6;'>
                                    <strong>What's next?</strong><br/>
                                    Once confirmed, you can start browsing items, creating listings, and connecting with other students on your campus!
                                </p>
                            </div>
                        </div>

                        <!-- Footer -->
                        <div style='background-color: #f9f9f9; padding: 30px 20px; text-align: center; border-top: 1px solid #eeeeee;'>
                            <p style='color: #999999; font-size: 12px; margin: 0 0 10px 0;'>
                                This email was sent by Campus Swap
                            </p>
                            <p style='color: #999999; font-size: 12px; margin: 0;'>
                                © 2025 Campus Swap. All rights reserved.
                            </p>
                        </div>
                    </div>
                </body>
                </html>"
        };

        using var smtp = new SmtpClient();
        await smtp.ConnectAsync("smtp.gmail.com", 587, MailKit.Security.SecureSocketOptions.StartTls);
        await smtp.AuthenticateAsync(_config["Smtp:Email"], _config["Smtp:Password"]);
        await smtp.SendAsync(email);
        await smtp.DisconnectAsync(true);
    }

    private async Task SendPasswordResetEmailAsync(string toEmail, string resetLink, string fullName)
    {
        var email = new MimeMessage();
        email.From.Add(MailboxAddress.Parse(_config["Smtp:Email"]));
        email.To.Add(MailboxAddress.Parse(toEmail));
        email.Subject = "Reset your password - Campus Swap";

        email.Body = new TextPart(TextFormat.Html)
        {
            Text = $@"
                <!DOCTYPE html>
                <html>
                <head>
                    <meta charset='utf-8'>
                    <meta name='viewport' content='width=device-width, initial-scale=1.0'>
                </head>
                <body style='margin: 0; padding: 0; font-family: Arial, Helvetica, sans-serif; background-color: #f5f5f5;'>
                    <div style='max-width: 600px; margin: 0 auto; background-color: #ffffff;'>
                        <!-- Header -->
                        <div style='background: linear-gradient(135deg, #ce1750 0%, #111 100%); padding: 40px 20px; text-align: center;'>
                            <img src='http://localhost:5173/logo.png' alt='Campus Swap Logo' style='max-width: 120px; height: auto; margin-bottom: 20px;' />
                            <h1 style='color: #ffffff; margin: 0; font-size: 28px; font-weight: bold;'>Password Reset Request</h1>
                        </div>

                        <!-- Content -->
                        <div style='padding: 40px 30px;'>
                            <p style='color: #333333; font-size: 16px; line-height: 1.6; margin-bottom: 20px;'>
                                Hi <strong>{fullName}</strong>,
                            </p>

                            <p style='color: #333333; font-size: 16px; line-height: 1.6; margin-bottom: 20px;'>
                                We received a request to reset your password for your Campus Swap account.
                            </p>

                            <p style='color: #333333; font-size: 16px; line-height: 1.6; margin-bottom: 30px;'>
                                Click the button below to create a new password:
                            </p>

                            <!-- Button -->
                            <div style='text-align: center; margin: 40px 0;'>
                                <a href='{resetLink}'
                                   style='background-color: #ce1750; color: #ffffff; padding: 16px 40px;
                                          text-decoration: none; border-radius: 8px; font-size: 16px;
                                          font-weight: bold; display: inline-block; box-shadow: 0 4px 6px rgba(206, 23, 80, 0.3);'>
                                    Reset Your Password
                                </a>
                            </div>

                            <p style='color: #666666; font-size: 14px; line-height: 1.6; margin-top: 30px;'>
                                If the button doesn't work, copy and paste this link into your browser:
                            </p>
                            <p style='color: #ce1750; font-size: 14px; word-break: break-all; background-color: #f9f9f9; padding: 10px; border-radius: 4px;'>
                                {resetLink}
                            </p>

                            <div style='margin-top: 40px; padding: 20px; background-color: #fff3cd; border-radius: 8px; border-left: 4px solid #ffc107;'>
                                <p style='color: #856404; font-size: 14px; margin: 0; line-height: 1.6;'>
                                    <strong>⚠️ Important:</strong><br/>
                                    • This link will expire in <strong>1 hour</strong><br/>
                                    • If you didn't request this reset, please ignore this email<br/>
                                    • Your password won't change unless you click the link above
                                </p>
                            </div>

                            <div style='margin-top: 30px; padding: 20px; background-color: #f9f9f9; border-radius: 8px;'>
                                <p style='color: #666666; font-size: 14px; margin: 0; line-height: 1.6;'>
                                    <strong>Security Tip:</strong> Choose a strong password with at least 6 characters. Consider using a mix of letters, numbers, and symbols.
                                </p>
                            </div>
                        </div>

                        <!-- Footer -->
                        <div style='background-color: #f9f9f9; padding: 30px 20px; text-align: center; border-top: 1px solid #eeeeee;'>
                            <p style='color: #999999; font-size: 12px; margin: 0 0 10px 0;'>
                                This email was sent by Campus Swap
                            </p>
                            <p style='color: #999999; font-size: 12px; margin: 0;'>
                                © 2025 Campus Swap. All rights reserved.
                            </p>
                        </div>
                    </div>
                </body>
                </html>"
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
