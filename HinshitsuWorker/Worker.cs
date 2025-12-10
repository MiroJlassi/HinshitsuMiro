using HinshitsuWorker.Services;

namespace HinshitsuWorker
{
    public class Worker : BackgroundService
    {
        private readonly ILogger<Worker> _logger;
        private readonly QueueProcessor _queueProcessor;
        private readonly BlobStorageService _blobStorageService;

        public Worker(
            ILogger<Worker> logger, 
            QueueProcessor queueProcessor,
            BlobStorageService blobStorageService)
        {
            _logger = logger;
            _queueProcessor = queueProcessor;
            _blobStorageService = blobStorageService;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("Worker started at: {time}", DateTimeOffset.Now);

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
    }
}
