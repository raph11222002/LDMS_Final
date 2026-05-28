// Models/RiderDeliveredReport.cs
namespace LDMS_Final.Models
{
    public class RiderDeliveredReport
    {
        public int Id { get; set; }

        public int OrderId { get; set; }
        public Order Order { get; set; } = null!;

        public int HubStopId { get; set; }
        public OrderHubStop HubStop { get; set; } = null!;

        public string DriverId { get; set; } = string.Empty;
        public ApplicationUser Driver { get; set; } = null!;

        public string PhotoPath { get; set; } = string.Empty;

        public string? Notes { get; set; }

        public DateTime DeliveredAt { get; set; } = DateTime.UtcNow;
    }
}