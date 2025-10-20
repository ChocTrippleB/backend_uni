using backend.Data;
using backend.DTO;
using backend.Model;
using Microsoft.EntityFrameworkCore;

namespace backend.Services
{
    public class SellerRatingService : ISellerRatingService
    {
        private readonly AppDbContext _context;
        private readonly ILogger<SellerRatingService> _logger;

        public SellerRatingService(AppDbContext context, ILogger<SellerRatingService> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task RecalculateSellerRatingAsync(int sellerId)
        {
            try
            {
                _logger.LogInformation("Recalculating rating for seller {SellerId}", sellerId);

                // Get all reviews for this seller
                var reviews = await _context.Reviews
                    .Where(r => r.SellerId == sellerId)
                    .ToListAsync();

                // Get total completed sales
                var totalSales = await _context.Orders
                    .Where(o => o.SellerId == sellerId && o.Status == OrderStatus.Completed)
                    .CountAsync();

                // Find or create seller rating record
                var sellerRating = await _context.SellerRatings
                    .FirstOrDefaultAsync(sr => sr.SellerId == sellerId);

                if (sellerRating == null)
                {
                    sellerRating = new SellerRating { SellerId = sellerId };
                    _context.SellerRatings.Add(sellerRating);
                    _logger.LogInformation("Creating new seller rating record for seller {SellerId}", sellerId);
                }

                // Calculate ratings
                sellerRating.TotalReviews = reviews.Count;
                sellerRating.TotalSales = totalSales;
                sellerRating.AverageRating = reviews.Any()
                    ? (decimal)reviews.Average(r => r.OverallRating)
                    : 0;

                // Calculate star distribution
                sellerRating.FiveStarCount = reviews.Count(r => r.OverallRating == 5);
                sellerRating.FourStarCount = reviews.Count(r => r.OverallRating == 4);
                sellerRating.ThreeStarCount = reviews.Count(r => r.OverallRating == 3);
                sellerRating.TwoStarCount = reviews.Count(r => r.OverallRating == 2);
                sellerRating.OneStarCount = reviews.Count(r => r.OverallRating == 1);

                sellerRating.LastUpdated = DateTime.UtcNow;

                await _context.SaveChangesAsync();

                _logger.LogInformation(
                    "Seller rating recalculated for seller {SellerId}: {AverageRating} ({TotalReviews} reviews, {TotalSales} sales)",
                    sellerId, sellerRating.AverageRating, sellerRating.TotalReviews, sellerRating.TotalSales);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to recalculate rating for seller {SellerId}", sellerId);
                throw;
            }
        }

        public async Task<SellerRatingDto?> GetSellerRatingAsync(int sellerId)
        {
            try
            {
                var rating = await _context.SellerRatings
                    .FirstOrDefaultAsync(sr => sr.SellerId == sellerId);

                // If no rating exists, calculate it for the first time
                if (rating == null)
                {
                    _logger.LogInformation("No rating found for seller {SellerId}, calculating initial rating", sellerId);
                    await RecalculateSellerRatingAsync(sellerId);
                    rating = await _context.SellerRatings
                        .FirstOrDefaultAsync(sr => sr.SellerId == sellerId);
                }

                if (rating == null)
                    return null;

                return new SellerRatingDto
                {
                    SellerId = rating.SellerId,
                    AverageRating = rating.AverageRating,
                    TotalReviews = rating.TotalReviews,
                    TotalSales = rating.TotalSales,
                    FiveStarCount = rating.FiveStarCount,
                    FourStarCount = rating.FourStarCount,
                    ThreeStarCount = rating.ThreeStarCount,
                    TwoStarCount = rating.TwoStarCount,
                    OneStarCount = rating.OneStarCount,
                    LastUpdated = rating.LastUpdated
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to get rating for seller {SellerId}", sellerId);
                throw;
            }
        }
    }
}
