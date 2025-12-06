namespace HinshitsuSecurity.Services.Interfaces
{
    /// <summary>
    /// Service for analyzing website security headers
    /// </summary>
    public interface ISecurityHeadersAnalyzer
    {
        /// <summary>
        /// Analyzes security headers for a given URL by making HTTP requests
        /// </summary>
        /// <param name="url">The URL to analyze</param>
        /// <returns>JSON string containing the security headers analysis</returns>
        Task<string> AnalyzeSecurityHeadersAsync(string url);
    }
}
