using backend.DTO;
using backend.Model;
using backend.Services;
using Microsoft.AspNetCore.Mvc;

namespace backend.Controllers
{
    [ApiController]
    [Route("api/v1/cart")]
    public class CartController : ControllerBase
    {
        private readonly ICartService _cartService;

        public CartController(ICartService cartService)
        {
            _cartService = cartService;
        }

        // ✅ Get cart for a user
        [HttpGet("{userId}")]
        public async Task<IActionResult> GetCart(int userId)
        {
            var cart = await _cartService.GetCartAsync(userId);
            return Ok(cart);
        }

        // ✅ Add product to cart
        [HttpPost("{userId}/add")]
        public async Task<IActionResult> AddItem(int userId, [FromBody] AddCartItemDto dto)
        {
            var cart = await _cartService.AddItemAsync(userId, dto.ProductId, dto.Quantity);
            return Ok(cart);
        }

        [HttpPatch("{userId}/items/{productId}")]
        public async Task<IActionResult> UpdateItemQuantity(
                                                            int userId,
                                                            int productId,
                                                            [FromQuery] int delta = 0)  // positive = increment, negative = decrement
        {
            if (delta == 0)
                return BadRequest("Delta must be non-zero");

            var cart = await _cartService.UpdateItemQuantityAsync(userId, productId, delta);
            return Ok(cart);
        }


        // ✅ Remove product from cart
        [HttpDelete("{userId}/remove/{productId}")]
        public async Task<IActionResult> RemoveItem(int userId, int productId)
        {
            var cart = await _cartService.RemoveItemAsync(userId, productId);
            return Ok(cart);
        }

        // ✅ Clear cart
        [HttpDelete("{userId}/clear")]
        public async Task<IActionResult> ClearCart(int userId)
        {
            var cart = await _cartService.ClearCartAsync(userId);
            return Ok(cart);
        }
    }

}
