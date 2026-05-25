using System.ComponentModel.DataAnnotations;

namespace LDMS_Final.ViewModels
{
    public class LoginViewModel
    {
        [Required]
        public string UserNameOrEmail { get; set; } = string.Empty;

        [Required]
        [DataType(DataType.Password)]
        public string Password { get; set; } = string.Empty;

        public bool RememberMe { get; set; }
    }
}