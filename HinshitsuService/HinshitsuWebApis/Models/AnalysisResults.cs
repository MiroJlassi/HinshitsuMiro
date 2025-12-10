namespace HinshitsuWebApis.Models;

/// <summary>
/// Represents the complete analysis results from all three analysis categories
/// </summary>
public class AnalysisResults
{
    public PerformanceAnalysis? Performance { get; set; }
    public SecurityAnalysis? Security { get; set; }
    public UiUxAnalysis? UiUx { get; set; }
}

/// <summary>
/// Performance analysis results from Lighthouse
/// </summary>
public class PerformanceAnalysis
{
    public PerformanceMeta? Meta { get; set; }
    public PerformanceAnalysisData? Analysis { get; set; }
    public PerformanceStats? Stats { get; set; }
}

public class PerformanceMeta
{
    public string? Url { get; set; }
    public DateTime Timestamp { get; set; }
    public LighthouseScore? LighthouseScore { get; set; }
}

public class LighthouseScore
{
    public double Performance { get; set; }
    public double Accessibility { get; set; }
    public double Seo { get; set; }
    public double BestPractices { get; set; }
}

public class PerformanceAnalysisData
{
    public CategoryAnalysis? Performance { get; set; }
    public CategoryAnalysis? Accessibility { get; set; }
    public CategoryAnalysis? Seo { get; set; }
    public CategoryAnalysis? BestPractices { get; set; }
    public CategoryAnalysis? Diagnostics { get; set; }
}

public class CategoryAnalysis
{
    public string? Summary { get; set; }
    public string? ScoreInterpretation { get; set; }
    public List<string>? TopIssues { get; set; }
    public List<string>? Recommendations { get; set; }
    public string? Priority { get; set; }
}

public class PerformanceStats
{
    public string? Url { get; set; }
    // Add more stats properties as needed
}

/// <summary>
/// Security analysis results
/// </summary>
public class SecurityAnalysis
{
    public string? Url { get; set; }
    public string? SecurityHeadersRaw { get; set; }
    public SecurityStructuredAnalysis? StructuredAnalysis { get; set; }
}

public class SecurityStructuredAnalysis
{
    public SecuritySummary? Summary { get; set; }
    public List<SecurityIssue>? IssuesAndRisks { get; set; }
    public List<SecurityRecommendation>? Recommendations { get; set; }
}

public class SecuritySummary
{
    public string? Grade { get; set; }
    public string? RiskLevel { get; set; }
    public int ConfiguredHeaders { get; set; }
    public int MissingHeaders { get; set; }
    public string? OverallAssessment { get; set; }
}

public class SecurityIssue
{
    public string? Header { get; set; }
    public string? Issue { get; set; }
    public string? Risk { get; set; }
    public List<string>? AttackVectors { get; set; }
}

public class SecurityRecommendation
{
    public string? Header { get; set; }
    public string? Action { get; set; }
    public string? Impact { get; set; }
    public string? Priority { get; set; }
}

/// <summary>
/// UI/UX analysis results
/// </summary>
public class UiUxAnalysis
{
    public string? Url { get; set; }
    public DateTime Timestamp { get; set; }
    public List<LinkData>? Links { get; set; }
    public List<UiAnalysisViewport>? UiAnalysis { get; set; }
    public GrammarCheck? GrammarCheck { get; set; }
    public SiteReview? SiteReview { get; set; }
}

public class LinkData
{
    public string? Text { get; set; }
    public string? Href { get; set; }
    public string? Title { get; set; }
}

public class UiAnalysisViewport
{
    public string? Viewport { get; set; }
    public UiAnalysisDetails? Analysis { get; set; }
}

public class UiAnalysisDetails
{
    public UiScores? Scores { get; set; }
    public List<string>? Issues { get; set; }
    public List<string>? Suggestions { get; set; }
}

public class UiScores
{
    public int Layout { get; set; }
    public int Colors { get; set; }
    public int Typography { get; set; }
    public int Spacing { get; set; }
    public int Overall { get; set; }
}

