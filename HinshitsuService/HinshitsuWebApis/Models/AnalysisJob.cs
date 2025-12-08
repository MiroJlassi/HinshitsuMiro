using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace HinshitsuWebApis.Models
{
    public class AnalysisJob
    {
        [BsonId]
        [BsonRepresentation(BsonType.ObjectId)]
        public string? Id { get; set; }

        public string UserId { get; set; } = string.Empty;
        public string WebsiteUrl { get; set; } = string.Empty;
        public string Status { get; set; } = "Pending";
        public DateTime CreatedAt { get; set; }
        public DateTime? CompletedAt { get; set; }
        public TaskResults Tasks { get; set; } = new TaskResults();
        public OverallSummary? Summary { get; set; }
    }

    public class TaskResults
    {
        public TaskResult? Security { get; set; }
        public TaskResult? Performance { get; set; }
        public TaskResult? UiUx { get; set; }
    }

    public class TaskResult
    {
        public string Status { get; set; } = "Pending";
        public object? Data { get; set; }
        public string? ErrorMessage { get; set; }
        public DateTime? CompletedAt { get; set; }
    }

    public class OverallSummary
    {
        public double OverallScore { get; set; }
        public int TotalIssues { get; set; }
        public int CriticalIssues { get; set; }
    }
}
