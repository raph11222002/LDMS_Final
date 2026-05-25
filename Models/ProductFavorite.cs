namespace LDMS_Final.Models
{
    public class ProductFavorite
    {
        public int Id { get; set; }
        public string BuyerId { get; set; } = string.Empty;
        public int ProductId { get; set; }
        public Product Product { get; set; } = null!;
        public DateTime AddedAt { get; set; } = DateTime.UtcNow;
    }
}