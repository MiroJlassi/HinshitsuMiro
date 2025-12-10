using HinshitsuWebApis.Models;
using System.Text.Json;

namespace HinshitsuWebApis.Services;

/// <summary>
/// Service for generating summaries from analysis results
/// Extracts the most important information for web response
/// </summary>
public class ResultsSummaryService
{
    private readonly ILogger<ResultsSummaryService> _logger;
    private readonly BlobStorageService _blobStorageService;
    private const string AnalysisResultsContainer = "analysis-results";

    public ResultsSummaryService(
        ILogger<ResultsSummaryService> logger,
        BlobStorageService blobStorageService)
    {
        _logger = logger;
        _blobStorageService = blobStorageService;
    }

    /// <summary>
    /// Downloads analysis results from blob storage and generates a summary for web
    /// </summary>
    /// <param name="userId">User ID</param>
    /// <param name="requestId">Request ID</param>
    /// <returns>Summary for web response</returns>
    public async Task<AnalysisSummaryForWeb> GenerateSummaryForWebAsync(string userId, string requestId)
    {
        _logger.LogInformation("Generating summary for web - UserId: {UserId}, RequestId: {RequestId}", userId, requestId);

        // Download the three JSON files from blob storage
        var analysisResults = await DownloadAnalysisResultsAsync(userId, requestId);

        // Extract the key information for web response
        var summary = ExtractKeyInsights(analysisResults, userId, requestId);

        _logger.LogInformation("Summary generated successfully for RequestId: {RequestId}", requestId);
        return summary;
    }

