using System.ComponentModel.DataAnnotations;

namespace backend.DTO
{
    /// <summary>
    /// DTO for updating user profile information
    /// </summary>
    public class UpdateProfileDto
    {
        /// <summary>
        /// User bio/about section (max 500 characters)
        /// </summary>
        [MaxLength(500, ErrorMessage = "Bio cannot exceed 500 characters")]
        public string? Bio { get; set; }

        /// <summary>
        /// Faculty/Department (e.g., "Engineering", "Business Sciences")
        /// </summary>
        [MaxLength(100, ErrorMessage = "Faculty cannot exceed 100 characters")]
        public string? Faculty { get; set; }

        /// <summary>
        /// Course/Major (e.g., "Computer Science", "Accounting")
        /// </summary>
        [MaxLength(100, ErrorMessage = "Course cannot exceed 100 characters")]
        public string? Course { get; set; }

        /// <summary>
        /// Phone number for notifications and order coordination
        /// </summary>
        [Phone(ErrorMessage = "Invalid phone number format")]
        public string? PhoneNumber { get; set; }
    }
}
