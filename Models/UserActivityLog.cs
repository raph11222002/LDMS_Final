namespace LDMS_Final.Models
{
    /// <summary>
    /// Records every meaningful user action (not login/logout – actions only).
    /// One row per event. Written via UserActivityService.
    /// </summary>
    public class UserActivityLog
    {
        public int Id { get; set; }

        // ── Who ─────────────────────────────────────────────────────────
        /// <summary>FK to AspNetUsers.Id (string, not enforced as FK to avoid cascade issues).</summary>
        public string UserId { get; set; } = string.Empty;
        public string UserName { get; set; } = string.Empty;  // snapshot – survives renames
        public string FullName { get; set; } = string.Empty;  // snapshot
        public string Role { get; set; } = string.Empty;  // primary role at time of action

        // ── What ────────────────────────────────────────────────────────
        /// <summary>Short action name, e.g. "Order Approved". Use UserAction constants.</summary>
        public string Action { get; set; } = string.Empty;
        /// <summary>Human-readable sentence describing what happened.</summary>
        public string? Description { get; set; }
        /// <summary>Table/domain this action relates to, e.g. "Order", "Product".</summary>
        public string? EntityType { get; set; }
        /// <summary>The primary key or business ID, e.g. order number "ORD-20260520-0001".</summary>
        public string? EntityId { get; set; }

        // ── When ────────────────────────────────────────────────────────
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }

    /// <summary>
    /// Canonical action name constants.
    /// Always use these – never raw strings – so filters and exports are consistent.
    /// </summary>
    public static class UserAction
    {
        // ── Orders ───────────────────────────────────────────────
        public const string OrderPlaced = "Order Placed";
        public const string OrderApproved = "Order Approved";
        public const string OrderCancelled = "Order Cancelled";

        // ── Logistics ────────────────────────────────────────────
        public const string RouteAssigned = "Route Assigned";
        public const string DriverAssigned = "Driver Assigned";
        public const string OrderScanned = "Order Scanned";

        // ── Products & stock ─────────────────────────────────────
        public const string ProductCreated = "Product Created";
        public const string ProductUpdated = "Product Updated";
        public const string ProductDeleted = "Product Deleted";
        public const string StockUpdated = "Stock Updated";

        // ── Account management ────────────────────────────────────
        public const string AccountCreated = "Account Created";
        public const string AccountUpdated = "Account Updated";
        public const string AccountDeleted = "Account Deleted";
        public const string AccountToggled = "Account Status Toggled";
    }
}