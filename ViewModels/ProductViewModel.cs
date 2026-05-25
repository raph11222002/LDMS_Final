using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http;

namespace LDMS_Final.ViewModels
{
    public class ProductViewModel
    {
        public int Id { get; set; }

        [Required]
        [Display(Name = "Product Name")]
        public string Name { get; set; } = string.Empty;

        [Required]
        [Display(Name = "Category")]
        public string Category { get; set; } = string.Empty;

        [Display(Name = "Description")]
        public string? Description { get; set; }

        [Required]
        [Display(Name = "Brand")]
        public string Brand { get; set; } = string.Empty;

        [Required]
        [Display(Name = "Price (₱)")]
        [Range(0.01, 999999.99)]
        public decimal Price { get; set; }

        [Required]
        [Display(Name = "Weight (kg)")]
        [Range(0.01, 999.99, ErrorMessage = "Weight must be between 0.01 and 999.99 kg")]
        public decimal Weight { get; set; }

        public bool IsActive { get; set; } = true;

        // Sizes — nullable, comma-separated when saved
        [Display(Name = "Available Sizes")]
        public List<string> Sizes { get; set; } = new();

        // Variants — each has a name and one image
        public List<ProductVariantViewModel> Variants { get; set; } = new();

        // Main product images (up to 5, no variant)
        [Display(Name = "Product Images (max 5)")]
        public List<IFormFile>? Images { get; set; }

        // Existing image paths for edit
        public string? ImagePath1 { get; set; }
        public string? ImagePath2 { get; set; }
        public string? ImagePath3 { get; set; }
        public string? ImagePath4 { get; set; }
        public string? ImagePath5 { get; set; }
    }

    public class ProductVariantViewModel
    {
        public string Name { get; set; } = string.Empty;        // e.g. "Red", "Ocean Blue"
        public IFormFile? Image { get; set; }
        public string? ExistingImagePath { get; set; }          // for edit
    }
}