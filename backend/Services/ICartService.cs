using backend.Model;
using System.Threading.Tasks;

namespace backend.Services
{
    public interface ICartService
    {
        Task<Cart> GetCartAsync(Guid userId);
        Task<Cart> AddItemAsync(Guid userId, int productId, int quantity = 1);
        Task<Cart> UpdateItemQuantityAsync(Guid userId, int productId, int delta);
        Task<Cart> RemoveItemAsync(Guid userId, int productId);
        Task<Cart> ClearCartAsync(Guid userId);
    }
}
    