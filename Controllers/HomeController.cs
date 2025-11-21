using Microsoft.AspNetCore.Mvc;
using AnimeStore.Models;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.IO;
using Microsoft.AspNetCore.Identity;

namespace AnimeStore.Controllers
{
    public class HomeController : Controller
    {
        private readonly UserManager<IdentityUser> _userManager;

        public HomeController(UserManager<IdentityUser> userManager)
        {
            _userManager = userManager;
        }

        public IActionResult Index()
        {
            // ? If ADMIN is logged in ? go to Admin Dashboard
            if (User.Identity?.IsAuthenticated == true &&
                User.Identity.Name == "admin@gmail.com")
            {
                return RedirectToAction("Index", "Admin");
            }

            // ? Normal user ? Load normal homepage
            var products = LoadProducts();

            ViewBag.Featured = products
                .Where(p => p.Category == "Figurines")
                .Take(3)
                .ToList();

            ViewBag.MostPurchased = products
                .Where(p => p.Category == "Figurines")
                .Skip(3)
                .Take(3)
                .ToList();

            ViewBag.NewArrivals = products
                .Where(p => p.Category == "Keychains")
                .Take(3)
                .ToList();

            ViewBag.Posters = products
                .Where(p => p.Category == "Posters")
                .Take(3)
                .ToList();

            return View();
        }

        private List<Product> LoadProducts()
        {
            var jsonPath = Path.Combine(
                Directory.GetCurrentDirectory(),
                "wwwroot", "data", "products.json"
            );

            if (!System.IO.File.Exists(jsonPath))
                throw new FileNotFoundException("products.json missing.");

            var jsonData = System.IO.File.ReadAllText(jsonPath);

            var products = JsonSerializer.Deserialize<List<Product>>(jsonData,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

            return products ?? new List<Product>();
        }
    }
}
