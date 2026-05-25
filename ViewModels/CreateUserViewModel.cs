using System.ComponentModel.DataAnnotations;
using LDMS_Final.Models;

namespace LDMS_Final.ViewModels
{
    public class CreateUserViewModel
    {
        [Required]
        public string UserName { get; set; } = string.Empty;

        [Required]
        public string FullName { get; set; } = string.Empty;

        [Required]
        public string ContactNumber { get; set; } = string.Empty;

        [Required]
        public string Gender { get; set; } = string.Empty;

        [Required]
        public string SelectedRole { get; set; } = string.Empty;

        public DeliveryHub? SelectedHub { get; set; }

        public DeliveryHub? AssignedHub { get; set; }
        public string? VehicleType { get; set; }
        public string? PlateNumber { get; set; }

        [Required]
        [DataType(DataType.Password)]
        public string Password { get; set; } = string.Empty;

        [Required]
        [DataType(DataType.Password)]
        [Compare("Password", ErrorMessage = "Passwords do not match.")]
        public string ConfirmPassword { get; set; } = string.Empty;
    }
}