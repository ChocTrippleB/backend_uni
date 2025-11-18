using backend.DTO;

namespace backend.Services
{
    public interface ISellerRatingService
    {
        Task RecalculateSellerRatingAsync(Guid sellerId);
        Task<SellerRatingDto?> GetSellerRatingAsync(Guid sellerId);
    }
}
