using backend.Model;
using System.Threading.Tasks;

namespace backend.Services
{
    public interface ICartItemService
    {
        Task<CartItem?> GetByIdAsync(int id);
        Task<CartItem> UpdateQuantityAsync(int itemId, int quantity);
    }
}