using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using HinshitsuWebApis.Services;
using HinshitsuWebApis.Models;

namespace HinshitsuWebApis.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AnalysisController : ControllerBase
    {
        private readonly AnalysisJobService _jobService;
        private readonly QueueService _queueService;
        private readonly ResultsSummaryService _summaryService;
        private readonly PdfReportService _pdfReportService;
        private readonly ILogger<AnalysisController> _logger;

        public AnalysisController(
            AnalysisJobService jobService,
            QueueService queueService,
            ResultsSummaryService summaryService,
            PdfReportService pdfReportService,
            ILogger<AnalysisController> logger)
        {
            _jobService = jobService;
            _queueService = queueService;
            _summaryService = summaryService;
            _pdfReportService = pdfReportService;
            _logger = logger;
        }

        [HttpPost("submit")]
        [Authorize]
        public async Task<IActionResult> SubmitAnalysis([FromBody] SubmitAnalysisRequest request)
        {
            var userId = User.Claims.First(c => c.Type == "id").Value;

            var job = await _jobService.CreateJobAsync(userId, request.Url);

            var securityTask = new WorkerTaskMessage
            {
                JobId = job.Id,
                UserId = userId,
                WebsiteUrl = request.Url,
                TaskType = "security",
                RequestedAt = DateTime.UtcNow
            };

            var performanceTask = new WorkerTaskMessage
            {
                JobId = job.Id,
                UserId = userId,
                WebsiteUrl = request.Url,
                TaskType = "performance",
                RequestedAt = DateTime.UtcNow
            };

            var uiuxTask = new WorkerTaskMessage
            {
                JobId = job.Id,
                UserId = userId,
                WebsiteUrl = request.Url,
                TaskType = "uiux",
                RequestedAt = DateTime.UtcNow
            };

            await _queueService.EnqueueMessageAsync("security-checks", securityTask);
            await _queueService.EnqueueMessageAsync("performance-checks", performanceTask);
            await _queueService.EnqueueMessageAsync("uiux-checks", uiuxTask);

            _logger.LogInformation("Analysis job {JobId} submitted with 3 worker tasks for website: {WebsiteUrl}", job.Id, request.Url);

            return Ok(new SubmitAnalysisResponse
            {
                JobId = job.Id,
                Status = job.Status,
                Message = "Analysis job submitted successfully with 3 worker tasks"
            });
        }

        [HttpGet("{jobId}")]
        [Authorize]
        public async Task<IActionResult> GetJobStatus(string jobId)
        {
            var userId = User.Claims.First(c => c.Type == "id").Value;
            var job = await _jobService.GetJobByIdAsync(jobId);

            if (job == null)
                return NotFound("Job not found");

            if (job.UserId != userId)
                return Forbid();

            return Ok(new AnalysisJobResponse
            {
                JobId = job.Id!,
                WebsiteUrl = job.WebsiteUrl,
                Status = job.Status,
                CreatedAt = job.CreatedAt,
                CompletedAt = job.CompletedAt,
                Tasks = new TaskResultsResponse
                {
                    Security = job.Tasks.Security != null ? new TaskResultDetail
                    {
                        Status = job.Tasks.Security.Status,
                        Data = job.Tasks.Security.Data,
                        ErrorMessage = job.Tasks.Security.ErrorMessage,
                        CompletedAt = job.Tasks.Security.CompletedAt
                    } : null,
                    Performance = job.Tasks.Performance != null ? new TaskResultDetail
                    {
                        Status = job.Tasks.Performance.Status,
                        Data = job.Tasks.Performance.Data,
                        ErrorMessage = job.Tasks.Performance.ErrorMessage,
                        CompletedAt = job.Tasks.Performance.CompletedAt
                    } : null,
                    UiUx = job.Tasks.UiUx != null ? new TaskResultDetail
                    {
                        Status = job.Tasks.UiUx.Status,
                        Data = job.Tasks.UiUx.Data,
                        ErrorMessage = job.Tasks.UiUx.ErrorMessage,
                        CompletedAt = job.Tasks.UiUx.CompletedAt
                    } : null
                },
                Summary = job.Summary != null ? new OverallSummaryResponse
                {
                    OverallScore = job.Summary.OverallScore,
                    TotalIssues = job.Summary.TotalIssues,
                    CriticalIssues = job.Summary.CriticalIssues
                } : null
            });
        }

        /// <summary>
        /// Gets the detailed analysis results summary for a completed job
        /// This endpoint downloads the results from blob storage and returns structured data for web display
        /// Returns results even if some tasks failed, as long as at least one task succeeded
        /// </summary>
        [HttpGet("{jobId}/results")]
        [Authorize]
        public async Task<IActionResult> GetAnalysisResults(string jobId)
        {
            try
            {
                var userId = User.Claims.First(c => c.Type == "id").Value;
                var job = await _jobService.GetJobByIdAsync(jobId);

                if (job == null)
                    return NotFound("Job not found");

                if (job.UserId != userId)
                    return Forbid();

                if (job.Status == "InProgress" || job.Status == "Pending")
                    return BadRequest("Analysis is still in progress");

                var summary = await _summaryService.GenerateSummaryForWebAsync(userId, jobId);

                if (summary.Performance == null && summary.Security == null && summary.UiUx == null)
                {
                    return BadRequest("No analysis results available. All tasks failed.");
                }

                return Ok(summary);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to get analysis results for job {JobId}", jobId);
                return StatusCode(500, "Failed to retrieve analysis results");
            }
        }

        /// <summary>
        /// Generates and returns the PDF report URL for a completed job
        /// </summary>
        [HttpPost("{jobId}/generate-report")]
        [Authorize]
        public async Task<IActionResult> GeneratePdfReport(string jobId)
        {
            try
            {
                var userId = User.Claims.First(c => c.Type == "id").Value;
                var job = await _jobService.GetJobByIdAsync(jobId);

                if (job == null)
                    return NotFound("Job not found");

                if (job.UserId != userId)
                    return Forbid();

                if (job.Status != "Completed")
                    return BadRequest("Analysis is not yet completed");

                // Generate PDF report
                var pdfUrl = await _pdfReportService.GeneratePdfReportAsync(userId, jobId);

                return Ok(new { pdfUrl });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to generate PDF report for job {JobId}", jobId);
                return StatusCode(500, "Failed to generate PDF report");
            }
        }
    }
}
