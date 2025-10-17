using backend.Data;
using backend.DTO;
using backend.Model;
using Microsoft.EntityFrameworkCore;

namespace backend.Services
{
    public class OrderService : IOrderService
    {
        private readonly AppDbContext _context;
        private readonly IPaystackService _paystackService;

        public OrderService(AppDbContext context, IPaystackService paystackService)
        {
            _context = context;
            _paystackService = paystackService;
        }

        public async Task<Order> CreateOrderAsync(int buyerId, CreateOrderDto dto)
        {
            // Get product details
            var product = await _context.Products
                .Include(p => p.Seller)
                .FirstOrDefaultAsync(p => p.Id == dto.ProductId && !p.IsDeleted);

            if (product == null)
                throw new InvalidOperationException("Product not found");

            if (product.IsSold)
                throw new InvalidOperationException("Product is already sold");

            // Create order
            var order = new Order
            {
                BuyerId = buyerId,
                SellerId = product.SellerId,
                ProductId = dto.ProductId,
                Amount = dto.Amount,
                Status = OrderStatus.Pending,
                ShippingAddress = dto.ShippingAddress,
                BuyerPhone = dto.BuyerPhone,
                Notes = dto.Notes,
                CreatedAt = DateTime.UtcNow,
                ExpiresAt = DateTime.UtcNow.AddHours(72) // 72-hour auto-release window
            };

            _context.Orders.Add(order);
            await _context.SaveChangesAsync();

            return order;
        }

        public async Task<Order?> GetOrderByIdAsync(int orderId)
        {
            return await _context.Orders
                .Include(o => o.Buyer)
                .Include(o => o.Seller)
                .Include(o => o.Product)
                    .ThenInclude(p => p.Images)
                .FirstOrDefaultAsync(o => o.Id == orderId);
        }

        public async Task<List<OrderResponseDto>> GetBuyerOrdersAsync(int buyerId)
        {
            var orders = await _context.Orders
                .Include(o => o.Seller)
                .Include(o => o.Product)
                    .ThenInclude(p => p.Images)
                .Where(o => o.BuyerId == buyerId)
                .OrderByDescending(o => o.CreatedAt)
                .ToListAsync();

            return orders.Select(o => MapToOrderResponseDto(o, showReleaseCode: true)).ToList();
        }

        public async Task<List<OrderResponseDto>> GetSellerOrdersAsync(int sellerId)
        {
            var orders = await _context.Orders
                .Include(o => o.Buyer)
                .Include(o => o.Product)
                    .ThenInclude(p => p.Images)
                .Where(o => o.SellerId == sellerId &&
                           (o.Status == OrderStatus.AwaitingRelease || o.Status == OrderStatus.Released))
                .OrderByDescending(o => o.CreatedAt)
                .ToListAsync();

            return orders.Select(o => MapToOrderResponseDto(o, showReleaseCode: false)).ToList();
        }

        public async Task<bool> UpdateOrderPaymentReferenceAsync(int orderId, string paymentReference)
        {
            var order = await _context.Orders.FindAsync(orderId);

            if (order == null)
                return false;

            order.PaymentReference = paymentReference;
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> UpdateOrderAfterPaymentAsync(string paymentReference, string releaseCode)
        {
            var order = await _context.Orders
                .FirstOrDefaultAsync(o => o.PaymentReference == paymentReference);

            if (order == null)
                return false;

            order.Status = OrderStatus.AwaitingRelease;
            order.PaidAt = DateTime.UtcNow;
            order.ReleaseCode = releaseCode;

            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<(bool success, string message)> VerifyReleaseCodeAsync(
            int sellerId,
            VerifyReleaseCodeDto dto)
        {
            var order = await _context.Orders
                .Include(o => o.Product)
                .Include(o => o.Seller)
                .FirstOrDefaultAsync(o => o.Id == dto.OrderId);

            if (order == null)
                return (false, "Order not found");

            if (order.SellerId != sellerId)
                return (false, "You are not authorized to release this order");

            if (order.Status != OrderStatus.AwaitingRelease)
                return (false, $"Order is not awaiting release. Current status: {order.Status}");

            // Check for too many failed attempts
            if (order.FailedReleaseAttempts >= 5)
                return (false, "Too many failed attempts. Please contact support.");

            // Verify release code
            if (order.ReleaseCode != dto.ReleaseCode)
            {
                order.FailedReleaseAttempts++;
                await _context.SaveChangesAsync();
                return (false, $"Invalid release code. Attempts remaining: {5 - order.FailedReleaseAttempts}");
            }

            // Release code is correct - update order status
            order.Status = OrderStatus.Released;
            order.ReleasedAt = DateTime.UtcNow;

            // Mark product as sold
            var product = order.Product;
            product.IsSold = true;
            product.SoldAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            // Initiate payout to seller (if Paystack recipient code exists)
            if (!string.IsNullOrEmpty(order.Seller.PaystackRecipientCode))
            {
                var transferReference = $"PAYOUT-{order.Id}-{DateTime.UtcNow.Ticks}";
                await _paystackService.InitiateTransferAsync(
                    order.Seller.PaystackRecipientCode,
                    order.Amount,
                    transferReference);
            }

            return (true, "Payment released successfully!");
        }

        public string GenerateReleaseCode()
        {
            var random = new Random();
            return random.Next(100000, 999999).ToString();
        }

        public async Task<OrderStatus?> GetOrderStatusAsync(int orderId)
        {
            var order = await _context.Orders.FindAsync(orderId);
            return order?.Status;
        }

        private OrderResponseDto MapToOrderResponseDto(Order order, bool showReleaseCode)
        {
            return new OrderResponseDto
            {
                Id = order.Id,
                BuyerId = order.BuyerId,
                BuyerName = order.Buyer?.FullName ?? "Unknown",
                BuyerEmail = order.Buyer?.Email,
                SellerId = order.SellerId,
                SellerName = order.Seller?.FullName ?? "Unknown",
                SellerEmail = order.Seller?.Email,
                ProductId = order.ProductId,
                ProductName = order.Product?.Name ?? "Unknown Product",
                ProductImage = order.Product?.Images?.FirstOrDefault()?.downloadUrl,
                Amount = order.Amount,
                PaymentReference = order.PaymentReference,
                Status = order.Status,
                ReleaseCode = showReleaseCode ? order.ReleaseCode : null, // Only show to buyer
                CreatedAt = order.CreatedAt,
                PaidAt = order.PaidAt,
                ReleasedAt = order.ReleasedAt,
                ExpiresAt = order.ExpiresAt,
                ShippingAddress = order.ShippingAddress,
                BuyerPhone = order.BuyerPhone,
                Notes = order.Notes
            };
        }
    }
}
