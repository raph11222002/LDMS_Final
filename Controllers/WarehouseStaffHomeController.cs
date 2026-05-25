using LDMS_Final.Models;
using LDMS_Final.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using LDMS_Final.Data;

namespace LDMS_Final.Controllers
{
    [Authorize(Roles = RoleNames.WarehouseStaff)]
    public class WarehouseStaffHomeController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public WarehouseStaffHomeController(
            ApplicationDbContext context,
            UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        private async Task<string?> GetParentAdminIdAsync()
        {
            var user = await _userManager.GetUserAsync(User);
            return user?.ParentAdminId;
        }

        // ── Dashboard ──────────────────────────────────────────
        public async Task<IActionResult> Index()
        {
            var adminId = await GetParentAdminIdAsync();
            if (string.IsNullOrEmpty(adminId)) return Forbid();

            var products = await _context.Products
                .Where(x => x.CreatedByAdminId == adminId && x.IsActive)
                .ToListAsync();

            var productIds = products.Select(p => p.Id).ToList();

            var stocks = await _context.ProductStocks
                .Where(x => productIds.Contains(x.ProductId))
                .ToListAsync();

            int inStock = 0, lowStock = 0, outOfStock = 0;

            foreach (var product in products)
            {
                var total = stocks.Where(s => s.ProductId == product.Id).Sum(s => s.Quantity);
                if (total == 0) outOfStock++;
                else if (total <= 5) lowStock++;
                else inStock++;
            }

            var model = new WarehouseStaffDashboardViewModel
            {
                TotalProductCount = products.Count,
                InStockCount = inStock,
                LowStockCount = lowStock,
                OutOfStockCount = outOfStock
            };

            return View(model);
        }

        // ── Product List ───────────────────────────────────────
        public async Task<IActionResult> Products()
        {
            var adminId = await GetParentAdminIdAsync();
            if (string.IsNullOrEmpty(adminId)) return Forbid();

            var products = await _context.Products
                .Where(x => x.CreatedByAdminId == adminId && x.IsActive)
                .OrderBy(x => x.Name)
                .ToListAsync();

            var productIds = products.Select(p => p.Id).ToList();

            var stocks = await _context.ProductStocks
                .Where(x => productIds.Contains(x.ProductId))
                .ToListAsync();

            var model = products.Select(p =>
            {
                var total = stocks.Where(s => s.ProductId == p.Id).Sum(s => s.Quantity);
                var status = total == 0 ? "OutOfStock" : total <= 5 ? "LowStock" : "InStock";

                return new ProductStockListViewModel
                {
                    ProductId = p.Id,
                    Name = p.Name,
                    Category = p.Category,
                    Brand = p.Brand,
                    ImagePath1 = p.ImagePath1,
                    Sizes = p.Sizes,
                    IsActive = p.IsActive,
                    TotalStock = total,
                    StockStatus = status
                };
            }).ToList();

            return View(model);
        }

        // ── Product Detail + Stock ─────────────────────────────
        public async Task<IActionResult> ProductDetail(int id)
        {
            var adminId = await GetParentAdminIdAsync();
            if (string.IsNullOrEmpty(adminId)) return Forbid();

            var product = await _context.Products.FindAsync(id);
            if (product == null || product.CreatedByAdminId != adminId)
                return NotFound();

            var variants = new List<ProductVariant>();
            if (!string.IsNullOrEmpty(product.VariantsJson))
                variants = System.Text.Json.JsonSerializer
                    .Deserialize<List<ProductVariant>>(product.VariantsJson) ?? new();

            var sizes = string.IsNullOrEmpty(product.Sizes)
                ? new List<string>()
                : product.Sizes.Split(',').ToList();

            var existingStocks = await _context.ProductStocks
                .Where(x => x.ProductId == id)
                .ToListAsync();

            var stockEntries = new List<VariantStockViewModel>();
            var currentUserId = _userManager.GetUserId(User)!;

            // Build all expected combinations
            if (variants.Any() && sizes.Any())
            {
                // variant + size combinations
                foreach (var variant in variants)
                {
                    foreach (var size in sizes)
                    {
                        var stock = existingStocks.FirstOrDefault(s =>
                            s.VariantName == variant.Name && s.SelectedSize == size);

                        if (stock == null)
                        {
                            stock = new ProductStock
                            {
                                ProductId = id,
                                VariantName = variant.Name,
                                SelectedSize = size,
                                Quantity = 0,
                                UpdatedByUserId = currentUserId,
                                UpdatedAt = DateTime.UtcNow
                            };
                            _context.ProductStocks.Add(stock);
                        }

                        stockEntries.Add(new VariantStockViewModel
                        {
                            StockId = stock.Id,
                            VariantName = variant.Name,
                            VariantImagePath = variant.ImagePath,
                            SelectedSize = size,
                            Quantity = stock.Quantity
                        });
                    }
                }
            }
            else if (variants.Any())
            {
                // variant only, no sizes
                foreach (var variant in variants)
                {
                    var stock = existingStocks.FirstOrDefault(s =>
                        s.VariantName == variant.Name && s.SelectedSize == null);

                    if (stock == null)
                    {
                        stock = new ProductStock
                        {
                            ProductId = id,
                            VariantName = variant.Name,
                            SelectedSize = null,
                            Quantity = 0,
                            UpdatedByUserId = currentUserId,
                            UpdatedAt = DateTime.UtcNow
                        };
                        _context.ProductStocks.Add(stock);
                    }

                    stockEntries.Add(new VariantStockViewModel
                    {
                        StockId = stock.Id,
                        VariantName = variant.Name,
                        VariantImagePath = variant.ImagePath,
                        SelectedSize = null,
                        Quantity = stock.Quantity
                    });
                }
            }
            else if (sizes.Any())
            {
                // size only, no variants
                foreach (var size in sizes)
                {
                    var stock = existingStocks.FirstOrDefault(s =>
                        s.VariantName == null && s.SelectedSize == size);

                    if (stock == null)
                    {
                        stock = new ProductStock
                        {
                            ProductId = id,
                            VariantName = null,
                            SelectedSize = size,
                            Quantity = 0,
                            UpdatedByUserId = currentUserId,
                            UpdatedAt = DateTime.UtcNow
                        };
                        _context.ProductStocks.Add(stock);
                    }

                    stockEntries.Add(new VariantStockViewModel
                    {
                        StockId = stock.Id,
                        VariantName = null,
                        SelectedSize = size,
                        Quantity = stock.Quantity
                    });
                }
            }
            else
            {
                // No variants, no sizes — single stock
                var stock = existingStocks.FirstOrDefault(s =>
                    s.VariantName == null && s.SelectedSize == null);

                if (stock == null)
                {
                    stock = new ProductStock
                    {
                        ProductId = id,
                        VariantName = null,
                        SelectedSize = null,
                        Quantity = 0,
                        UpdatedByUserId = currentUserId,
                        UpdatedAt = DateTime.UtcNow
                    };
                    _context.ProductStocks.Add(stock);
                }

                stockEntries.Add(new VariantStockViewModel
                {
                    StockId = stock.Id,
                    VariantName = null,
                    SelectedSize = null,
                    Quantity = stock.Quantity
                });
            }

            await _context.SaveChangesAsync();

            var logs = await _context.ProductStockLogs
                .Where(x => x.ProductId == id)
                .OrderByDescending(x => x.UpdatedAt)
                .Take(30)
                .ToListAsync();

            var model = new ProductStockDetailViewModel
            {
                ProductId = product.Id,
                Name = product.Name,
                Category = product.Category,
                Brand = product.Brand,
                Description = product.Description,
                Sizes = product.Sizes,
                Price = product.Price,
                ImagePath1 = product.ImagePath1,
                ImagePath2 = product.ImagePath2,
                ImagePath3 = product.ImagePath3,
                ImagePath4 = product.ImagePath4,
                ImagePath5 = product.ImagePath5,
                StockEntries = stockEntries,
                StockLogs = logs
            };

            return View(model);
        }

        // ── Update Stock ───────────────────────────────────────
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateStock(UpdateStockViewModel model)
        {
            var adminId = await GetParentAdminIdAsync();
            if (string.IsNullOrEmpty(adminId)) return Forbid();

            var product = await _context.Products.FindAsync(model.ProductId);
            if (product == null || product.CreatedByAdminId != adminId)
                return NotFound();

            var currentUser = await _userManager.GetUserAsync(User);

            foreach (var input in model.Stocks)
            {
                var stock = await _context.ProductStocks.FindAsync(input.StockId);
                if (stock == null || stock.ProductId != model.ProductId) continue;

                var previous = stock.Quantity;
                stock.Quantity = Math.Max(0, input.Quantity);
                stock.UpdatedByUserId = currentUser!.Id;
                stock.UpdatedAt = DateTime.UtcNow;

                if (previous != stock.Quantity)
                {
                    _context.ProductStockLogs.Add(new ProductStockLog
                    {
                        ProductId = model.ProductId,
                        VariantName = stock.VariantName,
                        SelectedSize = stock.SelectedSize,
                        PreviousQuantity = previous,
                        NewQuantity = stock.Quantity,
                        UpdatedByUserId = currentUser.Id,
                        UpdatedByName = currentUser.FullName ?? currentUser.UserName ?? "Unknown",
                        UpdatedAt = DateTime.UtcNow,
                        Note = input.Note
                    });
                }
            }

            await _context.SaveChangesAsync();

            TempData["Success"] = "Stock updated successfully.";
            return RedirectToAction(nameof(ProductDetail), new { id = model.ProductId });
        }
    }
}