using backend.Data;
using backend.DTO;
using backend.Model;
using Microsoft.EntityFrameworkCore;

namespace backend.Services
{
    public class ReviewService : IReviewService
    {
        private readonly AppDbContext _context;
        private readonly ISellerRatingService _ratingService;
        private readonly ILogger<ReviewService> _logger;

        public ReviewService(
            AppDbContext context,
            ISellerRatingService ratingService,
            ILogger<ReviewService> logger)
        {
            _context = context;
            _ratingService = ratingService;
            _logger = logger;
        }

        public async Task<ReviewResponseDto> CreateReviewAsync(int buyerId, CreateReviewDto dto)
        {
            try
            {
                _logger.LogInformation("User {BuyerId} attempting to create review for order {OrderId}",
                    buyerId, dto.OrderId);

                // 1. Validate order exists and belongs to buyer
                var order = await _context.Orders
                    .Include(o => o.Buyer)
                    .Include(o => o.Seller)
                    .Include(o => o.Product)
                        .ThenInclude(p => p.Images)
                    .FirstOrDefaultAsync(o => o.Id == dto.OrderId && o.BuyerId == buyerId);

                if (order == null)
                {
                    _logger.LogWarning("Review creation failed: Order {OrderId} not found or does not belong to user {BuyerId}",
                        dto.OrderId, buyerId);
                    throw new UnauthorizedAccessException("Order not found or you are not authorized to review this order");
                }

                // 2. Verify order is completed
                if (order.Status != OrderStatus.Completed)
                {
                    _logger.LogWarning("Review creation failed: Order {OrderId} is not completed (Status: {Status})",
                        dto.OrderId, order.Status);
                    throw new InvalidOperationException("You can only review completed orders");
                }

                // 3. Check if already reviewed
                if (await HasReviewedOrderAsync(dto.OrderId))
                {
                    _logger.LogWarning("Review creation failed: Order {OrderId} has already been reviewed", dto.OrderId);
                    throw new InvalidOperationException("You have already reviewed this order");
                }

                // 4. Create review
                var review = new Review
                {
                    OrderId = dto.OrderId,
                    BuyerId = buyerId,
                    SellerId = order.SellerId,
                    ProductId = order.ProductId,
                    OverallRating = dto.OverallRating,
                    ProductQualityRating = dto.ProductQualityRating,
                    CommunicationRating = dto.CommunicationRating,
                    ShippingSpeedRating = dto.ShippingSpeedRating,
                    ReviewTitle = dto.ReviewTitle,
                    ReviewText = dto.ReviewText,
                    IsVerifiedPurchase = true,
                    CreatedAt = DateTime.UtcNow
                };

                _context.Reviews.Add(review);
                await _context.SaveChangesAsync();

                _logger.LogInformation("Review {ReviewId} created successfully for order {OrderId} by user {BuyerId}",
                    review.Id, dto.OrderId, buyerId);

                // 5. Update seller rating cache
                await _ratingService.RecalculateSellerRatingAsync(order.SellerId);

                // 6. Return response
                return MapToDto(review, order);
            }
            catch (Exception ex) when (ex is not UnauthorizedAccessException && ex is not InvalidOperationException)
            {
                _logger.LogError(ex, "Failed to create review for order {OrderId} by user {BuyerId}",
                    dto.OrderId, buyerId);
                throw;
            }
        }

        public async Task<ReviewResponseDto?> UpdateReviewAsync(int reviewId, int buyerId, UpdateReviewDto dto)
        {
            try
            {
                _logger.LogInformation("User {BuyerId} attempting to update review {ReviewId}",
                    buyerId, reviewId);

                var review = await _context.Reviews
                    .Include(r => r.Order)
                        .ThenInclude(o => o.Buyer)
                    .Include(r => r.Order)
                        .ThenInclude(o => o.Seller)
                    .Include(r => r.Order)
                        .ThenInclude(o => o.Product)
                            .ThenInclude(p => p.Images)
                    .FirstOrDefaultAsync(r => r.Id == reviewId && r.BuyerId == buyerId);

                if (review == null)
                {
                    _logger.LogWarning("Update failed: Review {ReviewId} not found or does not belong to user {BuyerId}",
                        reviewId, buyerId);
                    return null;
                }

                // Check if review is within edit window (7 days)
                if ((DateTime.UtcNow - review.CreatedAt).TotalDays > 7)
                {
                    _logger.LogWarning("Update failed: Review {ReviewId} is older than 7 days", reviewId);
                    throw new InvalidOperationException("Reviews can only be edited within 7 days of creation");
                }

                // Update fields if provided
                if (dto.OverallRating.HasValue)
                    review.OverallRating = dto.OverallRating.Value;

                if (dto.ProductQualityRating.HasValue)
                    review.ProductQualityRating = dto.ProductQualityRating.Value;

                if (dto.CommunicationRating.HasValue)
                    review.CommunicationRating = dto.CommunicationRating.Value;

                if (dto.ShippingSpeedRating.HasValue)
                    review.ShippingSpeedRating = dto.ShippingSpeedRating.Value;

                if (dto.ReviewTitle != null)
                    review.ReviewTitle = dto.ReviewTitle;

                if (dto.ReviewText != null)
                    review.ReviewText = dto.ReviewText;

                review.UpdatedAt = DateTime.UtcNow;

                await _context.SaveChangesAsync();

                _logger.LogInformation("Review {ReviewId} updated successfully by user {BuyerId}", reviewId, buyerId);

                // Recalculate seller rating if overall rating changed
                if (dto.OverallRating.HasValue)
                {
                    await _ratingService.RecalculateSellerRatingAsync(review.SellerId);
                }

                return MapToDto(review, review.Order);
            }
            catch (Exception ex) when (ex is not InvalidOperationException)
            {
                _logger.LogError(ex, "Failed to update review {ReviewId} by user {BuyerId}", reviewId, buyerId);
                throw;
            }
        }

