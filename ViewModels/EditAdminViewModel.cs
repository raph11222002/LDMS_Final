using System.ComponentModel.DataAnnotations;

namespace LDMS_Final.ViewModels
{
    public class EditAdminViewModel
    {
        public string Id { get; set; } = string.Empty;

        [Required]
        [StringLength(100)]
        public string UserName { get; set; } = string.Empty;

        [Required]
        [StringLength(150)]
        public string FullName { get; set; } = string.Empty;

        [Required]
        [StringLength(20)]
        public string ContactNumber { get; set; } = string.Empty;

        public bool IsActive { get; set; }
    }
}