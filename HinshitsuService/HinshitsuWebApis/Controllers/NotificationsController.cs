using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using HinshitsuWebApis.Hubs;
using HinshitsuWebApis.Models;

namespace HinshitsuWebApis.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class NotificationsController : ControllerBase
    {
        private readonly IHubContext<NotificationHub> _hubContext;
        private readonly ILogger<NotificationsController> _logger;

        public NotificationsController(
            IHubContext<NotificationHub> hubContext,
            ILogger<NotificationsController> logger)
        {
            _hubContext = hubContext;
            _logger = logger;
        }

        [HttpPost("task-progress")]
        public async Task<IActionResult> TaskProgress([FromBody] TaskProgressNotification notification)
        {
            _logger.LogInformation(
                "Received task progress: JobId={JobId}, Task={TaskType}, Status={Status}",
                notification.JobId, notification.TaskType, notification.Status);

            // Broadcast to all clients in the job group
            await _hubContext.Clients.Group($"job_{notification.JobId}")
                .SendAsync("TaskProgress", notification);

            // Also broadcast to the user's group
            await _hubContext.Clients.Group($"user_{notification.UserId}")
                .SendAsync("TaskProgress", notification);

            return Ok(new { message = "Notification sent" });
        }

        [HttpPost("job-completion")]
        public async Task<IActionResult> JobCompletion([FromBody] JobCompletionNotification notification)
        {
            _logger.LogInformation(
                "Received job completion: JobId={JobId}, Status={Status}",
                notification.JobId, notification.Status);

            // Broadcast job completion
            await _hubContext.Clients.Group($"job_{notification.JobId}")
                .SendAsync("JobCompleted", notification);

            await _hubContext.Clients.Group($"user_{notification.UserId}")
                .SendAsync("JobCompleted", notification);

            return Ok(new { message = "Completion notification sent" });
        }
    }
}
