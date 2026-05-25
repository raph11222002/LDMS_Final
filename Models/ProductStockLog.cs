namespace LDMS_Final.Models
{
    public class ProductStockLog
    {
        public int Id { get; set; }

        public int ProductId { get; set; }
        public Product Product { get; set; } = null!;

        public string? VariantName { get; set; }
        public string? SelectedSize { get; set; }

        public int PreviousQuantity { get; set; }
        public int NewQuantity { get; set; }
        public int Change => NewQuantity - PreviousQuantity;

        public string UpdatedByUserId { get; set; } = string.Empty;
        public string UpdatedByName { get; set; } = string.Empty;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
        public string? Note { get; set; }
    }
}