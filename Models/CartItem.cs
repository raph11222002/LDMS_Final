namespace LDMS_Final.Models
{
    public class CartItem
    {
        public int Id { get; set; }
        public string BuyerId { get; set; } = string.Empty;
        public int ProductId { get; set; }
        public Product Product { get; set; } = null!;
        public string? VariantName { get; set; }
        public string? SelectedSize { get; set; }
        public int Quantity { get; set; } = 1;
        public DateTime AddedAt { get; set; } = DateTime.UtcNow;
    }
}