using backend.Model;

namespace backend.DTO
{
    public class OrderResponseDto
    {
        public int Id { get; set; }
        public Guid BuyerId { get; set; }
        public string BuyerName { get; set; } = null!;
        public string? BuyerEmail { get; set; }
        public Guid SellerId { get; set; }
        public string SellerName { get; set; } = null!;
        public string? SellerEmail { get; set; }
        public int ProductId { get; set; }
        public string ProductName { get; set; } = null!;
        public string? ProductSlug { get; set; }  // Product slug for shareable links
        public string? ProductImage { get; set; }
        public decimal Amount { get; set; }
        public string? PaymentReference { get; set; }
        public OrderStatus Status { get; set; }
        public string? ReleaseCode { get; set; }  // Only visible to buyer
        public DateTime CreatedAt { get; set; }
        public DateTime? PaidAt { get; set; }
        public DateTime? ReleasedAt { get; set; }
        public DateTime? ExpiresAt { get; set; }
        public string? ShippingAddress { get; set; }
        public string? BuyerPhone { get; set; }
        public string? Notes { get; set; }
    }
}
