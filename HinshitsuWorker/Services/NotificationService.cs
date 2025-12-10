using HinshitsuWorker.Models;
using System.Net.Http.Json;

namespace HinshitsuWorker.Services
{
    public class NotificationService
    {
        private readonly HttpClient _httpClient;
        private readonly ILogger<NotificationService> _logger;
        private readonly string _serviceBaseUrl;

        public NotificationService(
            HttpClient httpClient,
            IConfiguration configuration,
            ILogger<NotificationService> logger)
        {
            _httpClient = httpClient;
            _logger = logger;
            _serviceBaseUrl = configuration["ServiceSettings:BaseUrl"]!;
        }

        public async Task SendTaskProgressAsync(TaskProgressNotification notification, CancellationToken cancellationToken)
        {
            try
            {
                var response = await _httpClient.PostAsJsonAsync(
                    $"{_serviceBaseUrl}/api/notifications/task-progress",
                    notification,
                    cancellationToken);

                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogWarning("Failed to send task progress notification. Status: {StatusCode}", response.StatusCode);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending task progress notification for JobId: {JobId}", notification.JobId);
                // Don't throw - notification failure shouldn't break the worker
            }
        }

        public async Task SendJobCompletionAsync(JobCompletionNotification notification, CancellationToken cancellationToken)
        {
            try
            {
                var response = await _httpClient.PostAsJsonAsync(
                    $"{_serviceBaseUrl}/api/notifications/job-completion",
                    notification,
                    cancellationToken);

                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogWarning("Failed to send job completion notification. Status: {StatusCode}", response.StatusCode);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending job completion notification for JobId: {JobId}", notification.JobId);
                // Don't throw - notification failure shouldn't break the worker
            }
        }
    }
}
