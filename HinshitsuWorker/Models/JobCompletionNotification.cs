namespace HinshitsuWorker.Models
{
    public class JobCompletionNotification
    {
        public string JobId { get; set; } = string.Empty;
        public string UserId { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty; // "Completed", "Failed"
        public TaskCompletionSummary Tasks { get; set; } = new TaskCompletionSummary();
        public DateTime CompletedAt { get; set; } = DateTime.UtcNow;
    }

    public class TaskCompletionSummary
    {
        public string SecurityStatus { get; set; } = string.Empty;
        public string PerformanceStatus { get; set; } = string.Empty;
        public string UiUxStatus { get; set; } = string.Empty;
    }
}
