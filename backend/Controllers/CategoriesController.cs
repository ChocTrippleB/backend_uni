using backend.Data;
using backend.Model;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace backend.Controllers
{
    
    [ApiController]
    [Route("api/v1")]
    public class CategoriesController : Controller
    {
        private readonly AppDbContext _context;

        public CategoriesController(AppDbContext context)
        {
            _context = context;
        }

        [HttpGet("categories")]
        public async Task<IActionResult> GetCategories()
        {
            var categories = await _context.Categories
                .Select(c => new { c.Id, c.Name })
                .ToListAsync();

            return Ok(categories);
        }

        [HttpGet("subcategories/by-category/{categoryId}")]
        public async Task<IActionResult> GetSubCategories(int categoryId)
        {
            var subcategories = await _context.SubCategories
                .Where(sc => sc.CategoryId == categoryId)
                .Select(sc => new { sc.Id, sc.Name })
                .ToListAsync();

            return Ok(subcategories);
        }

        [HttpGet("brands")]
        public async Task<IActionResult> GetBrands()
        {
            var brands = await _context.Products
                .Where(p => !p.IsDeleted && !string.IsNullOrWhiteSpace(p.Brand))
                .Select(p => p.Brand)
                .Distinct()
                .OrderBy(b => b)
                .ToListAsync();

            return Ok(brands);
        }

        [HttpGet("categories-with-subcategories")]
        public async Task<IActionResult> GetCategoriesWithSubcategories()
        {
            var categories = await _context.Categories
                .Include(c => c.SubCategories)
                .Select(c => new
                {
                    c.Id,
                    c.Name,
                    SubCategories = c.SubCategories.Select(sc => new { sc.Id, sc.Name }).ToList()
                })
                .ToListAsync();

            return Ok(categories);
        }

    }
}
