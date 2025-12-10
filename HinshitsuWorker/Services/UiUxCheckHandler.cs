using HinshitsuWorker.Models;
using System.Net.Http.Json;
using MongoDB.Bson;

namespace HinshitsuWorker.Services
{
    public class UiUxCheckHandler
    {
        private readonly HttpClient _httpClient;
        private readonly JobUpdateService _jobUpdateService;
        private readonly NotificationService _notificationService;
        private readonly ILogger<UiUxCheckHandler> _logger;
        private readonly string _microserviceUrl;

        public UiUxCheckHandler(
            HttpClient httpClient,
            JobUpdateService jobUpdateService,
            NotificationService notificationService,
            IConfiguration configuration,
            ILogger<UiUxCheckHandler> logger)
        {
            _httpClient = httpClient;
            _jobUpdateService = jobUpdateService;
            _notificationService = notificationService;
            _logger = logger;
            _microserviceUrl = configuration["Microservices:UiUxCheckUrl"]!;
        }

        public async Task ProcessAsync(WorkerTaskMessage message, CancellationToken cancellationToken)
        {
            try
            {

                
                // Send Started notification
                await _notificationService.SendTaskProgressAsync(new TaskProgressNotification
                {
                    JobId = message.JobId!,
                    UserId = message.UserId,
                    TaskType = "UiUx",
                    Status = "Started"
                }, cancellationToken);

                var response = await _httpClient.PostAsJsonAsync(
                    _microserviceUrl,
                    new { url = message.WebsiteUrl },
                    cancellationToken);

                response.EnsureSuccessStatusCode();

                var jsonString = await response.Content.ReadAsStringAsync(cancellationToken);
                var bsonDocument = BsonDocument.Parse(jsonString);

                await _jobUpdateService.UpdateUiUxResultAsync(message.JobId!, bsonDocument, cancellationToken);
            }
            catch (TaskCanceledException ex) when (ex.InnerException is TimeoutException)
            {
                var errorMsg = $"UI/UX check timed out after {_httpClient.Timeout.TotalSeconds}s";
                _logger.LogError(ex, "{ErrorMsg} for JobId: {JobId}", errorMsg, message.JobId);
                await _jobUpdateService.UpdateTaskErrorAsync(message.JobId!, "UiUx", errorMsg, cancellationToken);
            }
            catch (TaskCanceledException ex)
            {
                var errorMsg = "UI/UX check was canceled or timed out";
                _logger.LogError(ex, "{ErrorMsg} for JobId: {JobId}", errorMsg, message.JobId);
                await _jobUpdateService.UpdateTaskErrorAsync(message.JobId!, "UiUx", errorMsg, cancellationToken);
            }
            catch (HttpRequestException ex)
            {
                var errorMsg = $"Network error during UI/UX check: {ex.Message}";
                _logger.LogError(ex, "{ErrorMsg} for JobId: {JobId}", errorMsg, message.JobId);
                await _jobUpdateService.UpdateTaskErrorAsync(message.JobId!, "UiUx", errorMsg, cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "UI/UX check failed for JobId: {JobId}", message.JobId);
                await _jobUpdateService.UpdateTaskErrorAsync(message.JobId!, "UiUx", ex.Message, cancellationToken);
            }
        }
    }
}
