namespace backend.DTO
{
    public class ReviewResponseDto
    {
        public int Id { get; set; }
        public int OrderId { get; set; }

        // Buyer Info
        public Guid BuyerId { get; set; }
        public string BuyerName { get; set; } = string.Empty;
        public string? BuyerAvatar { get; set; }

        // Seller Info
        public Guid SellerId { get; set; }
        public string SellerName { get; set; } = string.Empty;

        // Product Info
        public int ProductId { get; set; }
        public string ProductName { get; set; } = string.Empty;
        public string? ProductImage { get; set; }

        // Ratings
        public int OverallRating { get; set; }
        public int ProductQualityRating { get; set; }
        public int CommunicationRating { get; set; }
        public int ShippingSpeedRating { get; set; }

        // Content
        public string? ReviewTitle { get; set; }
        public string? ReviewText { get; set; }

        // Metadata
        public bool IsVerifiedPurchase { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }

        // Phase 3 Features (Optional)
        public string? SellerResponse { get; set; }
        public DateTime? SellerResponseDate { get; set; }
        public int HelpfulCount { get; set; }
        public int NotHelpfulCount { get; set; }
    }
}
