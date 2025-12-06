namespace HinshitsuSecurity.Models
{
    /// <summary>
    /// Response model containing security analysis results
    /// </summary>
    public class SecurityResponse
    {
        /// <summary>
        /// The analyzed website URL
        /// </summary>
        public string Url { get; set; } = string.Empty;

        /// <summary>
        /// Raw security headers data from the analyzer
        /// </summary>
        public object? SecurityHeadersRaw { get; set; }

        /// <summary>
        /// Structured AI-generated security analysis
        /// </summary>
        public SecurityAnalysis? StructuredAnalysis { get; set; }

        /// <summary>
        /// Raw AI analysis text (fallback)
        /// </summary>
        public string? GptAnalysisRaw { get; set; }

        /// <summary>
        /// Timestamp when the analysis was performed
        /// </summary>
        public DateTime AnalyzedAt { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// Indicates if the analysis was successful
        /// </summary>
        public bool IsSuccess { get; set; } = true;
    }
}