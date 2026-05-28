namespace LDMS_Final.ViewModels
{
    public class BuyerProductCardViewModel
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Category { get; set; } = string.Empty;
        public string Brand { get; set; } = string.Empty;
        public decimal Price { get; set; }
        public string? ImagePath1 { get; set; }
        public int TotalStock { get; set; }
        public string StockStatus { get; set; } = string.Empty;
        public bool IsFavorited { get; set; }
    }

    public class BuyerHomeViewModel
    {
        public List<BuyerProductCardViewModel> Products { get; set; } = new();
        public List<string> Categories { get; set; } = new();
        public string? SearchQuery { get; set; }
        public string? SelectedCategory { get; set; }
    }

    public class BuyerProductDetailViewModel
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Category { get; set; } = string.Empty;
        public string Brand { get; set; } = string.Empty;
        public string? Description { get; set; }
        public decimal Price { get; set; }
        public string? Sizes { get; set; }
        public int TotalStock { get; set; }
        public string StockStatus { get; set; } = string.Empty;
        public bool IsFavorited { get; set; }
        public string? ImagePath1 { get; set; }
        public string? ImagePath2 { get; set; }
        public string? ImagePath3 { get; set; }
        public string? ImagePath4 { get; set; }
        public string? ImagePath5 { get; set; }
        public List<BuyerVariantViewModel> Variants { get; set; } = new();
        public List<BuyerSizeViewModel> SizeOptions { get; set; } = new();
        public List<BuyerStockViewModel> Stocks { get; set; } = new();
    }

    public class BuyerVariantViewModel
    {
        public string Name { get; set; } = string.Empty;
        public string? ImagePath { get; set; }
        public int Stock { get; set; }
    }

    public class BuyerSizeViewModel
    {
        public string? Sizes { get; set; }
        public int Stock { get; set; }
    }

    public class BuyerFavoritesViewModel
    {
        public List<BuyerProductCardViewModel> Favorites { get; set; } = new();
    }

    public class BuyerStockViewModel
    {
        public string? VariantName { get; set; }
        public string? SelectedSize { get; set; }
        public int Stock { get; set; }
    }
}