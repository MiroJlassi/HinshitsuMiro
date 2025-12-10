namespace HinshitsuWebApis.Models
{
    public class SubmitAnalysisRequest
    {
        public string Url { get; set; } = string.Empty;
    }

    public class SubmitAnalysisResponse
    {
        public string? JobId { get; set; }
        public string Status { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
    }

    public class AnalysisJobResponse
    {
        public string JobId { get; set; } = string.Empty;
        public string WebsiteUrl { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
        public DateTime? CompletedAt { get; set; }
        public TaskResultsResponse? Tasks { get; set; }
        public OverallSummaryResponse? Summary { get; set; }
    }

    public class TaskResultsResponse
    {
        public TaskResultDetail? Security { get; set; }
        public TaskResultDetail? Performance { get; set; }
        public TaskResultDetail? UiUx { get; set; }
    }

    public class TaskResultDetail
    {
        public string Status { get; set; } = string.Empty;
        public object? Data { get; set; }
        public string? ErrorMessage { get; set; }
        public DateTime? CompletedAt { get; set; }
    }

    public class OverallSummaryResponse
    {
        public double OverallScore { get; set; }
        public int TotalIssues { get; set; }
        public int CriticalIssues { get; set; }
    }
}
