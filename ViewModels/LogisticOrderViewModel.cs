using LDMS_Final.Models;

namespace LDMS_Final.ViewModels
{
    public class LogisticOrderListViewModel
    {
        public List<Order> Orders        { get; set; } = new();
        public string      CurrentStatus { get; set; } = string.Empty;
    }

    public class LogisticOrderDetailViewModel
    {
        public Order                  Order            { get; set; } = null!;
        public OrderRouteAssignment?  RouteAssignment  { get; set; }

        /// <summary>
        /// Only drivers assigned to the same hub as this L-Staff (AssignedHub matches).
        /// Main L-Staff (AssignedHub == null) sees warehouse drivers (AssignedHub == null).
        /// </summary>
        public List<DriverWithVehicleViewModel> AvailableDrivers { get; set; } = new();

        public List<DriverWithVehicleViewModel> MotorcycleDrivers { get; set; } = new();

        /// <summary>Hubs the system suggests based on destination latitude.</summary>
        public List<DeliveryHub>      SuggestedHubs    { get; set; } = new();

        public string MapboxToken { get; set; } = string.Empty;

        /// <summary>True = Main L-Staff (warehouse), false = hub L-Staff.</summary>
        public bool IsMainLStaff { get; set; }

        /// <summary>The current L-Staff's own hub (null = warehouse).</summary>
        public DeliveryHub? CurrentStaffHub { get; set; }
    }

    // ── POST: Assign route (Main L-Staff only) ──────────────────────────────
    public class AssignRouteViewModel
    {
        public int     OrderId            { get; set; }
        public double? ResolvedLatitude   { get; set; }
        public double? ResolvedLongitude  { get; set; }
        public string? ResolvedAddress    { get; set; }

        /// <summary>Comma-separated hub int values in route order, e.g. "1,2,3".</summary>
        public string  SelectedHubs       { get; set; } = string.Empty;

        /// <summary>Driver IDs for each departure leg, indexed by StopOrder (0 = warehouse → hub0).</summary>
        public List<LegDriverAssignment> LegAssignments { get; set; } = new();
    }

    public class LegDriverAssignment
    {
        /// <summary>0 = leg from warehouse to first hub, 1 = first hub to second, …</summary>
        public int    LegIndex        { get; set; }
        public string? AssignedDriverId { get; set; }
    }

    // ── POST: Hub L-Staff assigns next-leg driver ──────────────────────────
    public class AssignNextDriverViewModel
    {
        public int    OrderId         { get; set; }
        public int    HubStopId       { get; set; }
        public string AssignedDriverId { get; set; } = string.Empty;
    }
}
