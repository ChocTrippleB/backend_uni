using backend.Data;
using backend.Model;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using System.Threading;
using System.Transactions;

namespace backend.Services
{
    public class CartService: ICartService
    {
        private readonly AppDbContext _db;


        public CartService(AppDbContext db)
        {
            _db = db;
        }

        public async Task<Cart> GetCartAsync(int userId)
        {
            var cart = await _db.Carts
                .Include(c => c.Items)
                .ThenInclude(i => i.Product)
                .ThenInclude(p => p.Images) // make sure images are loaded
                .Include(c => c.Items)
                .ThenInclude(i => i.Product)
                .ThenInclude(p => p.Seller) // include seller info
                .FirstOrDefaultAsync(c => c.UserId == userId)
                ?? new Cart { UserId = userId };

            // Ensure each product has at least one image
            foreach (var item in cart.Items)
            {
                if (item.Product.Images == null || !item.Product.Images.Any())
                {
                    item.Product.Images = new List<Image>
            {
                new Image { downloadUrl = "/blue-low-nike.png" }
            };
                }
            }

            return cart;
        }


        public async Task<Cart> AddItemAsync(int userId, int productId, int quantity)
        {
            var cart = await GetOrCreateCartAsync(userId);
            var product = await _db.Products.FindAsync(productId)
                          ?? throw new Exception("Product not found");

            var existingItem = cart.Items.FirstOrDefault(i => i.ProductId == productId);

            if (existingItem != null)
            {
                existingItem.Quantity += quantity;
                existingItem.SetTotalPrice();
            }
            else
            {
                var newItem = new CartItem
                {
                    ProductId = productId,
                    Quantity = quantity,
                    UnitPrice = product.Price
                };
                newItem.SetTotalPrice();
                cart.Items.Add(newItem);
            }

            cart.UpdateTotalAmount();
            await _db.SaveChangesAsync();

            return cart;
        }

        public async Task<Cart> UpdateItemQuantityAsync(int userId, int productId, int delta)
        {
            var cart = await GetOrCreateCartAsync(userId);
            var item = cart.Items.FirstOrDefault(i => i.ProductId == productId);

            if (item == null) throw new Exception("Item not in cart");

            item.Quantity += delta;

            if (item.Quantity <= 0)
                cart.Items.Remove(item);
            else
                item.SetTotalPrice();

            cart.UpdateTotalAmount();
            await _db.SaveChangesAsync();

            return cart;
        }


        public async Task<Cart> RemoveItemAsync(int userId, int productId)
        {
            var cart = await GetOrCreateCartAsync(userId);
            var item = cart.Items.FirstOrDefault(i => i.ProductId == productId);

            if (item != null)
            {
                cart.Items.Remove(item);
            }

            cart.UpdateTotalAmount();
            await _db.SaveChangesAsync();

            return cart;
        }

        public async Task<Cart> ClearCartAsync(int userId)
        {
            var cart = await GetOrCreateCartAsync(userId);
            cart.Items.Clear();
            cart.UpdateTotalAmount();
            await _db.SaveChangesAsync();
            return cart;
        }

        private async Task<Cart> GetOrCreateCartAsync(int userId)
        {
            var cart = await _db.Carts
                .Include(c => c.Items)
                .FirstOrDefaultAsync(c => c.UserId == userId);

            if (cart == null)
            {
                cart = new Cart { UserId = userId };
                _db.Carts.Add(cart);
                await _db.SaveChangesAsync();
            }

            return cart;
        }
    }
}