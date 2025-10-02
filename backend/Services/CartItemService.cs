using backend.Data;
using backend.Model;
using Microsoft.EntityFrameworkCore;

namespace backend.Services
{
    public class CartItemService : ICartItemService
    {
        private readonly AppDbContext _db;

        public CartItemService(AppDbContext db)
        {
            _db = db;
        }

        public async Task<CartItem?> GetByIdAsync(int id)
        {
            return await _db.CartItems
                .Include(ci => ci.Product)
                .FirstOrDefaultAsync(ci => ci.Id == id);
        }

        public async Task<CartItem> UpdateQuantityAsync(int itemId, int quantity)
        {
            var item = await _db.CartItems.FindAsync(itemId);
            if (item == null) throw new ArgumentException("Cart item not found");

            item.Quantity = quantity;
            item.UpdateTotalPrice();

            await _db.SaveChangesAsync();
            return item;
        }
    }
}
