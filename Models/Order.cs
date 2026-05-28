using System.ComponentModel.DataAnnotations.Schema;

namespace LDMS_Final.Models
{
    public class Order
    {
        public int Id { get; set; }

        public string BuyerId { get; set; } = string.Empty;
        public ApplicationUser Buyer { get; set; } = null!;

        public string OrderNumber { get; set; } = string.Empty;

        [Column(TypeName = "decimal(10,2)")]
        public decimal TotalAmount { get; set; }

        [Column(TypeName = "decimal(10,2)")]
        public decimal ShippingFee { get; set; }

        public string PaymentMethod { get; set; } = string.Empty;
        public string Status { get; set; } = OrderStatus.Pending;

        // Delivery snapshot
        public string DeliveryFullName      { get; set; } = string.Empty;
        public string DeliveryContactNumber { get; set; } = string.Empty;
        public string DeliveryAddress       { get; set; } = string.Empty;
        public string? DeliveryNote         { get; set; }
        public double? DeliveryLatitude     { get; set; }
        public double? DeliveryLongitude    { get; set; }

        public string? QrCodePath           { get; set; }
        public string? ShippingLabelPath    { get; set; }

        public string CreatedByAdminId      { get; set; } = string.Empty;
        public DateTime CreatedAt           { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedAt          { get; set; }

        public List<OrderItem>         Items          { get; set; } = new();
        public List<OrderStatusLog>    StatusLogs     { get; set; } = new();
        public OrderRouteAssignment?   RouteAssignment { get; set; }
    }

    public static class OrderStatus
    {
        // ── Core statuses ────────────────────────────────────────────────
        public const string Pending        = "Pending";
        public const string Approved       = "Order approved - Preparing to ship";
        public const string InTransit      = "In transit";
        public const string OutForDelivery = "Out for delivery";
        public const string Delivered      = "Completed";
        public const string Cancelled      = "Cancelled";

        // ── Buyer-visible In-Transit scan notes ───────────────────────────
        public static string ScanLeftWarehouse(string driverLabel)
            => $"Package left the Warehouse in Davao City.";

        public static string ScanArrivedHub(string hubLabel, string driverLabel)
            => $"Package arrived at {hubLabel}.";

        public static string ScanLeftHub(string hubLabel, string driverLabel)
            => $"Package left the {hubLabel}.";

        public static string ScanOutForDelivery(string driverLabel)
            => $"Package is out for delivery. (Rider: {driverLabel})";
    }
}
