using backend.DTO;
using backend.Model;

namespace backend.Services
{
    public interface IProductService
    {
        Task<Product> CreateAsync(Product product);
        Task<Product?> GetByIdAsync(int id);
        Task<Product?> GetBySlugAsync(string slug);  // NEW: Get by slug
        Task<Product?> GetBySlugOrIdAsync(string identifier);  // NEW: Get by slug or ID
        Task DeleteAsync(int id);
        Task<(int totalItems, List<object> items)> GetItemsAsync(
        string? search, string? category, string? condition, string? sort, int page, int pageSize);
        Task<List<object>> GetRecentItemsAsync(int count);
        Task<List<object>> SuggestItemsAsync(string query);
        Task<List<object>> GetItemsBySellerAsync(Guid sellerId);  // GUID seller

        Task<Product?> UpdateAsync(int id, UpdateProductDto dto);
        Task<bool> SoftDeleteAsync(int id);
        Task<IEnumerable<object>> FilterAsync(
            string? name,
            string? category,
            string? brand,
            string? subCategory);
    }

}
