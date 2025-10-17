using System.ComponentModel.DataAnnotations;

namespace backend.DTO
{
    public class InitializePaymentDto
    {
        [Required]
        public int OrderId { get; set; }

        [Required]
        [EmailAddress]
        public string Email { get; set; } = null!;

        [Required]
        public decimal Amount { get; set; }
    }
}
