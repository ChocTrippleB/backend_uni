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
        private readonly ISlugService _slugService;

        public ProductService(AppDbContext db, IImageService imageService, ISlugService slugService)
        {
            _db = db;
            _imageService = imageService;
            _slugService = slugService;
        }

        public async Task<Product> CreateAsync(Product product)
        {
            // Generate unique slug from product name
            product.Slug = await _slugService.GenerateUniqueSlugAsync(product.Name);

            _db.Products.Add(product);
            await _db.SaveChangesAsync();
            return product;
        }

        public async Task<Product?> GetByIdAsync(int id)
        {
            var product = await _db.Products
                .Include(p => p.Category)
                .Include(p => p.SubCategory)
                .Include(p => p.Seller)
                .FirstOrDefaultAsync(p => p.Id == id);

            if (product != null)
            {
                // Load images ordered by DisplayOrder
                await _db.Entry(product)
                    .Collection(p => p.Images)
                    .Query()
                    .OrderBy(i => i.DisplayOrder)
                    .LoadAsync();
            }

            return product;
        }

        public async Task<Product?> GetBySlugAsync(string slug)
        {
            var product = await _db.Products
                .Include(p => p.Category)
                .Include(p => p.SubCategory)
                .Include(p => p.Seller)
                .FirstOrDefaultAsync(p => p.Slug == slug);

            if (product != null)
            {
                // Load images ordered by DisplayOrder
                await _db.Entry(product)
                    .Collection(p => p.Images)
                    .Query()
                    .OrderBy(i => i.DisplayOrder)
                    .LoadAsync();
            }

            return product;
        }

        public async Task<Product?> GetBySlugOrIdAsync(string identifier)
        {
            // Try to parse as int ID first for backward compatibility
            if (int.TryParse(identifier, out var id))
            {
                var productById = await GetByIdAsync(id);
                if (productById != null) return productById;
            }

            // Otherwise, treat as slug
            return await GetBySlugAsync(identifier);
        }



        public async Task<Product?> UpdateAsync(int id, UpdateProductDto updated)
        {
            var product = await _db.Products.FindAsync(id);
            if (product == null) return null;

            // Regenerate slug if name changed
            if (product.Name != updated.Name)
            {
                product.Slug = await _slugService.GenerateUniqueSlugAsync(updated.Name, id);
            }

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
                    p.Slug,
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
                .Where(i => !i.IsDeleted && !i.IsSold) // Exclude deleted and sold items from shop
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(search))
            {
                var lowerSearch = search.ToLower();
                query = query.Where(i => i.Name.ToLower().Contains(lowerSearch) || i.Description.ToLower().Contains(lowerSearch));
            }

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
                    i.Slug,
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
                .Where(i => !i.IsDeleted && !i.IsSold) // Exclude deleted and sold items from home page
                .OrderByDescending(i => i.CreatedAt)
                .Take(count)
                .Select(i => new
                {
                    i.Id,
                    i.Slug,
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
            var lowerQuery = query.ToLower();

            // Search products (case-insensitive)
            var products = await _db.Products
                .Include(p => p.Images)
                .Include(p => p.SubCategory).ThenInclude(sc => sc.Category)
                .Where(p => !p.IsDeleted && (p.Name.ToLower().Contains(lowerQuery) || p.Brand.ToLower().Contains(lowerQuery)))
                .OrderBy(p => p.Name)
                .Take(5)
                .Select(p => new
                {
                    type = "product",
                    id = p.Id,
                    slug = p.Slug,
                    name = p.Name,
                    imageUrl = p.Images.FirstOrDefault() != null ? p.Images.FirstOrDefault().downloadUrl : null,
                    category = p.SubCategory.Category.Name,
                    condition = p.Condition,
                    price = p.Price
                })
                .Cast<object>()
                .ToListAsync();

            // Search users (case-insensitive)
            var users = await _db.Users
                .Where(u => u.Username.ToLower().Contains(lowerQuery) || u.FullName.ToLower().Contains(lowerQuery))
                .OrderBy(u => u.Username)
                .Take(3)
                .Select(u => new
                {
                    type = "user",
                    id = u.Id,
                    username = u.Username,
                    name = u.Username,
                    fullName = u.FullName,
                    imageUrl = u.ProfilePictureUrl,
                    institution = "",  // Institution relationship not needed for suggestions
                    faculty = u.Faculty ?? ""
                })
                .Cast<object>()
                .ToListAsync();

            // Combine products and users
            var combined = new List<object>();
            combined.AddRange(products);
            combined.AddRange(users);

            return combined;
        }

        public async Task<List<object>> GetItemsBySellerAsync(Guid sellerId)
        {
            return await _db.Products
                .Include(i => i.Images.OrderBy(img => img.DisplayOrder))
                .Include(i => i.SubCategory).ThenInclude(sc => sc.Category)
                .Where(i => !i.IsDeleted && i.SellerId == sellerId)
                .OrderByDescending(i => i.CreatedAt)
                .Select(i => new
                {
                    i.Id,
                    i.Slug,
                    i.Name,
                    i.Description,
                    i.Price,
                    i.Brand,
                    i.Condition,
                    i.SellerId,
                    i.IsSold, // Add sold status for profile page
                    Images = i.Images.OrderBy(img => img.DisplayOrder).Select(img => new { img.Id, img.downloadUrl }).ToList(),
                    Category = i.SubCategory.Category.Name,
                    SubCategory = i.SubCategory.Name
                })
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
