using AnimeStore.Data;
using AnimeStore.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AnimeStore.Controllers
{
    [Authorize]
    public class WishlistController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<IdentityUser> _userManager;

        public WishlistController(ApplicationDbContext context, UserManager<IdentityUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        // Add to wishlist (keeps current behavior)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddToWishlist(int id)
        {
            var userId = _userManager.GetUserId(User);
            if (string.IsNullOrEmpty(userId))
                return Json(new { success = false, message = "unauthorized" });

            var product = await _context.Products.FirstOrDefaultAsync(p => p.Id == id);
            if (product == null)
                return Json(new { success = false, message = "notfound" });

            var exists = await _context.WishlistItems
                .FirstOrDefaultAsync(w => w.ProductId == id && w.UserId == userId);

            if (exists == null)
            {
                _context.WishlistItems.Add(new WishlistItem { UserId = userId, ProductId = id });
                await _context.SaveChangesAsync();
            }

            return Json(new { success = true });
        }

        // Show wishlist
        public async Task<IActionResult> Index()
        {
            var userId = _userManager.GetUserId(User);
            var items = await _context.WishlistItems
                .Include(w => w.Product)
                .Where(w => w.UserId == userId)
                .ToListAsync();

            return View(items);
        }

        // Remove one wishlist item
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RemoveItem(int itemId)
        {
            var userId = _userManager.GetUserId(User);
            var item = await _context.WishlistItems
                .FirstOrDefaultAsync(w => w.Id == itemId && w.UserId == userId);

            if (item == null)
                return Json(new { success = false });

            _context.WishlistItems.Remove(item);
            await _context.SaveChangesAsync();

            return Json(new { success = true });
        }

        // Clear wishlist
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ClearWishlist()
        {
            var userId = _userManager.GetUserId(User);
            var items = _context.WishlistItems.Where(w => w.UserId == userId);

            _context.WishlistItems.RemoveRange(items);
            await _context.SaveChangesAsync();

            return Json(new { success = true });
        }

        // Move item to cart - FIXED: uses same DbContext transaction, returns JSON
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> MoveToCart(int productId)
        {
            var userId = _userManager.GetUserId(User);
            if (string.IsNullOrEmpty(userId))
                return Json(new { success = false, message = "unauthorized" });

            var product = await _context.Products.FirstOrDefaultAsync(p => p.Id == productId);
            if (product == null)
                return Json(new { success = false, message = "notfound" });

            // Add or increment cart item
            var existingCart = await _context.CartItems
                .FirstOrDefaultAsync(c => c.ProductId == productId && c.UserId == userId);

            if (existingCart == null)
            {
                _context.CartItems.Add(new CartItem
                {
                    UserId = userId,
                    ProductId = productId,
                    Quantity = 1
                });
            }
            else
            {
                existingCart.Quantity++;
                _context.CartItems.Update(existingCart);
            }

            // Remove wishlist item if present
            var wishlistItem = await _context.WishlistItems
                .FirstOrDefaultAsync(w => w.ProductId == productId && w.UserId == userId);

            if (wishlistItem != null)
                _context.WishlistItems.Remove(wishlistItem);

            await _context.SaveChangesAsync();

            return Json(new { success = true });
        }
    }
}
