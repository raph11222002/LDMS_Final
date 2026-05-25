namespace LDMS_Final.Models
{
    public class Notification
    {
        public int Id { get; set; }

        /// <summary>The user who should receive this notification.</summary>
        public string RecipientUserId { get; set; } = string.Empty;

        public string Title { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;

        /// <summary>Optional deep-link URL (e.g. /LogisticStaffOrder/Detail/5).</summary>
        public string? ActionUrl { get; set; }

        public bool IsRead { get; set; } = false;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
