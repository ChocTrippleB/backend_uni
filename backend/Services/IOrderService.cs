using backend.DTO;
using backend.Model;

namespace backend.Services
{
    public interface IOrderService
    {
        /// <summary>
        /// Create a new order
        /// </summary>
        Task<Order> CreateOrderAsync(int buyerId, CreateOrderDto dto);

        /// <summary>
        /// Get order by ID
        /// </summary>
        Task<Order?> GetOrderByIdAsync(int orderId);

        /// <summary>
        /// Get all orders for a buyer
        /// </summary>
        Task<List<OrderResponseDto>> GetBuyerOrdersAsync(int buyerId);

        /// <summary>
        /// Get all orders for a seller (only awaiting release orders)
        /// </summary>
        Task<List<OrderResponseDto>> GetSellerOrdersAsync(int sellerId);

        /// <summary>
        /// Update order payment reference
        /// </summary>
        Task<bool> UpdateOrderPaymentReferenceAsync(int orderId, string paymentReference);

        /// <summary>
        /// Update order status after payment confirmation
        /// </summary>
        Task<bool> UpdateOrderAfterPaymentAsync(string paymentReference, string releaseCode);

        /// <summary>
        /// Verify release code and release funds to seller
        /// </summary>
        Task<(bool success, string message)> VerifyReleaseCodeAsync(int sellerId, VerifyReleaseCodeDto dto);

        /// <summary>
        /// Generate a unique 6-digit release code
        /// </summary>
        string GenerateReleaseCode();

        /// <summary>
        /// Get order status
        /// </summary>
        Task<OrderStatus?> GetOrderStatusAsync(int orderId);
    }
}
