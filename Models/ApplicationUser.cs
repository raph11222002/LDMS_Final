using Microsoft.AspNetCore.Identity;

namespace LDMS_Final.Models
{
    public class ApplicationUser : IdentityUser
    {
        public string FullName        { get; set; } = string.Empty;
        public string ContactNumber   { get; set; } = string.Empty;
        public string Gender          { get; set; } = string.Empty;

        public string AddressLine1    { get; set; } = string.Empty;
        public string? AddressLine2   { get; set; }
        public string City            { get; set; } = string.Empty;

        public bool IsActive          { get; set; } = true;
        public string? CreatedByUserId { get; set; }
        public string? ParentAdminId  { get; set; }

        public DeliveryHub? AssignedHub { get; set; }

        public DateTime CreatedAt     { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedAt    { get; set; }

        // Add inside ApplicationUser class
        public ICollection<DriverVehicle> Vehicles { get; set; } = new List<DriverVehicle>();
    }
}
