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
        private readonly ILogger<AnalysisController> _logger;

        public AnalysisController(
            AnalysisJobService jobService,
            QueueService queueService,
            ILogger<AnalysisController> logger)
        {
            _jobService = jobService;
            _queueService = queueService;
            _logger = logger;
        }

        [HttpPost("submit")]
        [Authorize]
        public async Task<IActionResult> SubmitAnalysis([FromBody] SubmitAnalysisRequest request)
        {
            var userId = User.Claims.First(c => c.Type == "id").Value;

            var job = await _jobService.CreateJobAsync(userId, request.WebsiteUrl);

            var securityTask = new WorkerTaskMessage
            {
                JobId = job.Id,
                UserId = userId,
                WebsiteUrl = request.WebsiteUrl,
                TaskType = "security",
                RequestedAt = DateTime.UtcNow
            };

            var performanceTask = new WorkerTaskMessage
            {
                JobId = job.Id,
                UserId = userId,
                WebsiteUrl = request.WebsiteUrl,
                TaskType = "performance",
                RequestedAt = DateTime.UtcNow
            };

            var uiuxTask = new WorkerTaskMessage
            {
                JobId = job.Id,
                UserId = userId,
                WebsiteUrl = request.WebsiteUrl,
                TaskType = "uiux",
                RequestedAt = DateTime.UtcNow
            };

            await _queueService.EnqueueMessageAsync("security-checks", securityTask);
            await _queueService.EnqueueMessageAsync("performance-checks", performanceTask);
            await _queueService.EnqueueMessageAsync("uiux-checks", uiuxTask);

            _logger.LogInformation("Analysis job {JobId} submitted with 3 worker tasks for website: {WebsiteUrl}", job.Id, request.WebsiteUrl);

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
    }
}
