using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace LDMS_Final.Models
{
    public class Product
    {
        public int Id { get; set; }

        [Required]
        public string Name { get; set; } = string.Empty;

        [Required]
        public string Category { get; set; } = string.Empty;

        public string? Description { get; set; }

        [Required]
        public string Brand { get; set; } = string.Empty;

        // Nullable — comma-separated e.g. "S,M,L,XL" or null for backpack
        public string? Sizes { get; set; }

        // JSON array of {Name, ImagePath} stored as string
        public string? VariantsJson { get; set; }

        [Column(TypeName = "decimal(10,2)")]
        public decimal Price { get; set; }

        [Column(TypeName = "decimal(10,2)")]
        public decimal Weight { get; set; }

        public bool IsActive { get; set; } = true;

        public string CreatedByAdminId { get; set; } = string.Empty;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public DateTime? UpdatedAt { get; set; }

        // Main showcase images (up to 5)
        public string? ImagePath1 { get; set; }
        public string? ImagePath2 { get; set; }
        public string? ImagePath3 { get; set; }
        public string? ImagePath4 { get; set; }
        public string? ImagePath5 { get; set; }
    }
}