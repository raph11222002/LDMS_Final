using System.ComponentModel.DataAnnotations.Schema;

namespace LDMS_Final.Models
{
    /// <summary>
    /// Stores the full route plan for one order parcel.
    /// Each OrderHubStop = one leg (from → to).
    ///
    /// Stop 0  = Warehouse → Davao Hub (always, every parcel passes Davao first)
    /// Stop 1  = Davao Hub → Surigao Hub  (if needed)
    /// Stop 2  = Surigao → Tacloban Hub   (if needed)
    /// Stop 3  = Tacloban → Pasay Hub     (if needed)
    /// Last stop's "to" = final delivery destination.
    ///
    /// Each leg's driver is assigned by the L-Staff whose hub the parcel DEPARTS FROM:
    ///   - Leg 0 driver   → assigned by Main L-Staff (warehouse)
    ///   - Leg 1 driver   → assigned by Davao Hub L-Staff after parcel arrives there
    ///   - … and so on.
    /// </summary>
    public class OrderRouteAssignment
    {
        public int Id { get; set; }

        public int OrderId { get; set; }
        public Order Order { get; set; } = null!;

        /// <summary>The Main L-Staff who created this route plan.</summary>
        public string AssignedByStaffId { get; set; } = string.Empty;
        public ApplicationUser AssignedByStaff { get; set; } = null!;

        public List<OrderHubStop> HubStops { get; set; } = new();

        /// <summary>Coordinates resolved by the Main L-Staff on the map.</summary>
        [Column(TypeName = "decimal(10,7)")]
        public decimal? ResolvedLatitude { get; set; }

        [Column(TypeName = "decimal(10,7)")]
        public decimal? ResolvedLongitude { get; set; }

        public string? ResolvedAddress { get; set; }

        [Column(TypeName = "decimal(10,2)")]
        public decimal? EstimatedDistanceKm { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedAt { get; set; }
    }

    /// <summary>
    /// Represents one HUB STOP in the parcel's journey.
    ///
    /// StopOrder 0 = first hub (Davao) — the leg FROM warehouse TO Davao.
    /// StopOrder 1 = second hub (Surigao) — leg FROM Davao TO Surigao.
    /// … etc.
    ///
    /// The driver on each leg is the driver assigned BY the L-Staff at the
    /// DEPARTING hub.  Main L-Staff assigns Stop-0 driver.  Davao L-Staff
    /// assigns Stop-1 driver after parcel arrives, and so on.
    /// </summary>
    public class OrderHubStop
    {
        public int Id { get; set; }

        public int OrderRouteAssignmentId { get; set; }
        public OrderRouteAssignment RouteAssignment { get; set; } = null!;

        /// <summary>0-based index of this stop in the route.</summary>
        public int StopOrder { get; set; }

        /// <summary>The hub this stop represents (parcel arrives here).</summary>
        public DeliveryHub Hub { get; set; }

        public string HubLabel { get; set; } = string.Empty;

        // ── Driver for the leg DEPARTING from the previous location TO this hub ──
        /// <summary>
        /// Driver who carries the parcel on the leg ending at this hub.
        /// Assigned by the L-Staff of the previous/originating location.
        /// </summary>
        public string? AssignedDriverId { get; set; }
        public ApplicationUser? AssignedDriver { get; set; }

        // Outgoing driver for final leg (last hub → delivery destination)
        public string? OutgoingDriverId { get; set; }
        public string? OutgoingDriverLabel { get; set; }

        // Navigation
        public ApplicationUser? OutgoingDriver { get; set; }

        /// <summary>Human label e.g. "van driver 1".</summary>
        public string? DriverLabel { get; set; }

        // ── Status ────────────────────────────────────────────────────────
        public HubStopStatus StopStatus { get; set; } = HubStopStatus.Pending;
        public DateTime? DepartedFromPrevAt { get; set; }
        public DateTime? ArrivedAt { get; set; }
    }

    public enum HubStopStatus
    {
        Pending,      // not yet started
        InTransit,    // driver departed previous location
        Arrived,      // driver scanned arrival QR at this hub
        Completed     // parcel handed off / next leg assigned
    }

    public enum DeliveryHub
    {
        Davao    = 1,
        Surigao  = 2,
        Tacloban = 3,
        Pasay    = 4
    }

    public static class DeliveryHubInfo
    {
        public static readonly Dictionary<DeliveryHub, (double Lat, double Lng, string Label, string ShortName)> Hubs = new()
        {
            [DeliveryHub.Davao]    = (7.064764940987943, 125.60869053843403, "Mindanao Regional Hub (Davao Sorting Center)",       "Davao"),
            [DeliveryHub.Surigao]  = (9.790260868323017,  125.49312423896352, "Mindanao Gateway Hub (Surigao Sorting Center)",      "Surigao"),
            [DeliveryHub.Tacloban] = (11.222528827106181, 124.99200750511456, "Visayas Transshipment Hub (Tacloban Sorting Center)", "Tacloban"),
            [DeliveryHub.Pasay]    = (14.537682187625546, 121.00077350135048, "Luzon Central Sorting Hub (Pasay Delivery Hub)",     "Pasay"),
        };

        public const double WarehouseLatitude  = 7.036924878323465;
        public const double WarehouseLongitude = 125.52539152251724;
        public const string WarehouseLabel     = "Warehouse Origin (Davao City)";

        /// <summary>Canonical south-to-north hub order.</summary>
        public static readonly DeliveryHub[] HubSequence =
        [
            DeliveryHub.Davao,
            DeliveryHub.Surigao,
            DeliveryHub.Tacloban,
            DeliveryHub.Pasay,
        ];

        /// <summary>
        /// Given a destination latitude, return the hubs the parcel must pass
        /// through. A hub is included if its latitude is LESS THAN the
        /// destination latitude (the parcel has to travel past it).
        /// Davao hub is ALWAYS included (every parcel from warehouse passes it).
        /// </summary>
        public static List<DeliveryHub> DetermineRequiredHubs(double destLat)
        {
            var result = new List<DeliveryHub>();
            foreach (var hub in HubSequence)
            {
                var (lat, _, _, _) = Hubs[hub];
                // Always include Davao; include others if they sit before the destination
                if (hub == DeliveryHub.Davao || lat < destLat)
                    result.Add(hub);
            }
            return result;
        }
    }
}
