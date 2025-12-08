namespace HinshitsuWebApis.Models
{
    public class WorkerTaskMessage
    {
        public string? JobId { get; set; }
        public string UserId { get; set; } = string.Empty;
        public string WebsiteUrl { get; set; } = string.Empty;
        public string TaskType { get; set; } = string.Empty; // "security", "performance", or "uiux"
        public DateTime RequestedAt { get; set; }
    }
}
