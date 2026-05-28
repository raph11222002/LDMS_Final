// ViewModels/DriverLogsViewModel.cs
using LDMS_Final.Models;

namespace LDMS_Final.ViewModels
{
    public class DriverLogsViewModel
    {
        public ApplicationUser Driver { get; set; } = null!;
        public DriverVehicle? Vehicle { get; set; }
        public string ActiveTab { get; set; } = "deliveries";

        // Data sets
        public List<OrderHubStop> AllStops { get; set; } = new();
        public List<RiderDeliveredReport> DeliveryReports { get; set; } = new();
        public List<OrderStatusLog> StatusLogs { get; set; } = new();
        public List<Notification> Notifications { get; set; } = new();

        // Summary stats
        public int TotalAssigned { get; set; }
        public int TotalCompleted { get; set; }
        public int TotalInTransit { get; set; }
        public int TotalPending { get; set; }
        public int TotalReports { get; set; }
    }
}