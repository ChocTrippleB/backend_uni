namespace backend.DTO
{
    public class SellerRatingDto
    {
        public int SellerId { get; set; }
        public decimal AverageRating { get; set; }
        public int TotalReviews { get; set; }
        public int TotalSales { get; set; }

        // Star Distribution
        public int FiveStarCount { get; set; }
        public int FourStarCount { get; set; }
        public int ThreeStarCount { get; set; }
        public int TwoStarCount { get; set; }
        public int OneStarCount { get; set; }

        public DateTime LastUpdated { get; set; }
    }
}
