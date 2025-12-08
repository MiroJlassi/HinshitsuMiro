using Azure.Storage.Queues;
using System.Text.Json;

namespace HinshitsuWebApis.Services
{
    public class QueueService
    {
        private readonly string _connectionString;

        public QueueService(string connectionString)
        {
            _connectionString = connectionString;
        }

        public async Task EnqueueMessageAsync<T>(string queueName, T message)
        {
            var queueClient = new QueueClient(_connectionString, queueName);
            await queueClient.CreateIfNotExistsAsync();

            var messageJson = JsonSerializer.Serialize(message);
            await queueClient.SendMessageAsync(messageJson);
        }
    }
}
