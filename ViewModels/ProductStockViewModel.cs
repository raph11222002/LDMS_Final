using LDMS_Final.Models;

namespace LDMS_Final.ViewModels
{
    public class ProductStockListViewModel
    {
        public int ProductId { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Category { get; set; } = string.Empty;
        public string Brand { get; set; } = string.Empty;
        public string? ImagePath1 { get; set; }
        public string? Sizes { get; set; }
        public bool IsActive { get; set; }
        public int TotalStock { get; set; }
        public string StockStatus { get; set; } = string.Empty;
    }

    public class ProductStockDetailViewModel
    {
        public int ProductId { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Category { get; set; } = string.Empty;
        public string Brand { get; set; } = string.Empty;
        public string? Description { get; set; }
        public string? Sizes { get; set; }
        public decimal Price { get; set; }
        public string? ImagePath1 { get; set; }
        public string? ImagePath2 { get; set; }
        public string? ImagePath3 { get; set; }
        public string? ImagePath4 { get; set; }
        public string? ImagePath5 { get; set; }

        // Grouped: each entry is one unique variant+size combination
        public List<VariantStockViewModel> StockEntries { get; set; } = new();
        public List<ProductStockLog> StockLogs { get; set; } = new();
    }

    public class VariantStockViewModel
    {
        public int StockId { get; set; }
        public string? VariantName { get; set; }
        public string? VariantImagePath { get; set; }
        public string? SelectedSize { get; set; }
        public int Quantity { get; set; }
    }

    public class UpdateStockViewModel
    {
        public int ProductId { get; set; }
        public List<VariantStockInputViewModel> Stocks { get; set; } = new();
    }

    public class VariantStockInputViewModel
    {
        public int StockId { get; set; }
        public string? VariantName { get; set; }
        public string? SelectedSize { get; set; }
        public int Quantity { get; set; }
        public string? Note { get; set; }
    }
}