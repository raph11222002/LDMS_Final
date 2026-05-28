using LDMS_Final.Data;
using LDMS_Final.Models;
using LDMS_Final.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;

namespace LDMS_Final.Controllers
{
    //[Authorize(Roles = RoleNames.Buyer)]
    public class BuyerHomeController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public BuyerHomeController(
            ApplicationDbContext context,
            UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        private string GetBuyerId() => _userManager.GetUserId(User)!;

        private string GetStockStatus(int total) =>
            total == 0 ? "OutOfStock" : total <= 5 ? "LowStock" : "InStock";

        private async Task<int> GetProductTotalStockAsync(int productId)
        {
            return await _context.ProductStocks
                .Where(s => s.ProductId == productId)
                .SumAsync(s => s.Quantity);
        }

        // ── Index ──────────────────────────────────────────────
        public async Task<IActionResult> Index(string? search, string? category)
        {
            var buyerId = User.Identity?.IsAuthenticated == true ? GetBuyerId() : null;

            var query = _context.Products
                .Where(p => p.IsActive)
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(search))
                query = query.Where(p =>
                    p.Name.Contains(search) ||
                    p.Brand.Contains(search) ||
                    p.Category.Contains(search));

            if (!string.IsNullOrWhiteSpace(category))
                query = query.Where(p => p.Category == category);

            var products = await query.OrderBy(p => p.Category).ThenBy(p => p.Name).ToListAsync();

            var allCategories = await _context.Products
                .Where(p => p.IsActive)
                .Select(p => p.Category)
                .Distinct()
                .OrderBy(c => c)
                .ToListAsync();

            List<int> favoriteIds = new();
            if (buyerId != null)
            {
                favoriteIds = await _context.ProductFavorites
                    .Where(f => f.BuyerId == buyerId)
                    .Select(f => f.ProductId)
                    .ToListAsync();
            }

            var productCards = new List<BuyerProductCardViewModel>();
            foreach (var p in products)
            {
                var total = await GetProductTotalStockAsync(p.Id);
                productCards.Add(new BuyerProductCardViewModel
                {
                    Id = p.Id,
                    Name = p.Name,
                    Category = p.Category,
                    Brand = p.Brand,
                    Price = p.Price,
                    ImagePath1 = p.ImagePath1,
                    TotalStock = total,
                    StockStatus = GetStockStatus(total),
                    IsFavorited = favoriteIds.Contains(p.Id)
                });
            }

            var model = new BuyerHomeViewModel
            {
                Products = productCards,
                Categories = allCategories,
                SearchQuery = search,
                SelectedCategory = category
            };

            return View(model);
        }

        // ── Product Detail ─────────────────────────────────────
        public async Task<IActionResult> ProductDetail(int id)
        {
            var buyerId = GetBuyerId();

            var product = await _context.Products.FindAsync(id);
            if (product == null || !product.IsActive)
                return NotFound();

            var isFavorited = await _context.ProductFavorites
                .AnyAsync(f => f.BuyerId == buyerId && f.ProductId == id);

            var stockRecords = await _context.ProductStocks
                .Where(s => s.ProductId == id)
                .ToListAsync();

            var variants = new List<BuyerVariantViewModel>();

            if (!string.IsNullOrEmpty(product.VariantsJson))
            {
                var parsedVariants = JsonSerializer.Deserialize<List<ProductVariant>>(product.VariantsJson) ?? new();

                foreach (var v in parsedVariants)
                {
                    var variantStock = stockRecords
                        .Where(s => string.Equals(s.VariantName?.Trim(), v.Name.Trim(), StringComparison.OrdinalIgnoreCase))
                        .Sum(s => s.Quantity);

                    variants.Add(new BuyerVariantViewModel
                    {
                        Name = v.Name,
                        ImagePath = v.ImagePath,
                        Stock = variantStock
                    });
                }
            }

            var totalStock = stockRecords.Sum(s => s.Quantity);

            var model = new BuyerProductDetailViewModel
            {
                Id = product.Id,
                Name = product.Name,
                Category = product.Category,
                Brand = product.Brand,
                Description = product.Description,
                Price = product.Price,
                Sizes = product.Sizes,
                TotalStock = totalStock,
                StockStatus = GetStockStatus(totalStock),
                IsFavorited = isFavorited,
                ImagePath1 = product.ImagePath1,
                ImagePath2 = product.ImagePath2,
                ImagePath3 = product.ImagePath3,
                ImagePath4 = product.ImagePath4,
                ImagePath5 = product.ImagePath5,
                Variants = variants,
                SizeOptions = stockRecords
                    .Where(s => !string.IsNullOrEmpty(s.SelectedSize))
                    .GroupBy(s => s.SelectedSize!.Trim(), StringComparer.OrdinalIgnoreCase)
                    .Select(g => new BuyerSizeViewModel
                    {
                        Sizes = g.Key,
                        Stock = g.Sum(s => s.Quantity)
                    })
                    .ToList(),
                Stocks = stockRecords
                    .Select(s => new BuyerStockViewModel
                    {
                        VariantName = s.VariantName,
                        SelectedSize = s.SelectedSize,
                        Stock = s.Quantity
                    })
                    .ToList()
            };

            return View(model);
        }

        [Authorize(Roles = RoleNames.Buyer)]
        // ── Favorites ──────────────────────────────────────────
        public async Task<IActionResult> Favorites()
        {
            var buyerId = GetBuyerId();

            var favorites = await _context.ProductFavorites
                .Include(f => f.Product)
                .Where(f => f.BuyerId == buyerId && f.Product.IsActive)
                .OrderByDescending(f => f.AddedAt)
                .ToListAsync();

            var favoriteCards = new List<BuyerProductCardViewModel>();
            foreach (var f in favorites)
            {
                var total = await GetProductTotalStockAsync(f.ProductId);
                favoriteCards.Add(new BuyerProductCardViewModel
                {
                    Id = f.Product.Id,
                    Name = f.Product.Name,
                    Category = f.Product.Category,
                    Brand = f.Product.Brand,
                    Price = f.Product.Price,
                    ImagePath1 = f.Product.ImagePath1,
                    TotalStock = total,
                    StockStatus = GetStockStatus(total),
                    IsFavorited = true
                });
            }

            return View(new BuyerFavoritesViewModel { Favorites = favoriteCards });
        }

        // ── Toggle Favorite ────────────────────────────────────
        [HttpPost]
        [Authorize(Roles = RoleNames.Buyer)]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ToggleFavorite(int productId, string? returnUrl)
        {
            var buyerId = GetBuyerId();

            var existing = await _context.ProductFavorites
                .FirstOrDefaultAsync(f => f.BuyerId == buyerId && f.ProductId == productId);

            if (existing != null)
                _context.ProductFavorites.Remove(existing);
            else
                _context.ProductFavorites.Add(new ProductFavorite
                {
                    BuyerId = buyerId,
                    ProductId = productId,
                    AddedAt = DateTime.UtcNow
                });

            await _context.SaveChangesAsync();

            if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
                return Redirect(returnUrl);

            return RedirectToAction(nameof(Index));
        }
    }
}