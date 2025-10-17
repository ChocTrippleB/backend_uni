using System.ComponentModel.DataAnnotations;

namespace backend.DTO
{
    public class VerifyReleaseCodeDto
    {
        [Required]
        public int OrderId { get; set; }

        [Required]
        [StringLength(6, MinimumLength = 6)]
        public string ReleaseCode { get; set; } = null!;
    }
}
