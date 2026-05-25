using LDMS_Final.Data;
using LDMS_Final.Models;
using LDMS_Final.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace LDMS_Final.Controllers
{
    [Authorize(Roles = RoleNames.Buyer)]
    public class CartController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public CartController(ApplicationDbContext context, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        private string GetBuyerId() => _userManager.GetUserId(User)!;

        // ── Add to Cart ────────────────────────────────────────
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddToCart(int productId, string? variantName,
            string? selectedSize)
        {
            var buyerId = GetBuyerId();

            var product = await _context.Products.FindAsync(productId);
            if (product == null || !product.IsActive)
                return NotFound();

            // Check stock for this specific variant+size combo
            var stockQuery = _context.ProductStocks.Where(s => s.ProductId == productId);

            if (!string.IsNullOrEmpty(variantName))
                stockQuery = stockQuery.Where(s => s.VariantName == variantName);
            else
                stockQuery = stockQuery.Where(s => s.VariantName == null);

            if (!string.IsNullOrEmpty(selectedSize))
                stockQuery = stockQuery.Where(s => s.SelectedSize == selectedSize);
            else
                stockQuery = stockQuery.Where(s => s.SelectedSize == null);

            var stock = await stockQuery.FirstOrDefaultAsync();
            if (stock == null || stock.Quantity <= 0)
            {
                TempData["CartError"] = "This item is out of stock.";
                return RedirectToAction("ProductDetail", "BuyerHome", new { id = productId });
            }

            // Check if already in cart
            var existing = await _context.CartItems.FirstOrDefaultAsync(c =>
                c.BuyerId == buyerId &&
                c.ProductId == productId &&
                c.VariantName == variantName &&
                c.SelectedSize == selectedSize);

            if (existing != null)
            {
                if (existing.Quantity + 1 > stock.Quantity)
                {
                    TempData["CartError"] = "Not enough stock available.";
                    return RedirectToAction("ProductDetail", "BuyerHome", new { id = productId });
                }
                existing.Quantity++;
            }
            else
            {
                _context.CartItems.Add(new CartItem
                {
                    BuyerId = buyerId,
                    ProductId = productId,
                    VariantName = variantName,
                    SelectedSize = selectedSize,
                    Quantity = 1,
                    AddedAt = DateTime.UtcNow
                });
            }

            await _context.SaveChangesAsync();

            TempData["CartSuccess"] = "Item added to cart.";
            return RedirectToAction(nameof(Index));
        }

        // ── View Cart ──────────────────────────────────────────
        public async Task<IActionResult> Index()
        {
            var buyerId = GetBuyerId();

            var cartItems = await _context.CartItems
                .Include(c => c.Product)
                .Where(c => c.BuyerId == buyerId)
                .OrderBy(c => c.AddedAt)
                .ToListAsync();

            var items = new List<CartItemViewModel>();
            foreach (var c in cartItems)
            {
                var stockQuery = _context.ProductStocks.Where(s => s.ProductId == c.ProductId);
                if (!string.IsNullOrEmpty(c.VariantName))
                    stockQuery = stockQuery.Where(s => s.VariantName == c.VariantName);
                else
                    stockQuery = stockQuery.Where(s => s.VariantName == null);
                if (!string.IsNullOrEmpty(c.SelectedSize))
                    stockQuery = stockQuery.Where(s => s.SelectedSize == c.SelectedSize);
                else
                    stockQuery = stockQuery.Where(s => s.SelectedSize == null);

                var stock = await stockQuery.FirstOrDefaultAsync();

                items.Add(new CartItemViewModel
                {
                    CartItemId = c.Id,
                    ProductId = c.ProductId,
                    ProductName = c.Product.Name,
                    VariantName = c.VariantName,
                    SelectedSize = c.SelectedSize,
                    ImagePath = c.Product.ImagePath1,
                    UnitPrice = c.Product.Price,
                    Quantity = c.Quantity,
                    AvailableStock = stock?.Quantity ?? 0
                });
            }

            return View(items);
        }

        // ── Update Quantity ────────────────────────────────────
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateQuantity(int cartItemId, int quantity)
        {
            var buyerId = GetBuyerId();
            var item = await _context.CartItems
                .Include(c => c.Product)
                .FirstOrDefaultAsync(c => c.Id == cartItemId && c.BuyerId == buyerId);

            if (item == null) return NotFound();

            if (quantity <= 0)
            {
                _context.CartItems.Remove(item);
            }
            else
            {
                item.Quantity = quantity;
            }

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        // ── Remove Item ────────────────────────────────────────
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Remove(int cartItemId)
        {
            var buyerId = GetBuyerId();
            var item = await _context.CartItems
                .FirstOrDefaultAsync(c => c.Id == cartItemId && c.BuyerId == buyerId);

            if (item != null)
            {
                _context.CartItems.Remove(item);
                await _context.SaveChangesAsync();
            }

            return RedirectToAction(nameof(Index));
        }
    }
}