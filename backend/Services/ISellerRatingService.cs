using backend.DTO;

namespace backend.Services
{
    public interface ISellerRatingService
    {
        Task RecalculateSellerRatingAsync(int sellerId);
        Task<SellerRatingDto?> GetSellerRatingAsync(int sellerId);
    }
}
