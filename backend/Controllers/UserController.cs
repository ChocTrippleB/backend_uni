using backend.Data;
using backend.DTO;
using backend.Helpers;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using Firebase.Storage;
using Google.Apis.Auth.OAuth2;

namespace backend.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class UserController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly IConfiguration _configuration;
        private readonly GoogleCredential _googleCredential;
        private readonly string _firebaseBucket;
        private readonly long _maxFileBytes;

        private static readonly HashSet<string> AllowedMimeTypes = new(StringComparer.OrdinalIgnoreCase)
        {
            "image/jpeg", "image/png", "image/webp"
        };

        private static readonly HashSet<string> AllowedExtensions = new(StringComparer.OrdinalIgnoreCase)
        {
            ".jpg", ".jpeg", ".png", ".webp"
        };

        public UserController(AppDbContext context, IConfiguration configuration)
        {
            _context = context;
            _configuration = configuration;

            // Firebase configuration (reusing from ImageController setup)
            _firebaseBucket = _configuration["Firebase:StorageBucket"]
                ?? throw new InvalidOperationException("Firebase:StorageBucket missing in configuration");

            var serviceAccountPath = _configuration["Firebase:ServiceAccountPath"]
                ?? throw new InvalidOperationException("Firebase:ServiceAccountPath missing in configuration");

            _googleCredential = GoogleCredential.FromFile(serviceAccountPath)
                .CreateScoped("https://www.googleapis.com/auth/devstorage.full_control");

            // Max file size: 5MB for profile pictures
            _maxFileBytes = 5 * 1024 * 1024;
        }

        /// <summary>
        /// Create Firebase Storage instance with authentication
        /// </summary>
        private FirebaseStorage CreateFirebaseStorage()
        {
            return new FirebaseStorage(
                _firebaseBucket,
                new FirebaseStorageOptions
                {
                    AuthTokenAsyncFactory = async () =>
                        await _googleCredential.UnderlyingCredential.GetAccessTokenForRequestAsync(),
                    ThrowOnCancel = true
                });
        }

        /// <summary>
        /// Get current user's profile
        /// </summary>
        [HttpGet("profile")]
        public async Task<IActionResult> GetProfile()
        {
            var userId = User.GetUserId();
            if (userId == null)
                return Unauthorized(new { message = "Invalid user token" });

            var user = await _context.Users
                .Include(u => u.Role)
                .Include(u => u.Followers)
                .Include(u => u.Followed)
                .Include(u => u.Items)
                .FirstOrDefaultAsync(u => u.Id == userId);

            if (user == null)
                return NotFound(new { message = "User not found" });

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
                FollowingCount = user.Followed?.Count ?? 0,
                ListingsCount = user.Items?.Count ?? 0,
                Role = user.Role.Name
            });
        }

        /// <summary>
        /// Update current user's profile
        /// </summary>
        [HttpPut("profile")]
        public async Task<IActionResult> UpdateProfile([FromBody] UpdateProfileDto dto)
        {
            var userId = User.GetUserId();
            if (userId == null)
                return Unauthorized(new { message = "Invalid user token" });

            var user = await _context.Users.FirstOrDefaultAsync(u => u.Id == userId);

            if (user == null)
                return NotFound(new { message = "User not found" });

            // Update only the fields that are provided
            if (dto.Bio != null)
                user.Bio = dto.Bio;

            if (dto.Faculty != null)
                user.Faculty = dto.Faculty;

            if (dto.Course != null)
                user.Course = dto.Course;

            if (dto.PhoneNumber != null)
                user.PhoneNumber = dto.PhoneNumber;

            try
            {
                await _context.SaveChangesAsync();

                return Ok(new
                {
                    message = "Profile updated successfully",
                    user = new
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
                        user.CreatedAt
                    }
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Failed to update profile", error = ex.Message });
            }
        }

        /// <summary>
        /// Get user profile by ID (public view)
        /// </summary>
        [AllowAnonymous]
        [HttpGet("{id}")]
        public async Task<IActionResult> GetUserById(Guid id)
        {
            var user = await _context.Users
                .Include(u => u.Followers)
                .Include(u => u.Followed)
                .Include(u => u.Items)
                .FirstOrDefaultAsync(u => u.Id == id);

            if (user == null)
                return NotFound(new { message = "User not found" });

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
        /// Get user profile by username (public view) - for /@username URLs
        /// </summary>
        [AllowAnonymous]
        [HttpGet("@{username}")]
        public async Task<IActionResult> GetUserByUsername(string username)
        {
            var user = await _context.Users
                .Include(u => u.Followers)
                .Include(u => u.Followed)
                .Include(u => u.Items)
                .FirstOrDefaultAsync(u => u.Username == username);

            if (user == null)
                return NotFound(new { message = "User not found" });

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
        /// Upload profile picture to Firebase Storage
        /// </summary>
        [HttpPost("upload-avatar")]
        public async Task<IActionResult> UploadAvatar([FromForm] IFormFile image)
        {
            // Validate file exists
            if (image == null || image.Length == 0)
                return BadRequest(new { message = "No image file provided" });

            // Get current user ID
            var userId = User.GetUserId();
            if (userId == null)
                return Unauthorized(new { message = "Invalid user token" });

            var user = await _context.Users.FirstOrDefaultAsync(u => u.Id == userId);

            if (user == null)
                return NotFound(new { message = "User not found" });

            // Validate file size (5MB max)
            if (image.Length > _maxFileBytes)
                return BadRequest(new { message = $"File too large. Maximum size is {_maxFileBytes / (1024 * 1024)}MB" });

            // Validate file extension
            var extension = Path.GetExtension(image.FileName);
            if (!AllowedExtensions.Contains(extension))
                return BadRequest(new { message = $"Unsupported file type. Allowed: {string.Join(", ", AllowedExtensions)}" });

            // Validate MIME type
            if (!AllowedMimeTypes.Contains(image.ContentType))
                return BadRequest(new { message = $"Unsupported MIME type: {image.ContentType}" });

            try
            {
                // Create Firebase storage instance
                var storage = CreateFirebaseStorage();

                // Upload to Firebase: users/{userId}/profile{extension}
                // Using the same filename overwrites the old picture automatically
                var fileName = $"profile{extension.ToLowerInvariant()}";
                string downloadUrl;

                using (var stream = image.OpenReadStream())
                {
                    downloadUrl = await storage
                        .Child("users")
                        .Child(userId.ToString())
                        .Child(fileName)
                        .PutAsync(stream);
                }

                // Delete old profile picture from Firebase if it exists and is different
                if (!string.IsNullOrEmpty(user.ProfilePictureUrl) &&
                    !user.ProfilePictureUrl.Contains(fileName))
                {
                    try
                    {
                        // Extract old filename from URL and delete
                        var oldExtension = Path.GetExtension(user.ProfilePictureUrl.Split('?')[0]);
                        if (!string.IsNullOrEmpty(oldExtension))
                        {
                            var oldFileName = $"profile{oldExtension}";
                            await storage
                                .Child("users")
                                .Child(userId.ToString())
                                .Child(oldFileName)
                                .DeleteAsync();
                        }
                    }
                    catch
                    {
                        // Ignore errors when deleting old file (it might not exist)
                    }
                }

                // Update user's profile picture URL in database
                user.ProfilePictureUrl = downloadUrl;
                await _context.SaveChangesAsync();

                return Ok(new
                {
                    message = "Profile picture uploaded successfully",
                    profilePictureUrl = downloadUrl
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    message = "Failed to upload profile picture",
                    error = ex.Message
                });
            }
        }
    }
}
