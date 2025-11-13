using backend.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace backend.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class PayoutController : ControllerBase
    {
        private readonly IPayoutService _payoutService;
        private readonly ILogger<PayoutController> _logger;

        public PayoutController(IPayoutService payoutService, ILogger<PayoutController> logger)
        {
            _payoutService = payoutService;
            _logger = logger;
        }

        /// <summary>
        /// Process all pending payouts (Admin/System endpoint)
        /// This can be called manually or by a scheduled job
        /// </summary>
        [HttpPost("process")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> ProcessPendingPayouts()
        {
            try
            {
                _logger.LogInformation("Manual batch payout processing triggered");

                var (successCount, failureCount, errors) = await _payoutService.ProcessPendingPayoutsAsync();

                return Ok(new
                {
                    success = true,
                    message = $"Batch processing complete: {successCount} succeeded, {failureCount} failed",
                    data = new
                    {
                        successCount,
                        failureCount,
                        errors
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing batch payouts");
                return StatusCode(500, new
                {
                    success = false,
                    message = "Error processing payouts",
                    error = ex.Message
                });
            }
        }

        /// <summary>
        /// Get pending payouts for a specific date (Admin only)
        /// </summary>
        [HttpGet("pending/{date}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> GetPendingPayoutsByDate(DateTime date)
        {
            try
            {
                var payouts = await _payoutService.GetPendingPayoutsByDateAsync(date);

                return Ok(new
                {
                    success = true,
                    data = payouts
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting pending payouts");
                return StatusCode(500, new
                {
                    success = false,
                    message = "Error retrieving payouts",
                    error = ex.Message
                });
            }
        }

        /// <summary>
        /// Get all payouts for the current seller
        /// </summary>
        [HttpGet("my-payouts")]
        [Authorize] // Any authenticated user can view their own payouts
        public async Task<IActionResult> GetMyPayouts()
        {
            try
            {
                var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out int sellerId))
                {
                    return Unauthorized(new { message = "Invalid user authentication" });
                }

                var payouts = await _payoutService.GetSellerPayoutsAsync(sellerId);

                return Ok(new
                {
                    success = true,
                    data = payouts
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting seller payouts");
                return StatusCode(500, new
                {
                    success = false,
                    message = "Error retrieving your payouts",
                    error = ex.Message
                });
            }
        }

        /// <summary>
        /// Get payout by ID
        /// Sellers can only view their own payouts, admins can view all
        /// </summary>
        [HttpGet("{payoutId}")]
        public async Task<IActionResult> GetPayoutById(int payoutId)
        {
            try
            {
                var payout = await _payoutService.GetPayoutByIdAsync(payoutId);

                if (payout == null)
                {
                    return NotFound(new
                    {
                        success = false,
                        message = "Payout not found"
                    });
                }

                // Authorization check: sellers can only view their own payouts
                var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                var userRole = User.FindFirst(ClaimTypes.Role)?.Value;

                if (userRole != "Admin" && payout.SellerId.ToString() != userIdClaim)
                {
                    return Forbid();
                }

                return Ok(new
                {
                    success = true,
                    data = payout
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting payout by ID");
                return StatusCode(500, new
                {
                    success = false,
                    message = "Error retrieving payout",
                    error = ex.Message
                });
            }
        }

        /// <summary>
        /// Retry a failed payout (Admin only)
        /// </summary>
        [HttpPost("{payoutId}/retry")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> RetryFailedPayout(int payoutId)
        {
            try
            {
                var result = await _payoutService.RetryFailedPayoutAsync(payoutId);

                if (!result)
                {
                    return BadRequest(new
                    {
                        success = false,
                        message = "Payout not found or not in failed status"
                    });
                }

                return Ok(new
                {
                    success = true,
                    message = "Payout rescheduled for next payout date"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrying payout");
                return StatusCode(500, new
                {
                    success = false,
                    message = "Error retrying payout",
                    error = ex.Message
                });
            }
        }

        /// <summary>
        /// Get payout statistics (Admin only)
        /// </summary>
        [HttpGet("stats")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> GetPayoutStats()
        {
            try
            {
                var stats = await _payoutService.GetPayoutStatsAsync();

                return Ok(new
                {
                    success = true,
                    data = stats
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting payout stats");
                return StatusCode(500, new
                {
                    success = false,
                    message = "Error retrieving payout statistics",
                    error = ex.Message
                });
            }
        }
    }
}
