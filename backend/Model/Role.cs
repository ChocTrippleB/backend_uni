using System.ComponentModel.DataAnnotations;

namespace backend.Model
{
    public class Role
    {
        public int Id { get; set; }

        [Required]
        [MaxLength(50)]
        public string Name { get; set; } = null!;  // e.g. "User", "Admin", "Moderator"

        [MaxLength(200)]
        public string? Description { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // Navigation property
        public ICollection<User> Users { get; set; } = new List<User>();
    }

    // Default role names as constants for easy reference
    public static class RoleNames
    {
        public const string Admin = "Admin";        // Full system access
        public const string User = "User";          // Default role (buyers & sellers)
        public const string Moderator = "Moderator"; // Future: Content moderation
        public const string Vendor = "Vendor";      // Future: Bulk sellers
    }
}
