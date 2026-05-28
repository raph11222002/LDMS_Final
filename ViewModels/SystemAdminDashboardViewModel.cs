using LDMS_Final.Models;

namespace LDMS_Final.ViewModels
{
    public class SystemAdminDashboardViewModel
    {
        public int AdminCount { get; set; }
        public int BuyerCount { get; set; }
        public int DriverCount { get; set; }
        public int WarehouseStaffCount { get; set; }
        public int LogisticStaffCount { get; set; }
        public int OtherUsersCount { get; set; }
        public int ActiveAdminCount { get; set; }
        public int TotalLogsToday { get; set; }
        public List<UserActivityLog> RecentActivity { get; set; } = new();
    }

    public class UserLogsViewModel
    {
        public List<UserActivityLog> Logs { get; set; } = new();
        public int TotalCount { get; set; }
        public int PageNumber { get; set; } = 1;
        public int PageSize { get; set; } = 25;
        public int TotalPages => (int)Math.Ceiling((double)TotalCount / PageSize);

        // Active filter values
        public string? SearchName { get; set; }
        public string? SearchUserId { get; set; }
        public string? FilterRole { get; set; }
        public string? FilterAction { get; set; }
        public string? DateFrom { get; set; }
        public string? DateTo { get; set; }

        // Dropdown sources  (built from distinct values already in the table)
        public List<string> AllRoles { get; set; } = new();
        public List<string> AllActions { get; set; } = new();
    }
}