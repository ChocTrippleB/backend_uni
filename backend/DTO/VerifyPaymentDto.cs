using System.ComponentModel.DataAnnotations;

namespace backend.DTO
{
    public class VerifyPaymentDto
    {
        [Required]
        public string PaymentReference { get; set; } = null!;
    }
}
