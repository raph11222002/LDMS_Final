namespace LDMS_Final.Models
{
    public class DriverVehicle
    {
        public int Id { get; set; }
        public string DriverId { get; set; } = string.Empty;
        public string VehicleType { get; set; } = string.Empty; // "4-Wheels Closed Van" or "Motorcycle"
        public string PlateNumber { get; set; } = string.Empty;
        public bool IsActive { get; set; } = true;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedAt { get; set; }

        // Navigation
        public ApplicationUser Driver { get; set; } = null!;
    }
}