using System.ComponentModel.DataAnnotations;

namespace backend.Model
{
    /// <summary>
    /// Cached seller rating statistics for performance
    /// Recalculated whenever a new review is added, updated, or deleted
    /// </summary>
    public class SellerRating
    {
        [Key]
        public int SellerId { get; set; }
        public User Seller { get; set; } = null!;

        [Required]
        public decimal AverageRating { get; set; } = 0;

        [Required]
        public int TotalReviews { get; set; } = 0;

        [Required]
        public int TotalSales { get; set; } = 0;

        // Star Distribution
        public int FiveStarCount { get; set; } = 0;
        public int FourStarCount { get; set; } = 0;
        public int ThreeStarCount { get; set; } = 0;
        public int TwoStarCount { get; set; } = 0;
        public int OneStarCount { get; set; } = 0;

        [Required]
        public DateTime LastUpdated { get; set; } = DateTime.UtcNow;
    }
}
