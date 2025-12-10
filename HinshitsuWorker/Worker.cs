using HinshitsuWorker.Services;

namespace HinshitsuWorker
{
    public class Worker : BackgroundService
    {
        private readonly ILogger<Worker> _logger;
        private readonly QueueProcessor _queueProcessor;
        private readonly BlobStorageService _blobStorageService;
        private readonly IConfiguration _configuration;

        public Worker(
            ILogger<Worker> logger, 
            QueueProcessor queueProcessor,
            BlobStorageService blobStorageService,
            IConfiguration configuration)
        {
            _logger = logger;
            _queueProcessor = queueProcessor;
            _blobStorageService = blobStorageService;
            _configuration = configuration;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("========================================");
            _logger.LogInformation("Worker Service Starting...");
            _logger.LogInformation("Started at: {time}", DateTimeOffset.Now);
            _logger.LogInformation("========================================");

            // Log all configuration values (masked for security)
            LogConfiguration();

            try
            {
                // Initialize blob storage container
                await _blobStorageService.InitializeContainerAsync(stoppingToken);
                
                await _queueProcessor.ProcessQueuesAsync(stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Worker encountered an error");
            }
        }

        private void LogConfiguration()
        {
            _logger.LogInformation("========================================");
            _logger.LogInformation("Configuration Values:");
            _logger.LogInformation("========================================");

            var configValues = new Dictionary<string, string?>
            {
                { "AzureStorage:ConnectionString", _configuration["AzureStorage:ConnectionString"] },
                { "MongoSettings:ConnectionString", _configuration["MongoSettings:ConnectionString"] },
                { "MongoSettings:DatabaseName", _configuration["MongoSettings:DatabaseName"] },
                { "ServiceSettings:BaseUrl", _configuration["ServiceSettings:BaseUrl"] },
                { "Microservices:SecurityCheckUrl", _configuration["Microservices:SecurityCheckUrl"] },
                { "Microservices:PerformanceCheckUrl", _configuration["Microservices:PerformanceCheckUrl"] },
                { "Microservices:UiUxCheckUrl", _configuration["Microservices:UiUxCheckUrl"] },
                { "HttpClientSettings:TimeoutSeconds", _configuration["HttpClientSettings:TimeoutSeconds"] },
                { "HttpClientSettings:RetryCount", _configuration["HttpClientSettings:RetryCount"] },
                { "HttpClientSettings:RetryDelaySeconds", _configuration["HttpClientSettings:RetryDelaySeconds"] },
                { "ApplicationInsights:ConnectionString", _configuration["ApplicationInsights:ConnectionString"] }
            };

            foreach (var kvp in configValues)
            {
                var value = kvp.Value;
                var maskedValue = MaskSensitiveValue(kvp.Key, value);
                _logger.LogInformation("  {Key}: {Value}", kvp.Key, maskedValue);
            }

            _logger.LogInformation("========================================");
        }

        private string MaskSensitiveValue(string key, string? value)
        {
            if (string.IsNullOrEmpty(value))
            {
                return "[NOT SET]";
            }

            // Keys that should be fully or partially masked
            var sensitiveKeys = new[] { "ConnectionString", "Key", "Secret", "Password", "Token" };
            bool isSensitive = sensitiveKeys.Any(k => key.Contains(k, StringComparison.OrdinalIgnoreCase));

            if (isSensitive)
            {
                // Show first 10 characters then mask the rest
                if (value.Length <= 10)
                {
                    return "**********";
                }
                return $"{value.Substring(0, 10)}*******";
            }

            // For URLs and non-sensitive values, show in full
            return value;
        }
    }
}
