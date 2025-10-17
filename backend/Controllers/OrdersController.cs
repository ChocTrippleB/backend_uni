using backend.DTO;
using backend.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace backend.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class OrdersController : ControllerBase
    {
        private readonly IOrderService _orderService;

        public OrdersController(IOrderService orderService)
        {
            _orderService = orderService;
        }

        /// <summary>
        /// Create a new order (buyer initiates)
        /// </summary>
        [HttpPost("create")]
        public async Task<IActionResult> CreateOrder([FromBody] CreateOrderDto dto)
        {
            try
            {
                var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out int buyerId))
                {
                    return Unauthorized(new { message = "Invalid user authentication" });
                }

                var order = await _orderService.CreateOrderAsync(buyerId, dto);

                return Ok(new
                {
                    success = true,
                    message = "Order created successfully",
                    orderId = order.Id,
                    data = order
                });
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { success = false, message = ex.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = "An error occurred while creating the order", error = ex.Message });
            }
        }

        /// <summary>
        /// Get all orders for the authenticated buyer
        /// </summary>
        [HttpGet("buyer")]
        public async Task<IActionResult> GetBuyerOrders()
        {
            try
            {
                var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out int buyerId))
                {
                    return Unauthorized(new { message = "Invalid user authentication" });
                }

                var orders = await _orderService.GetBuyerOrdersAsync(buyerId);

                return Ok(new
                {
                    success = true,
                    count = orders.Count,
                    data = orders
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = "An error occurred while fetching orders", error = ex.Message });
            }
        }

        /// <summary>
        /// Get all orders for the authenticated seller (only awaiting release)
        /// </summary>
        [HttpGet("seller")]
        public async Task<IActionResult> GetSellerOrders()
        {
            try
            {
                var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out int sellerId))
                {
                    return Unauthorized(new { message = "Invalid user authentication" });
                }

                var orders = await _orderService.GetSellerOrdersAsync(sellerId);

                return Ok(new
                {
                    success = true,
                    count = orders.Count,
                    data = orders
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = "An error occurred while fetching orders", error = ex.Message });
            }
        }

        /// <summary>
        /// Verify release code and release payment to seller
        /// </summary>
        [HttpPost("verify-release-code")]
        public async Task<IActionResult> VerifyReleaseCode([FromBody] VerifyReleaseCodeDto dto)
        {
            try
            {
                var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out int sellerId))
                {
                    return Unauthorized(new { message = "Invalid user authentication" });
                }

                var (success, message) = await _orderService.VerifyReleaseCodeAsync(sellerId, dto);

                if (success)
                {
                    return Ok(new { success = true, message });
                }
                else
                {
                    return BadRequest(new { success = false, message });
                }
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = "An error occurred while verifying the release code", error = ex.Message });
            }
        }

        /// <summary>
        /// Get order status by ID
        /// </summary>
        [HttpGet("{orderId}/status")]
        public async Task<IActionResult> GetOrderStatus(int orderId)
        {
            try
            {
                var status = await _orderService.GetOrderStatusAsync(orderId);

                if (status == null)
                {
                    return NotFound(new { success = false, message = "Order not found" });
                }

                return Ok(new
                {
                    success = true,
                    orderId,
                    status = status.ToString()
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = "An error occurred while fetching order status", error = ex.Message });
            }
        }

        /// <summary>
        /// Get order details by ID
        /// </summary>
        [HttpGet("{orderId}")]
        public async Task<IActionResult> GetOrderById(int orderId)
        {
            try
            {
                var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out int userId))
                {
                    return Unauthorized(new { message = "Invalid user authentication" });
                }

                var order = await _orderService.GetOrderByIdAsync(orderId);

                if (order == null)
                {
                    return NotFound(new { success = false, message = "Order not found" });
                }

                // Ensure user is either buyer or seller
                if (order.BuyerId != userId && order.SellerId != userId)
                {
                    return Forbid();
                }

                return Ok(new
                {
                    success = true,
                    data = order
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = "An error occurred while fetching the order", error = ex.Message });
            }
        }
    }
}
