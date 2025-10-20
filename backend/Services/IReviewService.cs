using backend.DTO;

namespace backend.Services
{
    public interface IReviewService
    {
        // Create & Update
        Task<ReviewResponseDto> CreateReviewAsync(int buyerId, CreateReviewDto dto);
        Task<ReviewResponseDto?> UpdateReviewAsync(int reviewId, int buyerId, UpdateReviewDto dto);
        Task<bool> DeleteReviewAsync(int reviewId, int buyerId);

        // Query
        Task<ReviewResponseDto?> GetReviewByIdAsync(int reviewId);
        Task<ReviewResponseDto?> GetReviewByOrderIdAsync(int orderId);
        Task<List<ReviewResponseDto>> GetSellerReviewsAsync(int sellerId, int page = 1, int pageSize = 10);
        Task<List<ReviewResponseDto>> GetBuyerReviewsAsync(int buyerId);

        // Check
        Task<bool> CanReviewOrderAsync(int buyerId, int orderId);
        Task<bool> HasReviewedOrderAsync(int orderId);
    }
}
