namespace LDMS_Final.ViewModels
{
    public class AdminDashboardViewModel
    {
        public int TotalStaffCount { get; set; }
        public int ActiveStaffCount { get; set; }
        public int LogisticStaffCount { get; set; }
        public int WarehouseStaffCount { get; set; }

        // Admin can only view driver counts, not manage them
        public int TotalDriverCount { get; set; }
        public int ActiveDriverCount { get; set; }

        public int TotalProductCount { get; set; }
        public int ActiveProductCount { get; set; }
    }
}