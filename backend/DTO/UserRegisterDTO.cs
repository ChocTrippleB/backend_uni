using System.ComponentModel.DataAnnotations;

namespace backend.DTO
{
    public class UserRegisterDTO
    {        
        [Required]
            public string Username { get; set; } = string.Empty;

        [Required]
        [EmailAddress]
        //[MandelaOnly]
        public string Email { get; set; } = string.Empty;

        [Required]
        public string Password { get; set; } = string.Empty;

        [Required]
        public string FullName { get; set; } = string.Empty;        
    }
}