public class GrammarCheck
{
    public int Score { get; set; }
    public List<GrammarIssue>? Issues { get; set; }
    public string? Summary { get; set; }
}

public class GrammarIssue
{
    public string? Text { get; set; }
    public string? Type { get; set; }
    public string? Suggestion { get; set; }
}

public class SiteReview
{
    public List<ViewportReview>? ViewportReviews { get; set; }
    public string? OverallScore { get; set; }
    public List<string>? CriticalIssues { get; set; }
    public List<string>? Recommendations { get; set; }
    public string? Encouragement { get; set; }
}

public class ViewportReview
{
    public ResponsivenessScores? Scores { get; set; }
    public List<string>? Issues { get; set; }
    public List<string>? Recommendations { get; set; }
}

public class ResponsivenessScores
{
    public int Accessibility { get; set; }
    public int SemanticHTML { get; set; }
    public int Navigation { get; set; }
    public int Responsiveness { get; set; }
    public int Overall { get; set; }
}

/// <summary>
/// Simplified summary for web response (most important results)
/// </summary>
public class AnalysisSummaryForWeb
{
    public string? RequestId { get; set; }
    public string? UserId { get; set; }
    public DateTime CompletedAt { get; set; }
    
    // Performance summary
    public PerformanceSummaryForWeb? Performance { get; set; }
    
    // Security summary
    public SecuritySummaryForWeb? Security { get; set; }
    
    // UI/UX summary
    public UiUxSummaryForWeb? UiUx { get; set; }
    
    // PDF report link
    public string? PdfReportUrl { get; set; }
}

/// <summary>
/// Performance summary for web
/// </summary>
public class PerformanceSummaryForWeb
{
    public LighthouseScore? LighthouseScore { get; set; }
    public CategorySummary? Performance { get; set; }
    public CategorySummary? Accessibility { get; set; }
    public CategorySummary? Seo { get; set; }
    public CategorySummary? BestPractices { get; set; }
    public CategorySummary? Diagnostics { get; set; }
}

/// <summary>
/// Category summary for web (summary, score interpretation, priority)
/// </summary>
public class CategorySummary
{
    public string? Summary { get; set; }
    public string? ScoreInterpretation { get; set; }
    public string? Priority { get; set; }
}

/// <summary>
/// Security summary for web
/// </summary>
public class SecuritySummaryForWeb
{
    public SecurityStructuredAnalysis? StructuredAnalysis { get; set; }
    public Dictionary<string, SecurityHeaderStatus>? SecurityHeaders { get; set; }
}

/// <summary>
/// Security header status
/// </summary>
public class SecurityHeaderStatus
{
    public bool Present { get; set; }
    public string? Value { get; set; }
    public string? Status { get; set; }
}

/// <summary>
/// UI/UX summary for web
/// </summary>
public class UiUxSummaryForWeb
{
    public List<LinkData>? Links { get; set; }
    public ResponsivenessData? Responsiveness { get; set; }
    public GrammarData? Grammar { get; set; }
    public string? OverallScore { get; set; }
    public List<string>? CriticalIssues { get; set; }
    public List<string>? Recommendations { get; set; }
    public string? Encouragement { get; set; }
}

/// <summary>
/// Responsiveness data by viewport
/// </summary>
public class ResponsivenessData
{
    public ViewportData? Desktop { get; set; }
    public ViewportData? Tablet { get; set; }
    public ViewportData? Mobile { get; set; }
}

/// <summary>
/// Viewport-specific data
/// </summary>
public class ViewportData
{
    public ResponsivenessScores? Scores { get; set; }
    public List<string>? Issues { get; set; }
    public List<string>? Recommendations { get; set; }
}

/// <summary>
/// Grammar check data
/// </summary>
public class GrammarData
{
    public int Score { get; set; }
    public string? Summary { get; set; }
    public List<GrammarIssue>? Issues { get; set; }
}
