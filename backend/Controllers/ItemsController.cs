using backend.Data;
using backend.DTO;
using backend.Model;
using backend.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace backend.Controllers
{
    
    [ApiController]
    [Route("api/v1")]
    public class ItemsController : Controller
    {
        private readonly IProductService _productService;
        private readonly IImageService _imageService;

        public ItemsController(IProductService productService, IImageService imageService)
        {
            _productService = productService;
            _imageService = imageService;
        }

        [HttpGet("items")]
        public async Task<IActionResult> GetItems(
            [FromQuery] string? search,
            [FromQuery] string? category,
            [FromQuery] string? condition,
            [FromQuery] string? sort = null,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 10)
        {
            var (totalItems, items) = await _productService.GetItemsAsync(search, category, condition, sort, page, pageSize);

            return Ok(new { totalItems, page, pageSize, items });
        }

        // UPDATE
        [HttpPut("item/{id}")]
        public async Task<IActionResult> UpdateItem(int id, [FromForm] UpdateProductDto dto)
        {
            var updated = await _productService.UpdateAsync(id, dto);
            if (updated == null)
                return NotFound();

            // if new images uploaded, replace or add
            if (dto.Images != null && dto.Images.Count > 0)
            {
                var uploaded = await _imageService.UploadManyAsync(dto.Images, id);
                updated.Images = uploaded;
            }

            return Ok(updated);
        }

        // SOFT DELETE
        [HttpDelete("item/{id}")]
        public async Task<IActionResult> DeleteItem(int id)
        {
            var result = await _productService.SoftDeleteAsync(id);
            if (!result)
                return NotFound();

            return NoContent();
        }

        // SEARCH by name, category, brand, subcategory
        [HttpGet("items/filter")]
        public async Task<IActionResult> FilterItems(
            [FromQuery] string? name,
            [FromQuery] string? category,
            [FromQuery] string? brand,
            [FromQuery] string? subCategory)
        {
            var items = await _productService.FilterAsync(name, category, brand, subCategory);
            return Ok(items);
        }

        [HttpGet("recent")]
        public async Task<IActionResult> GetRecentItems([FromQuery] int count = 6)
        {
            var items = await _productService.GetRecentItemsAsync(count);
            return Ok(items);
        }

        [HttpGet("suggest")]
        public async Task<IActionResult> SuggestItems([FromQuery] string query)
        {
            if (string.IsNullOrWhiteSpace(query))
                return Ok(new List<object>());
            var items = await _productService.SuggestItemsAsync(query);
            return Ok(items);
        }

        [HttpGet("item/{id}")]
        public async Task<IActionResult> GetItem(int id)
        {
            var item = await _productService.GetByIdAsync(id);
            if (item == null)
                return NotFound("Product not found!");

            return Ok(new
            {
                item.Id,
                item.Name,
                item.Brand,
                item.Description,
                item.Price,
                item.Condition,
                Images = item.Images.Select(img => new { img.downloadUrl }).ToList(),
                Category = item.Category.Name,
                SubCategory = item.SubCategory.Name
            });
        }

        [HttpPost("items/add")]
        public async Task<IActionResult> CreateItem([FromForm] CreateItemDto dto)
        {
            var product = new Product
            {
                Name = dto.Name,
                Description = dto.Description ?? string.Empty,
                Price = dto.Price,
                Condition = dto.Condition,
                Brand = dto.Brand,
                SellerId = dto.SellerId,
                CategoryId = dto.CategoryId,
                SubCategoryId = dto.SubCategoryId,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
                Images = new List<Image>()
            };

            await _productService.CreateAsync(product);

            if (dto.Images != null && dto.Images.Count > 0)
            {
                var uploaded = await _imageService.UploadManyAsync(dto.Images, product.Id);
                product.Images = uploaded;
            }

            return CreatedAtAction(nameof(GetItem), new { id = product.Id }, product);
        }

    }
}
