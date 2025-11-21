using AnimeStore.Data;
using AnimeStore.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;

namespace AnimeStore.Controllers
{
    public class ProductsController : Controller
    {
        private readonly ApplicationDbContext _context;

        public ProductsController(ApplicationDbContext context)
        {
            _context = context;
        }

        // ✅ INDEX PAGE (with optional category/sort filters)
        public async Task<IActionResult> Index(string? category, string? sortOrder)
        {
            var products = await _context.Products.ToListAsync();

            if (!string.IsNullOrEmpty(category))
                products = products.Where(p => p.Category == category).ToList();

            if (sortOrder == "low")
                products = products.OrderBy(p => p.Price).ToList();
            else if (sortOrder == "high")
                products = products.OrderByDescending(p => p.Price).ToList();

            ViewBag.SelectedCategory = category;
            ViewBag.SortOrder = sortOrder;

            return View(products);
        }

        // ✅ DETAILS PAGE
        public async Task<IActionResult> Details(int id)
        {
            if (id <= 0)
                return NotFound();

            var product = await _context.Products.FirstOrDefaultAsync(p => p.Id == id);
            if (product == null)
                return NotFound();

            var related = await _context.Products
                .Where(p => p.Category == product.Category && p.Id != id)
                .Take(4)
                .ToListAsync();

            ViewBag.Related = related;
            return View(product);
        }

        // ✅ SEED DATABASE FROM JSON (Run once if DB is empty)
        [HttpGet]
        public async Task<IActionResult> SeedFromJson()
        {
            if (await _context.Products.AnyAsync())
                return Content("✅ Database already contains products. No seeding needed.");

            var jsonPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "data", "products.json");
            if (!System.IO.File.Exists(jsonPath))
                return NotFound("❌ products.json file missing in wwwroot/data.");

            var jsonData = await System.IO.File.ReadAllTextAsync(jsonPath);
            var products = JsonSerializer.Deserialize<List<Product>>(jsonData, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

            if (products == null || products.Count == 0)
                return Content("❌ No products found in JSON.");

            _context.Products.AddRange(products);
            await _context.SaveChangesAsync();

            return Content($"✅ Seeded {products.Count} products into the database.");
        }
    }
}
