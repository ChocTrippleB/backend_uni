using backend.DTO;
using backend.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace backend.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ReviewController : ControllerBase
    {
        private readonly IReviewService _reviewService;
        private readonly ILogger<ReviewController> _logger;

        public ReviewController(IReviewService reviewService, ILogger<ReviewController> logger)
        {
            _reviewService = reviewService;
            _logger = logger;
        }

        /// <summary>
        /// Create a review for a completed order
        /// </summary>
        [HttpPost]
        [Authorize]
        public async Task<IActionResult> CreateReview([FromBody] CreateReviewDto dto)
        {
            try
            {
                var buyerId = GetCurrentUserId();

                if (buyerId == 0)
                {
                    return Unauthorized(new { success = false, message = "User not authenticated" });
                }

                var review = await _reviewService.CreateReviewAsync(buyerId, dto);
                return Ok(new { success = true, data = review, message = "Review created successfully" });
            }
            catch (UnauthorizedAccessException ex)
            {
                return Unauthorized(new { success = false, message = ex.Message });
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { success = false, message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating review");
                return StatusCode(500, new { success = false, message = "An error occurred while creating the review" });
            }
        }

        /// <summary>
        /// Update a review (within 7 days of creation)
        /// </summary>
        [HttpPut("{reviewId}")]
        [Authorize]
        public async Task<IActionResult> UpdateReview(int reviewId, [FromBody] UpdateReviewDto dto)
        {
            try
            {
                var buyerId = GetCurrentUserId();

                if (buyerId == 0)
                {
                    return Unauthorized(new { success = false, message = "User not authenticated" });
                }

                var review = await _reviewService.UpdateReviewAsync(reviewId, buyerId, dto);

                if (review == null)
                {
                    return NotFound(new { success = false, message = "Review not found or you are not authorized to update it" });
                }

                return Ok(new { success = true, data = review, message = "Review updated successfully" });
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { success = false, message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating review {ReviewId}", reviewId);
                return StatusCode(500, new { success = false, message = "An error occurred while updating the review" });
            }
        }

        /// <summary>
        /// Delete a review
        /// </summary>
        [HttpDelete("{reviewId}")]
        [Authorize]
        public async Task<IActionResult> DeleteReview(int reviewId)
        {
            try
            {
                var buyerId = GetCurrentUserId();

                if (buyerId == 0)
                {
                    return Unauthorized(new { success = false, message = "User not authenticated" });
                }

                var success = await _reviewService.DeleteReviewAsync(reviewId, buyerId);

                if (!success)
                {
                    return NotFound(new { success = false, message = "Review not found or you are not authorized to delete it" });
                }

                return Ok(new { success = true, message = "Review deleted successfully" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting review {ReviewId}", reviewId);
                return StatusCode(500, new { success = false, message = "An error occurred while deleting the review" });
            }
        }

        /// <summary>
        /// Get a specific review by ID
        /// </summary>
        [HttpGet("{reviewId}")]
        [AllowAnonymous]
        public async Task<IActionResult> GetReview(int reviewId)
        {
            try
            {
                var review = await _reviewService.GetReviewByIdAsync(reviewId);

                if (review == null)
                {
                    return NotFound(new { success = false, message = "Review not found" });
                }

                return Ok(new { success = true, data = review });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting review {ReviewId}", reviewId);
                return StatusCode(500, new { success = false, message = "An error occurred while retrieving the review" });
            }
        }

        /// <summary>
        /// Get review for a specific order
        /// </summary>
        [HttpGet("order/{orderId}")]
        [Authorize]
        public async Task<IActionResult> GetReviewByOrder(int orderId)
        {
            try
            {
                var review = await _reviewService.GetReviewByOrderIdAsync(orderId);
                return Ok(new { success = true, data = review });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting review for order {OrderId}", orderId);
                return StatusCode(500, new { success = false, message = "An error occurred while retrieving the review" });
            }
        }

        /// <summary>
        /// Get all reviews for a seller (paginated)
        /// </summary>
        [HttpGet("seller/{sellerId}")]
        [AllowAnonymous]
        public async Task<IActionResult> GetSellerReviews(int sellerId, [FromQuery] int page = 1, [FromQuery] int pageSize = 10)
        {
            try
            {
                if (page < 1) page = 1;
                if (pageSize < 1 || pageSize > 50) pageSize = 10;

                var reviews = await _reviewService.GetSellerReviewsAsync(sellerId, page, pageSize);
                return Ok(new { success = true, data = reviews, page, pageSize });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting reviews for seller {SellerId}", sellerId);
                return StatusCode(500, new { success = false, message = "An error occurred while retrieving reviews" });
            }
        }

        /// <summary>
        /// Get all reviews by the current user
        /// </summary>
        [HttpGet("my-reviews")]
        [Authorize]
        public async Task<IActionResult> GetMyReviews()
        {
            try
            {
                var buyerId = GetCurrentUserId();

                if (buyerId == 0)
                {
                    return Unauthorized(new { success = false, message = "User not authenticated" });
                }

                var reviews = await _reviewService.GetBuyerReviewsAsync(buyerId);
                return Ok(new { success = true, data = reviews });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting reviews for current user");
                return StatusCode(500, new { success = false, message = "An error occurred while retrieving your reviews" });
            }
        }

        /// <summary>
        /// Check if the current user can review a specific order
        /// </summary>
        [HttpGet("can-review/{orderId}")]
        [Authorize]
        public async Task<IActionResult> CanReviewOrder(int orderId)
        {
            try
            {
                var buyerId = GetCurrentUserId();

                if (buyerId == 0)
                {
                    return Unauthorized(new { success = false, message = "User not authenticated" });
                }

                var canReview = await _reviewService.CanReviewOrderAsync(buyerId, orderId);
                return Ok(new { success = true, canReview });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking if order {OrderId} can be reviewed", orderId);
                return StatusCode(500, new { success = false, message = "An error occurred" });
            }
        }

        private int GetCurrentUserId()
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            return int.TryParse(userIdClaim, out var userId) ? userId : 0;
        }
    }
}
