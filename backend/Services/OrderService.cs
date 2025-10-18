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
                           (o.Status == OrderStatus.AwaitingRelease || o.Status == OrderStatus.AwaitingPayout || o.Status == OrderStatus.Completed))
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

            // Release code is correct - update order status to AwaitingPayout
            order.Status = OrderStatus.AwaitingPayout;
            order.ReleasedAt = DateTime.UtcNow;

            // Mark product as sold
            var product = order.Product;
            product.IsSold = true;
            product.SoldAt = DateTime.UtcNow;

            // Check if seller has added bank details
            if (string.IsNullOrEmpty(order.Seller.PaystackRecipientCode))
            {
                await _context.SaveChangesAsync();
                return (false, "Seller has not added bank details. Please add your bank details in Account Settings to receive payouts.");
            }

            // Calculate next payout date (Mon/Wed/Fri schedule)
            var scheduledDate = GetNextPayoutDate();

            // Add to payout queue instead of immediate transfer
            var payoutQueue = new PayoutQueue
            {
                OrderId = order.Id,
                SellerId = sellerId,
                SellerRecipientCode = order.Seller.PaystackRecipientCode,
                Amount = order.Amount,
                QueuedAt = DateTime.UtcNow,
                ScheduledPayoutDate = scheduledDate,
                Status = PayoutStatus.Pending
            };

            _context.PayoutQueue.Add(payoutQueue);
            await _context.SaveChangesAsync();

            return (true, $"Payment released! Funds will be transferred on {scheduledDate:dddd, MMMM dd, yyyy} at 9:00 AM.");
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

        /// <summary>
        /// Calculate next payout date based on Mon/Wed/Fri schedule
        /// </summary>
        private DateTime GetNextPayoutDate(DateTime? fromDate = null)
        {
            var today = DateTime.SpecifyKind((fromDate ?? DateTime.UtcNow).Date, DateTimeKind.Utc);
            var dayOfWeek = today.DayOfWeek;

            // Payout days: Monday, Wednesday, Friday
            switch (dayOfWeek)
            {
                case DayOfWeek.Saturday:
                case DayOfWeek.Sunday:
                case DayOfWeek.Monday:
                    return GetNextDayOfWeek(today, DayOfWeek.Monday);
                case DayOfWeek.Tuesday:
                case DayOfWeek.Wednesday:
                    return GetNextDayOfWeek(today, DayOfWeek.Wednesday);
                case DayOfWeek.Thursday:
                case DayOfWeek.Friday:
                    return GetNextDayOfWeek(today, DayOfWeek.Friday);
                default:
                    return DateTime.SpecifyKind(today.AddDays(2), DateTimeKind.Utc);
            }
        }

        private DateTime GetNextDayOfWeek(DateTime current, DayOfWeek targetDay)
        {
            int daysUntilTarget = ((int)targetDay - (int)current.DayOfWeek + 7) % 7;
            if (daysUntilTarget == 0) daysUntilTarget = 7; // If today is target day, schedule for next week
            return DateTime.SpecifyKind(current.AddDays(daysUntilTarget), DateTimeKind.Utc);
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
