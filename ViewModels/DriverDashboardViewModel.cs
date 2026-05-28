using LDMS_Final.Models;

namespace LDMS_Final.ViewModels
{
    public class DriverDashboardViewModel
    {
        public ApplicationUser Driver { get; set; } = null!;
        public List<OrderHubStop> AllStops { get; set; } = new();
        public string ActiveFilter { get; set; } = "Pending";
        public int TotalAssigned { get; set; }
        public int InTransitCount { get; set; }
        public int CompletedCount { get; set; }
        public int PendingCount { get; set; }
        public string CurrentDriverId { get; set; } = string.Empty;
    }
}