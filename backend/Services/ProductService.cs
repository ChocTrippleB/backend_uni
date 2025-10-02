using backend.Data;
using backend.DTO;
using backend.Model;
using Microsoft.EntityFrameworkCore;

namespace backend.Services
{
    public class ProductService : IProductService
    {
        private readonly AppDbContext _db;
        private readonly IImageService _imageService;

        public ProductService(AppDbContext db, IImageService imageService)
        {
            _db = db;
            _imageService = imageService;
        }

        public async Task<Product> CreateAsync(Product product)
        {
            _db.Products.Add(product);
            await _db.SaveChangesAsync();
            return product;
        }

        public async Task<Product?> GetByIdAsync(int id) =>
            await _db.Products
                .Include(p => p.Images)
                .Include(p => p.Category)
                .Include(p => p.SubCategory)
                .FirstOrDefaultAsync(p => p.Id == id);

        

        public async Task<Product?> UpdateAsync(int id, UpdateProductDto updated)
        {
            var product = await _db.Products.FindAsync(id);
            if (product == null) return null;

            product.Name = updated.Name;
            product.Description = updated.Description;
            product.Price = updated.Price;
            product.Condition = updated.Condition;
            product.Brand = updated.Brand;
            product.SubCategoryId = updated.SubCategoryId;
            product.CategoryId = updated.CategoryId;
            product.UpdatedAt = DateTime.UtcNow;

            await _db.SaveChangesAsync();
            return product;
        }

        public async Task<IEnumerable<object>> FilterAsync(
    string? name,
    string? category,
    string? brand,
    string? subCategory)
        {
            var query = _db.Products
                .Include(p => p.Images)
                .Include(p => p.SubCategory)
                    .ThenInclude(sc => sc.Category)
                .Where(p => !p.IsDeleted) // skip deleted
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(name))
                query = query.Where(p => p.Name.Contains(name));

            if (!string.IsNullOrWhiteSpace(category))
                query = query.Where(p => p.SubCategory.Category.Name == category);

            if (!string.IsNullOrWhiteSpace(subCategory))
                query = query.Where(p => p.SubCategory.Name == subCategory);

            if (!string.IsNullOrWhiteSpace(brand))
                query = query.Where(p => p.Brand == brand);

            return await query
                .OrderByDescending(p => p.CreatedAt)
                .Select(p => new
                {
                    p.Id,
                    p.Name,
                    p.Description,
                    p.Price,
                    p.Condition,
                    p.Brand,
                    Images = p.Images.Select(img => new { img.downloadUrl }).ToList(),
                    Category = p.SubCategory.Category.Name,
                    SubCategory = p.SubCategory.Name
                })
                .ToListAsync();
        }


        public async Task<(int totalItems, List<object> items)> GetItemsAsync(
        string? search, string? category, string? condition,
        string? sort, int page, int pageSize)
        {
            if (page <= 0) page = 1;
            if (pageSize <= 0 || pageSize > 100) pageSize = 10;

            var query = _db.Products
                .Include(i => i.Images)
                .Include(i => i.SubCategory).ThenInclude(sc => sc.Category)
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(search))
                query = query.Where(i => i.Name.Contains(search) || i.Description.Contains(search));

            if (!string.IsNullOrWhiteSpace(category))
                query = query.Where(i => i.SubCategory.Category.Name == category);

            if (!string.IsNullOrWhiteSpace(condition))
                query = query.Where(i => i.Condition == condition);

            query = sort switch
            {
                "price_asc" => query.OrderBy(i => i.Price),
                "price_desc" => query.OrderByDescending(i => i.Price),
                _ => query.OrderBy(i => i.Id)
            };

            var totalItems = await query.CountAsync();

            var items = await query
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(i => new
                {
                    i.Id,
                    i.Name,
                    i.Description,
                    i.Price,
                    i.Brand,
                    i.Condition,
                    Images = i.Images.Select(img => new { img.downloadUrl }).ToList(),
                    Category = i.SubCategory.Category.Name,
                    SubCategory = i.SubCategory.Name
                })
                .ToListAsync();

            return (totalItems, items.Cast<object>().ToList());
        }

        public async Task<List<object>> GetRecentItemsAsync(int count)
        {
            return await _db.Products
                .Include(i => i.Images)
                .Include(i => i.SubCategory).ThenInclude(sc => sc.Category)
                .OrderByDescending(i => i.CreatedAt)
                .Take(count)
                .Select(i => new
                {
                    i.Id,
                    i.Name,
                    i.Description,
                    i.Price,
                    i.Condition,
                    Images = i.Images.Select(img => new { img.downloadUrl }),
                    Category = i.SubCategory.Category.Name,
                    SubCategory = i.SubCategory.Name
                })
                .Cast<object>()
                .ToListAsync();
        }

        public async Task<List<object>> SuggestItemsAsync(string query)
        {
            return await _db.Products
                .Where(i => i.Name.Contains(query))
                .OrderBy(i => i.Name)
                .Take(5)
                .Select(i => new { i.Id, i.Name })
                .Cast<object>()
                .ToListAsync();
        }

        public async Task<List<Product>> GetByNameAsync(string name)
        {
            return await _db.Products
                .Include(p => p.Images)
                .Include(p => p.SubCategory).ThenInclude(sc => sc.Category)
                .Where(p => !p.IsDeleted && p.Name.Contains(name))
                .ToListAsync();
        }

        public async Task<List<Product>> GetByCategoryAsync(string category)
        {
            return await _db.Products
                .Include(p => p.Images)
                .Include(p => p.SubCategory).ThenInclude(sc => sc.Category)
                .Where(p => !p.IsDeleted && p.SubCategory.Category.Name == category)
                .ToListAsync();
        }

        public async Task<List<Product>> GetByBrandAsync(string brand)
        {
            return await _db.Products
                .Include(p => p.Images)
                .Include(p => p.SubCategory).ThenInclude(sc => sc.Category)
                .Where(p => !p.IsDeleted && p.Brand == brand)
                .ToListAsync();
        }

        public async Task<List<Product>> GetBySubCategoryAsync(string subCategory)
        {
            return await _db.Products
                .Include(p => p.Images)
                .Include(p => p.SubCategory).ThenInclude(sc => sc.Category)
                .Where(p => !p.IsDeleted && p.SubCategory.Name == subCategory)
                .ToListAsync();
        }

        public async Task<bool> SoftDeleteAsync(int id)
        {
            var product = await _db.Products.FindAsync(id);
            if (product == null) return false;

            product.IsDeleted = true;
            product.UpdatedAt = DateTime.UtcNow;

            await _db.SaveChangesAsync();
            return true;
        }


        public async Task DeleteAsync(int id)
        {
            var product = await _db.Products.Include(p => p.Images)
                .FirstOrDefaultAsync(p => p.Id == id);

            if (product == null) return;

            foreach (var img in product.Images.ToList())
            {
                await _imageService.DeleteAsync(img.Id);
            }

            _db.Products.Remove(product);
            await _db.SaveChangesAsync();
        }
    }

}
