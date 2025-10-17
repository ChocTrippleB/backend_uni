using System.ComponentModel.DataAnnotations;

namespace backend.DTO
{
    public class CreateOrderDto
    {
        [Required]
        public int ProductId { get; set; }

        [Required]
        public decimal Amount { get; set; }

        public string? ShippingAddress { get; set; }

        public string? BuyerPhone { get; set; }

        public string? Notes { get; set; }
    }
}
