using System.ComponentModel.DataAnnotations;

namespace LDMS_Final.Models
{
    public class ProductStock
    {
        public int Id { get; set; }

        public int ProductId { get; set; }
        public Product Product { get; set; } = null!;

        // Null = no variant
        public string? VariantName { get; set; }

        // Null = no size (e.g. bag)
        public string? SelectedSize { get; set; }

        public int Quantity { get; set; } = 0;

        public string UpdatedByUserId { get; set; } = string.Empty;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    }
}