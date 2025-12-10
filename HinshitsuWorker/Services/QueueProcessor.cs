using Azure.Storage.Queues;
using Azure.Storage.Queues.Models;
using HinshitsuWorker.Models;
using System.Text.Json;

namespace HinshitsuWorker.Services
{
    public class QueueProcessor
    {
        private readonly string _connectionString;
        private readonly ILogger<QueueProcessor> _logger;
        private readonly IServiceProvider _serviceProvider;

        public QueueProcessor(
            IConfiguration configuration,
            ILogger<QueueProcessor> logger,
            IServiceProvider serviceProvider)
        {
            _connectionString = configuration["AzureStorage:ConnectionString"]!;
            _logger = logger;
            _serviceProvider = serviceProvider;
        }

        public async Task ProcessQueuesAsync(CancellationToken stoppingToken)
        {
            var queueNames = new[] { "security-checks", "performance-checks", "uiux-checks" };
            var queueClients = queueNames.Select(name => new QueueClient(_connectionString, name)).ToArray();

            // Ensure queues exist
            foreach (var client in queueClients)
            {
                await client.CreateIfNotExistsAsync(cancellationToken: stoppingToken);
            }

            while (!stoppingToken.IsCancellationRequested)
            {
                bool foundMessage = false;

                foreach (var queueClient in queueClients)
                {
                    try
                    {
                        QueueMessage[]? messages = await queueClient.ReceiveMessagesAsync(
                            maxMessages: 1,
                            cancellationToken: stoppingToken);

                        if (messages != null && messages.Length > 0)
                        {
                            foundMessage = true;
                            var message = messages[0];
                            await ProcessMessageAsync(queueClient.Name, message, stoppingToken);
                            await queueClient.DeleteMessageAsync(message.MessageId, message.PopReceipt, stoppingToken);
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Error processing queue {QueueName}", queueClient.Name);
                    }
                }

                // If no messages found, wait longer before polling again
                var delayMs = foundMessage ? 500 : 5000;
                await Task.Delay(delayMs, stoppingToken);
            }
        }

        private async Task ProcessMessageAsync(string queueName, QueueMessage message, CancellationToken cancellationToken)
        {
            try
            {
                var taskMessage = JsonSerializer.Deserialize<WorkerTaskMessage>(message.MessageText);
                if (taskMessage == null) return;

                using var scope = _serviceProvider.CreateScope();
                
                switch (taskMessage.TaskType.ToLower())
                {
                    case "security":
                        var securityHandler = scope.ServiceProvider.GetRequiredService<SecurityCheckHandler>();
                        await securityHandler.ProcessAsync(taskMessage, cancellationToken);
                        break;
                    case "performance":
                        var performanceHandler = scope.ServiceProvider.GetRequiredService<PerformanceCheckHandler>();
                        await performanceHandler.ProcessAsync(taskMessage, cancellationToken);
                        break;
                    case "uiux":
                        var uiuxHandler = scope.ServiceProvider.GetRequiredService<UiUxCheckHandler>();
                        await uiuxHandler.ProcessAsync(taskMessage, cancellationToken);
                        break;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing message from {QueueName}", queueName);
            }
        }
    }
}
