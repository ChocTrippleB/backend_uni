using backend.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace backend.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class SellerRatingController : ControllerBase
    {
        private readonly ISellerRatingService _ratingService;
        private readonly ILogger<SellerRatingController> _logger;

        public SellerRatingController(ISellerRatingService ratingService, ILogger<SellerRatingController> logger)
        {
            _ratingService = ratingService;
            _logger = logger;
        }

        /// <summary>
        /// Get seller rating and statistics
        /// </summary>
        [HttpGet("{sellerId}")]
        [AllowAnonymous]
        public async Task<IActionResult> GetSellerRating(Guid sellerId)
        {
            try
            {
                var rating = await _ratingService.GetSellerRatingAsync(sellerId);

                if (rating == null)
                {
                    // Return default rating if seller has no reviews yet
                    return Ok(new
                    {
                        success = true,
                        data = new
                        {
                            sellerId,
                            averageRating = 0m,
                            totalReviews = 0,
                            totalSales = 0,
                            fiveStarCount = 0,
                            fourStarCount = 0,
                            threeStarCount = 0,
                            twoStarCount = 0,
                            oneStarCount = 0,
                            lastUpdated = DateTime.UtcNow
                        }
                    });
                }

                return Ok(new { success = true, data = rating });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting rating for seller {SellerId}", sellerId);
                return StatusCode(500, new { success = false, message = "An error occurred while retrieving seller rating" });
            }
        }

        /// <summary>
        /// Manually recalculate seller rating (Admin only - optional)
        /// </summary>
        [HttpPost("{sellerId}/recalculate")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> RecalculateRating(Guid sellerId)
        {
            try
            {
                await _ratingService.RecalculateSellerRatingAsync(sellerId);
                var rating = await _ratingService.GetSellerRatingAsync(sellerId);
                return Ok(new { success = true, data = rating, message = "Rating recalculated successfully" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error recalculating rating for seller {SellerId}", sellerId);
                return StatusCode(500, new { success = false, message = "An error occurred while recalculating rating" });
            }
        }
    }
}
