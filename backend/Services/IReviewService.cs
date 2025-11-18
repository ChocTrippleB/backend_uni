using backend.DTO;

namespace backend.Services
{
    public interface IReviewService
    {
        // Create & Update
        Task<ReviewResponseDto> CreateReviewAsync(Guid buyerId, CreateReviewDto dto);
        Task<ReviewResponseDto?> UpdateReviewAsync(int reviewId, Guid buyerId, UpdateReviewDto dto);
        Task<bool> DeleteReviewAsync(int reviewId, Guid buyerId);

        // Query
        Task<ReviewResponseDto?> GetReviewByIdAsync(int reviewId);
        Task<ReviewResponseDto?> GetReviewByOrderIdAsync(int orderId);
        Task<List<ReviewResponseDto>> GetSellerReviewsAsync(Guid sellerId, int page = 1, int pageSize = 10);
        Task<List<ReviewResponseDto>> GetBuyerReviewsAsync(Guid buyerId);

        // Check
        Task<bool> CanReviewOrderAsync(Guid buyerId, int orderId);
        Task<bool> HasReviewedOrderAsync(int orderId);
    }
}
