using AnimeStore.Data;
using AnimeStore.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AnimeStore.Controllers
{
    [Authorize]
    public class CartController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<IdentityUser> _userManager;

        public CartController(ApplicationDbContext context, UserManager<IdentityUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        // Add To Cart (AJAX)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddToCart(int id)
        {
            var userId = _userManager.GetUserId(User);
            if (userId == null) return Json(new { success = false, message = "unauthorized" });

            var product = await _context.Products.FindAsync(id);
            if (product == null) return Json(new { success = false, message = "notfound" });

            var existing = await _context.CartItems.FirstOrDefaultAsync(c => c.ProductId == id && c.UserId == userId);
            if (existing == null)
            {
                _context.CartItems.Add(new CartItem { UserId = userId, ProductId = id, Quantity = 1 });
            }
            else
            {
                existing.Quantity++;
                _context.CartItems.Update(existing);
            }

            await _context.SaveChangesAsync();
            return Json(new { success = true });
        }

        // Update quantity (AJAX)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateQuantity(int itemId, int quantity)
        {
            var userId = _userManager.GetUserId(User);
            var item = await _context.CartItems.FirstOrDefaultAsync(c => c.Id == itemId && c.UserId == userId);
            if (item == null) return Json(new { success = false, message = "notfound" });

            item.Quantity = Math.Max(1, quantity);
            _context.CartItems.Update(item);
            await _context.SaveChangesAsync();
            return Json(new { success = true });
        }

        // Remove item (AJAX)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RemoveItem(int itemId)
        {
            var userId = _userManager.GetUserId(User);
            var item = await _context.CartItems.FirstOrDefaultAsync(c => c.Id == itemId && c.UserId == userId);
            if (item == null) return Json(new { success = false, message = "notfound" });

            _context.CartItems.Remove(item);
            await _context.SaveChangesAsync();
            return Json(new { success = true });
        }

        // Clear cart (AJAX)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ClearCart()
        {
            var userId = _userManager.GetUserId(User);
            var items = _context.CartItems.Where(c => c.UserId == userId);
            _context.CartItems.RemoveRange(items);
            await _context.SaveChangesAsync();
            return Json(new { success = true });
        }

        // Place order (AJAX) — accepts form fields and saves Order + OrderItems then clears cart
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> PlaceOrder([FromForm] string fullName, [FromForm] string phone,
                                                    [FromForm] string city, [FromForm] string address,
                                                    [FromForm] string paymentMethod)
        {
            var userId = _userManager.GetUserId(User);
            if (userId == null) return Json(new { success = false, message = "unauthorized" });

            var cartItems = await _context.CartItems.Include(c => c.Product).Where(c => c.UserId == userId).ToListAsync();
            if (!cartItems.Any()) return Json(new { success = false, message = "empty" });

            // Build order
            var order = new Order
            {
                UserId = userId,
                FullName = fullName ?? "N/A",
                Phone = phone ?? "N/A",
                City = city ?? "N/A",
                Address = address ?? "N/A",
                TotalAmount = cartItems.Sum(ci => ci.Product.Price * ci.Quantity),
                PaymentMethod = paymentMethod ?? "Unknown",
                Status = paymentMethod == "UPI" ? "Paid" : "Confirmed" // demo: UPI marks Paid
            };

            foreach (var ci in cartItems)
            {
                order.Items.Add(new OrderItem
                {
                    ProductId = ci.ProductId,
                    Quantity = ci.Quantity,
                    Price = ci.Product.Price
                });
            }

            _context.Orders.Add(order);
            _context.CartItems.RemoveRange(cartItems);

            await _context.SaveChangesAsync();

            return Json(new { success = true, orderId = order.Id });
        }

        // Cart page
        public async Task<IActionResult> Index()
        {
            var userId = _userManager.GetUserId(User);
            var items = await _context.CartItems.Include(c => c.Product).Where(c => c.UserId == userId).ToListAsync();
            return View(items);
        }

        // Checkout page (GET)
        public async Task<IActionResult> Checkout()
        {
            // optionally pass cart total or other data
            var userId = _userManager.GetUserId(User);
            var items = await _context.CartItems.Include(c => c.Product).Where(c => c.UserId == userId).ToListAsync();
            ViewBag.Total = items.Sum(i => i.Product.Price * i.Quantity);
            return View();
        }

        // Order confirmation page
        public IActionResult OrderConfirmed(int orderId)
        {
            return View(model: orderId);
        }
    }
}
