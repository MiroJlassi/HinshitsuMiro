using Microsoft.AspNetCore.Mvc;
using HinshitsuSecurity.Services.Interfaces;
using HinshitsuSecurity.Models;

namespace SecurityAuditAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Produces("application/json")]
    public class HinshitsuSecurityController : ControllerBase
    {
        private readonly ISecurityHeadersAnalyzer _headersAnalyzer;
        private readonly IGptService _gptService;
        private readonly ILogger<HinshitsuSecurityController> _logger;

        public HinshitsuSecurityController(
            ISecurityHeadersAnalyzer headersAnalyzer,
            IGptService gptService,
            ILogger<HinshitsuSecurityController> logger)
        {
            _headersAnalyzer = headersAnalyzer ?? throw new ArgumentNullException(nameof(headersAnalyzer));
            _gptService = gptService ?? throw new ArgumentNullException(nameof(gptService));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <summary>
        /// Performs comprehensive security headers analysis on a website URL
        /// </summary>
        /// <param name="request">The URL to analyze</param>
        /// <returns>Security analysis results including headers and AI recommendations</returns>
        /// <response code="200">Returns the security analysis results</response>
        /// <response code="400">If the URL is invalid or missing</response>
        /// <response code="500">If an error occurs during analysis</response>
        [HttpPost("analyze")]
        [ProducesResponseType(typeof(SecurityResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult<SecurityResponse>> AnalyzeSecurityAsync([FromBody] UrlRequest request)
        {
            if (!ModelState.IsValid)
            {
                _logger.LogWarning("Invalid request received: {Errors}", ModelState.Values.SelectMany(v => v.Errors));
                return BadRequest(ModelState);
            }

            try
            {
                _logger.LogInformation("Starting security analysis for URL: {Url}", request.Url);

                var headersReport = await _headersAnalyzer.AnalyzeSecurityHeadersAsync(request.Url);
                var (structuredAnalysis, rawAnalysis) = await _gptService.AnalyzeStructuredAsync(headersReport, request.Url);

                var result = new SecurityResponse
                {
                    Url = request.Url,
                    SecurityHeadersRaw = headersReport,
                    StructuredAnalysis = structuredAnalysis,
                    GptAnalysisRaw = rawAnalysis,
                    AnalyzedAt = DateTime.UtcNow,
                    IsSuccess = true
                };

                _logger.LogInformation("Successfully completed security analysis for URL: {Url}", request.Url);
                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error during security analysis for URL: {Url}", request.Url);
                return StatusCode(StatusCodes.Status500InternalServerError, new ProblemDetails
                {
                    Status = StatusCodes.Status500InternalServerError,
                    Title = "Internal Server Error",
                    Detail = "An unexpected error occurred while analyzing the URL."
                });
            }
        }

        /// <summary>
        /// Health check endpoint for the security analysis service
        /// </summary>
        [HttpGet("health")]
        [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
        public IActionResult HealthCheck()
        {
            return Ok(new
            {
                status = "healthy",
                timestamp = DateTime.UtcNow,
                service = "Hinshitsu Security API",
                analyzer = "Custom Headers Analyzer"
            });
        }
    }
}
