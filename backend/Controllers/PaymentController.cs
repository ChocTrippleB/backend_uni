using backend.DTO;
using backend.Helpers;
using backend.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace backend.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class PaymentController : ControllerBase
    {
        private readonly IPaystackService _paystackService;
        private readonly IOrderService _orderService;

        public PaymentController(IPaystackService paystackService, IOrderService orderService)
        {
            _paystackService = paystackService;
            _orderService = orderService;
        }

        /// <summary>
        /// Initialize a Paystack payment transaction
        /// </summary>
        [HttpPost("initialize")]
        public async Task<IActionResult> InitializePayment([FromBody] InitializePaymentDto dto)
        {
            try
            {
                var userId = User.GetUserId();
                if (userId == null)
                {
                    return Unauthorized(new { message = "Invalid user authentication" });
                }

                // Get order details
                var order = await _orderService.GetOrderByIdAsync(dto.OrderId);
                if (order == null)
                {
                    return NotFound(new { success = false, message = "Order not found" });
                }

                // Generate unique payment reference
                var reference = $"ORD-{dto.OrderId}-{DateTime.UtcNow.Ticks}";

                // Initialize Paystack payment
                var response = await _paystackService.InitializePaymentAsync(
                    dto.Email,
                    dto.Amount,
                    reference);

                if (response == null || !response.Status)
                {
                    return BadRequest(new
                    {
                        success = false,
                        message = "Failed to initialize payment with Paystack"
                    });
                }

                // Save the payment reference to database (use our generated reference)
                await _orderService.UpdateOrderPaymentReferenceAsync(order.Id, reference);

                return Ok(new
                {
                    success = true,
                    message = "Payment initialized successfully",
                    data = new
                    {
                        authorizationUrl = response.Data?.Authorization_url,
                        accessCode = response.Data?.Access_code,
                        reference = reference  // Return OUR generated reference, not Paystack's
                    }
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    success = false,
                    message = "An error occurred while initializing payment",
                    error = ex.Message
                });
            }
        }

        /// <summary>
        /// Verify Paystack payment and update order
        /// </summary>
        [HttpPost("verify")]
        [AllowAnonymous] // Allow unauthenticated access for callback from Paystack redirect
        public async Task<IActionResult> VerifyPayment([FromBody] VerifyPaymentDto dto)
        {
            try
            {
                // Verify payment with Paystack
                var response = await _paystackService.VerifyPaymentAsync(dto.PaymentReference);

                if (response == null || !response.Status)
                {
                    return BadRequest(new
                    {
                        success = false,
                        message = "Failed to verify payment with Paystack"
                    });
                }

                // Check if payment was successful
                if (response.Data?.Status?.ToLower() != "success")
                {
                    return BadRequest(new
                    {
                        success = false,
                        message = $"Payment was not successful. Status: {response.Data?.Status}"
                    });
                }

                // Generate 6-digit release code
                var releaseCode = _orderService.GenerateReleaseCode();

                // Update order status and add release code
                var updated = await _orderService.UpdateOrderAfterPaymentAsync(
                    dto.PaymentReference,
                    releaseCode);

                if (!updated)
                {
                    return NotFound(new
                    {
                        success = false,
                        message = "Order not found for this payment reference"
                    });
                }

                return Ok(new
                {
                    success = true,
                    message = "Payment verified successfully",
                    data = new
                    {
                        releaseCode,
                        amount = response.Data?.Amount / 100, // Convert from kobo to main currency
                        paidAt = response.Data?.Paid_at,
                        status = response.Data?.Status
                    }
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    success = false,
                    message = "An error occurred while verifying payment",
                    error = ex.Message
                });
            }
        }

        /// <summary>
        /// Webhook endpoint for Paystack payment notifications
        /// </summary>
        [HttpPost("webhook")]
        [AllowAnonymous]
        public async Task<IActionResult> PaystackWebhook()
        {
            try
            {
                // Read the request body
                using var reader = new StreamReader(Request.Body);
                var body = await reader.ReadToEndAsync();

                // TODO: Verify webhook signature for security
                // var signature = Request.Headers["x-paystack-signature"].ToString();
                // if (!VerifyPaystackSignature(body, signature)) return Unauthorized();

                // Parse webhook data and process accordingly
                // This is where you'd handle different event types:
                // - charge.success
                // - transfer.success
                // - transfer.failed
                // etc.

                return Ok(new { success = true });
            }
            catch (Exception ex)
            {
                // Log error but return 200 to prevent Paystack from retrying
                Console.WriteLine($"Webhook Error: {ex.Message}");
                return Ok(new { success = false, error = ex.Message });
            }
        }
    }
}