    /// <summary>
    /// Downloads analysis results from blob storage
    /// Container: analysis-results
    /// Folder: {userId}_{requestId}
    /// Files: performance.json, security.json, uiux.json
    /// Note: Some files may be missing if their corresponding tasks failed
    /// </summary>
    private async Task<AnalysisResults> DownloadAnalysisResultsAsync(string userId, string requestId)
    {
        var folderPrefix = $"{userId}_{requestId}";
        _logger.LogInformation("Downloading analysis results from folder: {Folder}", folderPrefix);

        var results = new AnalysisResults();

        // Download each file individually and handle failures gracefully
        try
        {
            var performanceJson = await _blobStorageService.DownloadBlobAsStringAsync(
                AnalysisResultsContainer, 
                $"{folderPrefix}/performance.json");
            results.Performance = JsonSerializer.Deserialize<PerformanceAnalysis>(
                performanceJson,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
            _logger.LogInformation("Performance results downloaded successfully for {RequestId}", requestId);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Performance results not available for {RequestId}", requestId);
            results.Performance = null;
        }

        try
        {
            var securityJson = await _blobStorageService.DownloadBlobAsStringAsync(
                AnalysisResultsContainer, 
                $"{folderPrefix}/security.json");
            results.Security = JsonSerializer.Deserialize<SecurityAnalysis>(
                securityJson,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
            _logger.LogInformation("Security results downloaded successfully for {RequestId}", requestId);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Security results not available for {RequestId}", requestId);
            results.Security = null;
        }

        try
        {
            var uiuxJson = await _blobStorageService.DownloadBlobAsStringAsync(
                AnalysisResultsContainer, 
                $"{folderPrefix}/uiux.json");
            results.UiUx = JsonSerializer.Deserialize<UiUxAnalysis>(
                uiuxJson,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
            _logger.LogInformation("UI/UX results downloaded successfully for {RequestId}", requestId);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "UI/UX results not available for {RequestId}", requestId);
            results.UiUx = null;
        }

        _logger.LogInformation("Analysis results download completed for {RequestId} - Performance: {HasPerf}, Security: {HasSec}, UiUx: {HasUi}", 
            requestId, results.Performance != null, results.Security != null, results.UiUx != null);

        return results;
    }

    /// <summary>
    /// Extracts key insights from full analysis results for web display
    /// </summary>
    private AnalysisSummaryForWeb ExtractKeyInsights(AnalysisResults results, string userId, string requestId)
    {
        _logger.LogInformation("Extracting key insights for RequestId: {RequestId}", requestId);

        var summary = new AnalysisSummaryForWeb
        {
            RequestId = requestId,
            UserId = userId,
            CompletedAt = DateTime.UtcNow,
            
            // Extract performance data
            Performance = ExtractPerformanceSummary(results.Performance),
            
            // Extract security data
            Security = ExtractSecuritySummary(results.Security),
            
            // Extract UI/UX data
            UiUx = ExtractUiUxSummary(results.UiUx),
            
            // PDF URL will be set after PDF generation
            PdfReportUrl = null
        };

        return summary;
    }

    /// <summary>
    /// Extracts performance summary with lighthouse scores and all category summaries
    /// </summary>
    private PerformanceSummaryForWeb? ExtractPerformanceSummary(PerformanceAnalysis? performance)
    {
        if (performance == null) return null;

        return new PerformanceSummaryForWeb
        {
            LighthouseScore = performance.Meta?.LighthouseScore,
            
            Performance = new CategorySummary
            {
                Summary = performance.Analysis?.Performance?.Summary,
                ScoreInterpretation = performance.Analysis?.Performance?.ScoreInterpretation,
                Priority = performance.Analysis?.Performance?.Priority
            },
            
            Accessibility = new CategorySummary
            {
                Summary = performance.Analysis?.Accessibility?.Summary,
                ScoreInterpretation = performance.Analysis?.Accessibility?.ScoreInterpretation,
                Priority = performance.Analysis?.Accessibility?.Priority
            },
            
            Seo = new CategorySummary
            {
                Summary = performance.Analysis?.Seo?.Summary,
                ScoreInterpretation = performance.Analysis?.Seo?.ScoreInterpretation,
                Priority = performance.Analysis?.Seo?.Priority
            },
            
            BestPractices = new CategorySummary
            {
                Summary = performance.Analysis?.BestPractices?.Summary,
                ScoreInterpretation = performance.Analysis?.BestPractices?.ScoreInterpretation,
                Priority = performance.Analysis?.BestPractices?.Priority
            },
            
            Diagnostics = new CategorySummary
            {
                Summary = performance.Analysis?.Diagnostics?.Summary,
                ScoreInterpretation = performance.Analysis?.Diagnostics?.ScoreInterpretation,
                Priority = performance.Analysis?.Diagnostics?.Priority
            }
        };
    }

    /// <summary>
    /// Extracts security summary with structured analysis and security headers map
    /// </summary>
    private SecuritySummaryForWeb? ExtractSecuritySummary(SecurityAnalysis? security)
    {
        if (security == null) return null;

        // Parse security headers from raw JSON
        var securityHeaders = ParseSecurityHeaders(security.SecurityHeadersRaw);

        return new SecuritySummaryForWeb
        {
            StructuredAnalysis = security.StructuredAnalysis,
            SecurityHeaders = securityHeaders
        };
    }

    /// <summary>
    /// Parses the security headers raw JSON into a dictionary
    /// </summary>
    private Dictionary<string, SecurityHeaderStatus>? ParseSecurityHeaders(string? rawJson)
    {
        if (string.IsNullOrEmpty(rawJson)) return null;

        try
        {
            var doc = JsonSerializer.Deserialize<JsonDocument>(rawJson);
            var headersElement = doc?.RootElement.GetProperty("headers");
            
            if (headersElement == null) return null;

            var headers = new Dictionary<string, SecurityHeaderStatus>();
            
            foreach (var header in headersElement.Value.EnumerateObject())
            {
                var headerObj = header.Value;
                headers[header.Name] = new SecurityHeaderStatus
                {
                    Present = headerObj.GetProperty("present").GetBoolean(),
                    Value = headerObj.TryGetProperty("value", out var val) && val.ValueKind != JsonValueKind.Null 
                        ? val.GetString() 
                        : null,
                    Status = headerObj.GetProperty("status").GetString()
                };
            }

            return headers;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to parse security headers raw JSON");
            return null;
        }
    }

    /// <summary>
    /// Extracts UI/UX summary with links, responsiveness scores, grammar, and overall assessment
    /// </summary>
    private UiUxSummaryForWeb? ExtractUiUxSummary(UiUxAnalysis? uiux)
    {
        if (uiux == null) return null;

        // Extract responsiveness data by viewport
        var responsiveness = new ResponsivenessData
        {
            Desktop = ExtractViewportData(uiux.SiteReview?.ViewportReviews, 0),
            Tablet = ExtractViewportData(uiux.SiteReview?.ViewportReviews, 1),
            Mobile = ExtractViewportData(uiux.SiteReview?.ViewportReviews, 2)
        };

        return new UiUxSummaryForWeb
        {
            Links = uiux.Links,
            Responsiveness = responsiveness,
            Grammar = new GrammarData
            {
                Score = uiux.GrammarCheck?.Score ?? 0,
                Summary = uiux.GrammarCheck?.Summary,
                Issues = uiux.GrammarCheck?.Issues
            },
            OverallScore = uiux.SiteReview?.OverallScore,
            CriticalIssues = uiux.SiteReview?.CriticalIssues,
            Recommendations = uiux.SiteReview?.Recommendations,
            Encouragement = uiux.SiteReview?.Encouragement
        };
    }

    /// <summary>
    /// Extracts viewport-specific data from site review
    /// </summary>
    private ViewportData? ExtractViewportData(List<ViewportReview>? viewportReviews, int index)
    {
        if (viewportReviews == null || index >= viewportReviews.Count) return null;

        var review = viewportReviews[index];
        return new ViewportData
        {
            Scores = review.Scores,
            Issues = review.Issues,
            Recommendations = review.Recommendations
        };
    }

    /// <summary>
    /// Gets raw analysis results for PDF generation (returns complete analysis data for detailed PDF report)
    /// </summary>
    public async Task<AnalysisResults> GetFullAnalysisResultsAsync(string userId, string requestId)
    {
        _logger.LogInformation("Getting full analysis results - UserId: {UserId}, RequestId: {RequestId}", userId, requestId);
        
        var results = await DownloadAnalysisResultsAsync(userId, requestId);
        
        return results;
    }
}
