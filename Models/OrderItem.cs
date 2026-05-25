using System.ComponentModel.DataAnnotations.Schema;

namespace LDMS_Final.Models
{
    public class OrderItem
    {
        public int Id { get; set; }

        public int OrderId { get; set; }
        public Order Order { get; set; } = null!;

        public int ProductId { get; set; }
        public Product Product { get; set; } = null!;

        public string ProductName { get; set; } = string.Empty;  // snapshot
        public string? VariantName { get; set; }
        public string? SelectedSize { get; set; }

        [Column(TypeName = "decimal(10,2)")]
        public decimal UnitPrice { get; set; }

        public int Quantity { get; set; }

        [Column(TypeName = "decimal(10,2)")]
        public decimal Subtotal => UnitPrice * Quantity;
    }
}