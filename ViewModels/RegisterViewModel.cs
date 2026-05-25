using System.ComponentModel.DataAnnotations;

namespace LDMS_Final.ViewModels
{
    public class RegisterViewModel
    {
        [Required]
        public string FullName { get; set; } = string.Empty;

        [Required]
        [RegularExpression(@"^09\d{9}$", ErrorMessage = "Invalid contact number format.")]
        public string ContactNumber { get; set; } = string.Empty;

        [Required, EmailAddress]
        public string Email { get; set; } = string.Empty;

        [Required]
        public string Gender { get; set; } = string.Empty;

        [Required]
        public string AddressLine1 { get; set; } = string.Empty;

        public string? AddressLine2 { get; set; }

        [Required]
        public string City { get; set; } = string.Empty;

        [Required]
        [DataType(DataType.Password)]
        [StringLength(100, MinimumLength = 8)]
        public string Password { get; set; } = string.Empty;

        [Required]
        [DataType(DataType.Password)]
        [Compare(nameof(Password))]
        public string ConfirmPassword { get; set; } = string.Empty;
    }
}