        public async Task<bool> DeleteReviewAsync(int reviewId, int buyerId)
        {
            try
            {
                _logger.LogInformation("User {BuyerId} attempting to delete review {ReviewId}",
                    buyerId, reviewId);

                var review = await _context.Reviews
                    .FirstOrDefaultAsync(r => r.Id == reviewId && r.BuyerId == buyerId);

                if (review == null)
                {
                    _logger.LogWarning("Delete failed: Review {ReviewId} not found or does not belong to user {BuyerId}",
                        reviewId, buyerId);
                    return false;
                }

                var sellerId = review.SellerId;

                _context.Reviews.Remove(review);
                await _context.SaveChangesAsync();

                _logger.LogInformation("Review {ReviewId} deleted successfully by user {BuyerId}", reviewId, buyerId);

                // Recalculate seller rating
                await _ratingService.RecalculateSellerRatingAsync(sellerId);

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to delete review {ReviewId} by user {BuyerId}", reviewId, buyerId);
                throw;
            }
        }

        public async Task<ReviewResponseDto?> GetReviewByIdAsync(int reviewId)
        {
            var review = await _context.Reviews
                .Include(r => r.Buyer)
                .Include(r => r.Seller)
                .Include(r => r.Product)
                    .ThenInclude(p => p.Images)
                .Include(r => r.Order)
                .FirstOrDefaultAsync(r => r.Id == reviewId);

            if (review == null)
                return null;

            return MapToDto(review, review.Order);
        }

        public async Task<ReviewResponseDto?> GetReviewByOrderIdAsync(int orderId)
        {
            var review = await _context.Reviews
                .Include(r => r.Buyer)
                .Include(r => r.Seller)
                .Include(r => r.Product)
                    .ThenInclude(p => p.Images)
                .Include(r => r.Order)
                .FirstOrDefaultAsync(r => r.OrderId == orderId);

            if (review == null)
                return null;

            return MapToDto(review, review.Order);
        }

        public async Task<List<ReviewResponseDto>> GetSellerReviewsAsync(int sellerId, int page = 1, int pageSize = 10)
        {
            var reviews = await _context.Reviews
                .Include(r => r.Buyer)
                .Include(r => r.Seller)
                .Include(r => r.Product)
                    .ThenInclude(p => p.Images)
                .Include(r => r.Order)
                .Where(r => r.SellerId == sellerId)
                .OrderByDescending(r => r.CreatedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            return reviews.Select(r => MapToDto(r, r.Order)).ToList();
        }

        public async Task<List<ReviewResponseDto>> GetBuyerReviewsAsync(int buyerId)
        {
            var reviews = await _context.Reviews
                .Include(r => r.Buyer)
                .Include(r => r.Seller)
                .Include(r => r.Product)
                    .ThenInclude(p => p.Images)
                .Include(r => r.Order)
                .Where(r => r.BuyerId == buyerId)
                .OrderByDescending(r => r.CreatedAt)
                .ToListAsync();

            return reviews.Select(r => MapToDto(r, r.Order)).ToList();
        }

        public async Task<bool> CanReviewOrderAsync(int buyerId, int orderId)
        {
            var order = await _context.Orders
                .FirstOrDefaultAsync(o => o.Id == orderId && o.BuyerId == buyerId);

            if (order == null)
                return false;

            if (order.Status != OrderStatus.Completed)
                return false;

            if (await HasReviewedOrderAsync(orderId))
                return false;

            return true;
        }

        public async Task<bool> HasReviewedOrderAsync(int orderId)
        {
            return await _context.Reviews.AnyAsync(r => r.OrderId == orderId);
        }

        // Helper method to map Review entity to DTO
        private ReviewResponseDto MapToDto(Review review, Order order)
        {
            return new ReviewResponseDto
            {
                Id = review.Id,
                OrderId = review.OrderId,
                BuyerId = review.BuyerId,
                BuyerName = review.Buyer?.FullName ?? order.Buyer?.FullName ?? "Unknown",
                BuyerAvatar = null, // Add profile picture later in profile enhancement phase
                SellerId = review.SellerId,
                SellerName = review.Seller?.FullName ?? order.Seller?.FullName ?? "Unknown",
                ProductId = review.ProductId,
                ProductName = review.Product?.Name ?? order.Product?.Name ?? "Unknown",
                ProductImage = review.Product?.Images?.FirstOrDefault()?.downloadUrl ?? order.Product?.Images?.FirstOrDefault()?.downloadUrl,
                OverallRating = review.OverallRating,
                ProductQualityRating = review.ProductQualityRating,
                CommunicationRating = review.CommunicationRating,
                ShippingSpeedRating = review.ShippingSpeedRating,
                ReviewTitle = review.ReviewTitle,
                ReviewText = review.ReviewText,
                IsVerifiedPurchase = review.IsVerifiedPurchase,
                CreatedAt = review.CreatedAt,
                UpdatedAt = review.UpdatedAt,
                SellerResponse = review.SellerResponse,
                SellerResponseDate = review.SellerResponseDate,
                HelpfulCount = review.HelpfulCount,
                NotHelpfulCount = review.NotHelpfulCount
            };
        }
    }
}
