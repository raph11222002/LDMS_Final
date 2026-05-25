using System.ComponentModel.DataAnnotations;

namespace LDMS_Final.ViewModels
{
    public class CartItemViewModel
    {
        public int CartItemId { get; set; }
        public int ProductId { get; set; }
        public string ProductName { get; set; } = string.Empty;
        public string? VariantName { get; set; }
        public string? SelectedSize { get; set; }
        public string? ImagePath { get; set; }
        public decimal UnitPrice { get; set; }
        public decimal Weight { get; set; }
        public int Quantity { get; set; }
        public int AvailableStock { get; set; }
        public decimal Subtotal => UnitPrice * Quantity;
    }

    public class CheckoutViewModel
    {
        public List<CartItemViewModel> Items { get; set; } = new();
        public decimal TotalAmount => Items.Sum(x => x.Subtotal);
        public decimal ShippingFee { get; set; }
        public decimal GrandTotal => TotalAmount + ShippingFee;

        [Required]
        [Display(Name = "Full Name")]
        public string DeliveryFullName { get; set; } = string.Empty;

        [Required]
        [Display(Name = "Contact Number")]
        public string DeliveryContactNumber { get; set; } = string.Empty;

        [Required]
        [Display(Name = "Delivery Address")]
        public string DeliveryAddress { get; set; } = string.Empty;

        [Display(Name = "Delivery Note")]
        public string? DeliveryNote { get; set; }

        public double? DeliveryLatitude { get; set; }
        public double? DeliveryLongitude { get; set; }

        [Required]
        [Display(Name = "Payment Method")]
        public string PaymentMethod { get; set; } = string.Empty;
    }

    public class OrderStatusHistoryViewModel
    {
        public string Status { get; set; } = string.Empty;
        public string? Note { get; set; }
        public string? UpdatedByName { get; set; }
        public DateTime CreatedAt { get; set; }
    }

    public class BuyerOrderListViewModel
    {
        public int OrderId { get; set; }
        public string OrderNumber { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public decimal TotalAmount { get; set; }
        public string PaymentMethod { get; set; } = string.Empty;
        public int ItemCount { get; set; }
        public string? FirstProductImage { get; set; }
        public DateTime CreatedAt { get; set; }
    }

    public class BuyerOrderDetailViewModel
    {
        public int OrderId { get; set; }
        public string OrderNumber { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public string PaymentMethod { get; set; } = string.Empty;
        public decimal TotalAmount { get; set; }
        public string DeliveryFullName { get; set; } = string.Empty;
        public string DeliveryContactNumber { get; set; } = string.Empty;
        public string DeliveryAddress { get; set; } = string.Empty;
        public string? DeliveryNote { get; set; }
        public double? DeliveryLatitude { get; set; }
        public double? DeliveryLongitude { get; set; }
        public DateTime CreatedAt { get; set; }
        public List<CartItemViewModel> Items { get; set; } = new();
        public List<OrderStatusHistoryViewModel> StatusHistory { get; set; } = new();
    }
}