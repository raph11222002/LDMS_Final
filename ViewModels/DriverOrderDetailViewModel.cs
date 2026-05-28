// ViewModels/DriverOrderDetailViewModel.cs
using LDMS_Final.Models;

namespace LDMS_Final.ViewModels
{
    public class DriverOrderDetailViewModel
    {
        public Order Order { get; set; } = null!;
        public OrderHubStop MyStop { get; set; } = null!;
        public bool IsMotoDriver { get; set; }
        public string MapboxToken { get; set; } = string.Empty;

        // Last hub coordinates (for route display)
        public double LastHubLat { get; set; }
        public double LastHubLng { get; set; }
        public string LastHubLabel { get; set; } = string.Empty;

        // Buyer destination
        public double? DestLat { get; set; }
        public double? DestLng { get; set; }
        public string DestAddress { get; set; } = string.Empty;

        // Delivery report (if completed)
        public RiderDeliveredReport? DeliveryReport { get; set; }
        public string? StaticRouteMapUrl { get; set; }
    }
}