namespace HinshitsuWorker.Models
{
    public class TaskProgressNotification
    {
        public string JobId { get; set; } = string.Empty;
        public string UserId { get; set; } = string.Empty;
        public string TaskType { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty; // "Started", "Completed", "Failed"
        public object? Data { get; set; }
        public string? ErrorMessage { get; set; }
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    }
}
