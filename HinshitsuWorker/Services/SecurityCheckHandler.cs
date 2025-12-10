using HinshitsuWorker.Models;
using System.Net.Http.Json;
using MongoDB.Bson;
using System.Text.Json;

namespace HinshitsuWorker.Services
{
    public class SecurityCheckHandler
    {
        private readonly HttpClient _httpClient;
        private readonly JobUpdateService _jobUpdateService;
        private readonly NotificationService _notificationService;
        private readonly ILogger<SecurityCheckHandler> _logger;
        private readonly string _microserviceUrl;

        public SecurityCheckHandler(
            HttpClient httpClient,
            JobUpdateService jobUpdateService,
            NotificationService notificationService,
            IConfiguration configuration,
            ILogger<SecurityCheckHandler> logger)
        {
            _httpClient = httpClient;
            _jobUpdateService = jobUpdateService;
            _notificationService = notificationService;
            _logger = logger;
            _microserviceUrl = configuration["Microservices:SecurityCheckUrl"]!;
        }

        public async Task ProcessAsync(WorkerTaskMessage message, CancellationToken cancellationToken)
        {
            try
            {
                _logger.LogInformation("Calling security microservice for {Url}", message.WebsiteUrl);
                
                // Send Started notification
                await _notificationService.SendTaskProgressAsync(new TaskProgressNotification
                {
                    JobId = message.JobId!,
                    UserId = message.UserId,
                    TaskType = "Security",
                    Status = "Started"
                }, cancellationToken);

                var response = await _httpClient.PostAsJsonAsync(
                    _microserviceUrl,
                    new { url = message.WebsiteUrl },
                    cancellationToken);

                response.EnsureSuccessStatusCode();

                var jsonString = await response.Content.ReadAsStringAsync(cancellationToken);
                var bsonDocument = BsonDocument.Parse(jsonString);

                await _jobUpdateService.UpdateSecurityResultAsync(message.JobId!, bsonDocument, cancellationToken);

                _logger.LogInformation("Security check completed for JobId: {JobId}", message.JobId);
            }
            catch (TaskCanceledException ex) when (ex.InnerException is TimeoutException)
            {
                var errorMsg = $"Security check timed out after {_httpClient.Timeout.TotalSeconds}s";
                _logger.LogError(ex, "{ErrorMsg} for JobId: {JobId}", errorMsg, message.JobId);
                await _jobUpdateService.UpdateTaskErrorAsync(message.JobId!, "Security", errorMsg, cancellationToken);
            }
            catch (TaskCanceledException ex)
            {
                var errorMsg = "Security check was canceled or timed out";
                _logger.LogError(ex, "{ErrorMsg} for JobId: {JobId}", errorMsg, message.JobId);
                await _jobUpdateService.UpdateTaskErrorAsync(message.JobId!, "Security", errorMsg, cancellationToken);
            }
            catch (HttpRequestException ex)
            {
                var errorMsg = $"Network error during security check: {ex.Message}";
                _logger.LogError(ex, "{ErrorMsg} for JobId: {JobId}", errorMsg, message.JobId);
                await _jobUpdateService.UpdateTaskErrorAsync(message.JobId!, "Security", errorMsg, cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Security check failed for JobId: {JobId}", message.JobId);
                await _jobUpdateService.UpdateTaskErrorAsync(message.JobId!, "Security", ex.Message, cancellationToken);
            }
        }
    }
}
