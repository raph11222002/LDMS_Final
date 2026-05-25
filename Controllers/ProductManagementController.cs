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
    [Authorize(Roles = RoleNames.Admin)]
    public class ProductManagementController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IWebHostEnvironment _env;

        public ProductManagementController(
            ApplicationDbContext context,
            UserManager<ApplicationUser> userManager,
            IWebHostEnvironment env)
        {
            _context = context;
            _userManager = userManager;
            _env = env;
        }

        // ── Index ──────────────────────────────────────────────
        public async Task<IActionResult> Index()
        {
            var adminId = _userManager.GetUserId(User);
            var products = await _context.Products
                .Where(x => x.CreatedByAdminId == adminId)
                .OrderByDescending(x => x.CreatedAt)
                .ToListAsync();

            return View(products);
        }

        // ── Create ─────────────────────────────────────────────
        [HttpGet]
        public IActionResult Create() => View(new ProductViewModel());

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(ProductViewModel model)
        {
            if (!ModelState.IsValid) return View(model);

            var adminId = _userManager.GetUserId(User);
            var imagePaths = await SaveMainImagesAsync(model.Images);
            var variantsJson = await SaveVariantsAsync(model.Variants);

            var product = new Product
            {
                Name = model.Name.Trim(),
                Category = model.Category.Trim(),
                Description = model.Description?.Trim(),
                Brand = model.Brand.Trim(),
                Sizes = model.Sizes.Any()
                    ? string.Join(",", model.Sizes.Where(s => !string.IsNullOrWhiteSpace(s)))
                    : null,
                VariantsJson = variantsJson,
                Price = model.Price,
                Weight = model.Weight,
                IsActive = true,
                CreatedByAdminId = adminId!,
                CreatedAt = DateTime.UtcNow,
                ImagePath1 = imagePaths.ElementAtOrDefault(0),
                ImagePath2 = imagePaths.ElementAtOrDefault(1),
                ImagePath3 = imagePaths.ElementAtOrDefault(2),
                ImagePath4 = imagePaths.ElementAtOrDefault(3),
                ImagePath5 = imagePaths.ElementAtOrDefault(4)
            };

            _context.Products.Add(product);
            await _context.SaveChangesAsync();

            TempData["Success"] = $"Product '{product.Name}' added successfully.";
            return RedirectToAction(nameof(Index));
        }

        // ── Edit ───────────────────────────────────────────────
        [HttpGet]
        public async Task<IActionResult> Edit(int id)
        {
            var adminId = _userManager.GetUserId(User);
            var product = await _context.Products.FindAsync(id);

            if (product == null || product.CreatedByAdminId != adminId)
                return NotFound();

            var existingVariants = string.IsNullOrEmpty(product.VariantsJson)
                ? new List<ProductVariantViewModel>()
                : JsonSerializer.Deserialize<List<ProductVariant>>(product.VariantsJson)!
                    .Select(v => new ProductVariantViewModel
                    {
                        Name = v.Name,
                        ExistingImagePath = v.ImagePath
                    }).ToList();

            var model = new ProductViewModel
            {
                Id = product.Id,
                Name = product.Name,
                Category = product.Category,
                Description = product.Description,
                Brand = product.Brand,
                Sizes = string.IsNullOrEmpty(product.Sizes)
                    ? new List<string>()
                    : product.Sizes.Split(',').ToList(),
                Variants = existingVariants,
                Price = product.Price,
                Weight = product.Weight,
                IsActive = product.IsActive,
                ImagePath1 = product.ImagePath1,
                ImagePath2 = product.ImagePath2,
                ImagePath3 = product.ImagePath3,
                ImagePath4 = product.ImagePath4,
                ImagePath5 = product.ImagePath5
            };

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(ProductViewModel model)
        {
            if (!ModelState.IsValid) return View(model);

            var adminId = _userManager.GetUserId(User);
            var product = await _context.Products.FindAsync(model.Id);

            if (product == null || product.CreatedByAdminId != adminId)
                return NotFound();

            var newImagePaths = await SaveMainImagesAsync(model.Images);
            var variantsJson = await SaveVariantsAsync(model.Variants);

            product.Name = model.Name.Trim();
            product.Category = model.Category.Trim();
            product.Description = model.Description?.Trim();
            product.Brand = model.Brand.Trim();
            product.Sizes = model.Sizes.Any()
                ? string.Join(",", model.Sizes.Where(s => !string.IsNullOrWhiteSpace(s)))
                : null;
            product.VariantsJson = variantsJson;
            product.Price = model.Price;
            product.Weight = model.Weight;
            product.IsActive = model.IsActive;
            product.UpdatedAt = DateTime.UtcNow;
            product.ImagePath1 = newImagePaths.ElementAtOrDefault(0) ?? model.ImagePath1;
            product.ImagePath2 = newImagePaths.ElementAtOrDefault(1) ?? model.ImagePath2;
            product.ImagePath3 = newImagePaths.ElementAtOrDefault(2) ?? model.ImagePath3;
            product.ImagePath4 = newImagePaths.ElementAtOrDefault(3) ?? model.ImagePath4;
            product.ImagePath5 = newImagePaths.ElementAtOrDefault(4) ?? model.ImagePath5;

            await _context.SaveChangesAsync();

            TempData["Success"] = $"Product '{product.Name}' updated successfully.";
            return RedirectToAction(nameof(Index));
        }

        // ── Toggle Status ──────────────────────────────────────
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ToggleStatus(int id)
        {
            var adminId = _userManager.GetUserId(User);
            var product = await _context.Products.FindAsync(id);

            if (product == null || product.CreatedByAdminId != adminId)
                return NotFound();

            product.IsActive = !product.IsActive;
            product.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            TempData["Success"] = $"'{product.Name}' has been {(product.IsActive ? "activated" : "deactivated")}.";
            return RedirectToAction(nameof(Index));
        }

        // ── Delete ─────────────────────────────────────────────
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            var adminId = _userManager.GetUserId(User);
            var product = await _context.Products.FindAsync(id);

            if (product == null || product.CreatedByAdminId != adminId)
                return NotFound();

            _context.Products.Remove(product);
            await _context.SaveChangesAsync();

            TempData["Success"] = "Product deleted successfully.";
            return RedirectToAction(nameof(Index));
        }

        // ── Helpers ────────────────────────────────────────────
        private async Task<List<string>> SaveMainImagesAsync(List<IFormFile>? images)
        {
            var paths = new List<string>();
            if (images == null || !images.Any()) return paths;

            var folder = Path.Combine(_env.WebRootPath, "images", "products");
            Directory.CreateDirectory(folder);

            var allowed = new[] { ".jpg", ".jpeg", ".png", ".webp" };

            foreach (var image in images.Take(5))
            {
                var ext = Path.GetExtension(image.FileName).ToLowerInvariant();
                if (!allowed.Contains(ext) || image.Length == 0) continue;

                var fileName = $"{Guid.NewGuid()}{ext}";
                var filePath = Path.Combine(folder, fileName);
                using var stream = new FileStream(filePath, FileMode.Create);
                await image.CopyToAsync(stream);
                paths.Add($"/images/products/{fileName}");
            }

            return paths;
        }

        private async Task<string?> SaveVariantsAsync(List<ProductVariantViewModel> variants)
        {
            if (!variants.Any()) return null;

            var folder = Path.Combine(_env.WebRootPath, "images", "variants");
            Directory.CreateDirectory(folder);

            var allowed = new[] { ".jpg", ".jpeg", ".png", ".webp" };
            var savedVariants = new List<ProductVariant>();

            foreach (var variant in variants.Where(v => !string.IsNullOrWhiteSpace(v.Name)))
            {
                string? imagePath = variant.ExistingImagePath;

                if (variant.Image != null && variant.Image.Length > 0)
                {
                    var ext = Path.GetExtension(variant.Image.FileName).ToLowerInvariant();
                    if (allowed.Contains(ext))
                    {
                        var fileName = $"{Guid.NewGuid()}{ext}";
                        var filePath = Path.Combine(folder, fileName);
                        using var stream = new FileStream(filePath, FileMode.Create);
                        await variant.Image.CopyToAsync(stream);
                        imagePath = $"/images/variants/{fileName}";
                    }
                }

                savedVariants.Add(new ProductVariant
                {
                    Name = variant.Name.Trim(),
                    ImagePath = imagePath
                });
            }

            return savedVariants.Any()
                ? JsonSerializer.Serialize(savedVariants)
                : null;
        }
    }
}