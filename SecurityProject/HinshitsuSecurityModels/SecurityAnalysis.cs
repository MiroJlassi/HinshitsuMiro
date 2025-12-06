namespace HinshitsuSecurity.Models
{
    /// <summary>
    /// Structured security analysis result from GPT
    /// </summary>
    public class SecurityAnalysis
    {
        /// <summary>
        /// Executive summary of the security assessment
        /// </summary>
        public Summary Summary { get; set; } = new();

        /// <summary>
        /// List of identified security issues and associated risks
        /// </summary>
        public List<IssueWithRisk> IssuesAndRisks { get; set; } = new();

        /// <summary>
        /// List of prioritized recommendations
        /// </summary>
        public List<Suggestion> Suggestions { get; set; } = new();
    }

    /// <summary>
    /// Security assessment summary
    /// </summary>
    public class Summary
    {
        /// <summary>
        /// Overall security grade (A-F)
        /// </summary>
        public string Grade { get; set; } = string.Empty;

        /// <summary>
        /// Risk level (Critical, High, Medium, Low)
        /// </summary>
        public string RiskLevel { get; set; } = string.Empty;

        /// <summary>
        /// Number of headers configured
        /// </summary>
        public int ConfiguredHeaders { get; set; }

        /// <summary>
        /// Number of headers missing
        /// </summary>
        public int MissingHeaders { get; set; }

        /// <summary>
        /// Brief overall assessment
        /// </summary>
        public string OverallAssessment { get; set; } = string.Empty;
    }

    /// <summary>
    /// Security issue with associated risk
    /// </summary>
    public class IssueWithRisk
    {
        /// <summary>
        /// Security header name
        /// </summary>
        public string Header { get; set; } = string.Empty;

        /// <summary>
        /// Issue description
        /// </summary>
        public string Issue { get; set; } = string.Empty;

        /// <summary>
        /// Associated security risk
        /// </summary>
        public string Risk { get; set; } = string.Empty;

        /// <summary>
        /// Attack vectors this header prevents
        /// </summary>
        public List<string> AttackVectors { get; set; } = new();
    }

    /// <summary>
    /// Security recommendation
    /// </summary>
    public class Suggestion
    {
        /// <summary>
        /// Priority level (1=Immediate, 2=High, 3=Medium)
        /// </summary>
        public int Priority { get; set; }

        /// <summary>
        /// Header to configure
        /// </summary>
        public string Header { get; set; } = string.Empty;

        /// <summary>
        /// Recommendation description
        /// </summary>
        public string Description { get; set; } = string.Empty;

        /// <summary>
        /// Exact header configuration to implement
        /// </summary>
        public string Implementation { get; set; } = string.Empty;
    }
}
