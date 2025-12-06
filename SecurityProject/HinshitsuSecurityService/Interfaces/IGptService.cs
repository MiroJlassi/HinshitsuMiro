using HinshitsuSecurity.Models;

namespace HinshitsuSecurity.Services.Interfaces
{
    /// <summary>
    /// Service for AI-powered security analysis using Azure OpenAI
    /// </summary>
    public interface IGptService
    {
        /// <summary>
        /// Analyzes security report using AI to provide actionable insights
        /// </summary>
        /// <param name="securityJson">Raw security report JSON</param>
        /// <param name="url">The analyzed website URL</param>
        /// <returns>AI-generated security analysis and recommendations</returns>
        //Task<string> AnalyzeAsync(string securityJson, string url);

        /// <summary>
        /// Analyzes security report using AI with structured JSON output
        /// </summary>
        /// <param name="securityJson">Raw security report JSON</param>
        /// <param name="url">The analyzed website URL</param>
        /// <returns>Tuple containing structured analysis object and raw text (fallback)</returns>
        Task<(SecurityAnalysis? Structured, string Raw)> AnalyzeStructuredAsync(string securityJson, string url);
    }
}
