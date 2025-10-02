using backend.Model;
using System.Threading.Tasks;

namespace backend.Services
{
    public interface ICartService
    {
        Task<Cart> GetCartAsync(int userId);
        Task<Cart> AddItemAsync(int userId, int productId, int quantity = 1);
        Task<Cart> UpdateItemQuantityAsync(int userId, int productId, int delta);
        Task<Cart> RemoveItemAsync(int userId, int productId);
        Task<Cart> ClearCartAsync(int userId);
    }
}
    