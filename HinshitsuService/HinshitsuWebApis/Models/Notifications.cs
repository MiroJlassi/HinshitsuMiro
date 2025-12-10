namespace HinshitsuWebApis.Models
{
    public class TaskProgressNotification
    {
        public string JobId { get; set; } = string.Empty;
        public string UserId { get; set; } = string.Empty;
        public string TaskType { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public object? Data { get; set; }
        public string? ErrorMessage { get; set; }
        public DateTime Timestamp { get; set; }
    }

    public class JobCompletionNotification
    {
        public string JobId { get; set; } = string.Empty;
        public string UserId { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public TaskCompletionSummary Tasks { get; set; } = new();
        public DateTime CompletedAt { get; set; }
    }

    public class TaskCompletionSummary
    {
        public string SecurityStatus { get; set; } = string.Empty;
        public string PerformanceStatus { get; set; } = string.Empty;
        public string UiUxStatus { get; set; } = string.Empty;
    }
}
