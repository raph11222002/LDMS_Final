using System.ComponentModel.DataAnnotations.Schema;

namespace LDMS_Final.Models
{
    public class OrderStatusLog
    {
        public int Id { get; set; }

        public int OrderId { get; set; }
        public Order Order { get; set; } = null!;

        public string Status { get; set; } = string.Empty;
        public string? Note { get; set; }
        public string? UpdatedByUserId { get; set; }
        public string? UpdatedByName { get; set; }

        /// <summary>
        /// When true this log entry is shown to the buyer in their order tracking.
        /// Internal logistic/warehouse processing notes are false.
        /// </summary>
        public bool IsVisibleToBuyer { get; set; } = false;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
