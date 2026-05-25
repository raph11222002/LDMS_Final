namespace LDMS_Final.ViewModels
{
    public class DriverWithVehicleViewModel
    {
        public string Id { get; set; } = string.Empty;
        public string FullName { get; set; } = string.Empty;
        public string VehicleType { get; set; } = string.Empty;
        public string PlateNumber { get; set; } = string.Empty;

        public string DisplayName => string.IsNullOrWhiteSpace(VehicleType)
            ? FullName
            : $"{FullName} — {VehicleType}";
    }
}