using System.ComponentModel.DataAnnotations;

namespace LDMS_Final.ViewModels
{
    public class EditUserViewModel
    {
        public string Id { get; set; } = string.Empty;

        [Required]
        public string UserName { get; set; } = string.Empty;

        [Required]
        public string FullName { get; set; } = string.Empty;

        [Required]
        public string ContactNumber { get; set; } = string.Empty;

        public string Gender { get; set; } = string.Empty;

        public bool IsActive { get; set; }
    }
}