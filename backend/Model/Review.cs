using System.ComponentModel.DataAnnotations;

namespace backend.Model
{
    public class Review
    {
        public int Id { get; set; }

        // Relations
        [Required]
        public int OrderId { get; set; }
        public Order Order { get; set; } = null!;

        [Required]
        public int BuyerId { get; set; }
        public User Buyer { get; set; } = null!;

        [Required]
        public int SellerId { get; set; }
        public User Seller { get; set; } = null!;

        [Required]
        public int ProductId { get; set; }
        public Product Product { get; set; } = null!;

        // Ratings (1-5)
        [Required]
        [Range(1, 5)]
        public int OverallRating { get; set; }

        [Required]
        [Range(1, 5)]
        public int ProductQualityRating { get; set; }

        [Required]
        [Range(1, 5)]
        public int CommunicationRating { get; set; }

        [Required]
        [Range(1, 5)]
        public int ShippingSpeedRating { get; set; }

        // Content
        [MaxLength(200)]
        public string? ReviewTitle { get; set; }

        [MaxLength(2000)]
        public string? ReviewText { get; set; }

        // Metadata
        public bool IsVerifiedPurchase { get; set; } = true;

        [Required]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public DateTime? UpdatedAt { get; set; }

        // Phase 3 Features (Optional - can add later)
        [MaxLength(2000)]
        public string? SellerResponse { get; set; }

        public DateTime? SellerResponseDate { get; set; }

        public int HelpfulCount { get; set; } = 0;
        public int NotHelpfulCount { get; set; } = 0;
    }
}
