using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using System.Text;
using System.Text.Json;

namespace HinshitsuWorker.Services
{
    public class BlobStorageService
    {
        private readonly BlobServiceClient _blobServiceClient;
        private readonly ILogger<BlobStorageService> _logger;
        private const string ContainerName = "analysis-results";

        public BlobStorageService(IConfiguration configuration, ILogger<BlobStorageService> logger)
        {
            var connectionString = configuration["AzureStorage:ConnectionString"];
            _blobServiceClient = new BlobServiceClient(connectionString);
            _logger = logger;
        }

        public async Task InitializeContainerAsync(CancellationToken cancellationToken = default)
        {
            try
            {
                var containerClient = _blobServiceClient.GetBlobContainerClient(ContainerName);
                await containerClient.CreateIfNotExistsAsync(
                    PublicAccessType.None, 
                    cancellationToken: cancellationToken);
                
                _logger.LogInformation("Container '{ContainerName}' is ready", ContainerName);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to initialize container '{ContainerName}'", ContainerName);
                throw;
            }
        }

        public async Task SaveAnalysisResultAsync(
            string userId, 
            string jobId, 
            string taskType, 
            object data, 
            CancellationToken cancellationToken = default)
        {
            try
            {
                var containerClient = _blobServiceClient.GetBlobContainerClient(ContainerName);
                
                // Create folder path: userId_jobId/taskType.json
                var blobPath = $"{userId}_{jobId}/{taskType.ToLower()}.json";
                var blobClient = containerClient.GetBlobClient(blobPath);

                // Serialize data to JSON - handle BsonDocument specially
                string jsonString;
                if (data is MongoDB.Bson.BsonDocument bsonDoc)
                {
                    jsonString = MongoDB.Bson.BsonExtensionMethods.ToJson(bsonDoc, new MongoDB.Bson.IO.JsonWriterSettings 
                    { 
                        Indent = true 
                    });
                }
                else
                {
                    jsonString = JsonSerializer.Serialize(data, new JsonSerializerOptions 
                    { 
                        WriteIndented = true 
                    });
                }
                var bytes = Encoding.UTF8.GetBytes(jsonString);

                // Upload to blob storage
                using var stream = new MemoryStream(bytes);
                await blobClient.UploadAsync(
                    stream, 
                    overwrite: true,
                    cancellationToken: cancellationToken);

                _logger.LogInformation(
                    "Saved {TaskType} analysis result to blob: {BlobPath}", 
                    taskType, 
                    blobPath);
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex, 
                    "Failed to save {TaskType} result for JobId: {JobId}", 
                    taskType, 
                    jobId);
                // Don't throw - blob storage failure shouldn't break the worker
            }
        }

        public async Task<string?> GetAnalysisResultAsync(
            string userId, 
            string jobId, 
            string taskType, 
            CancellationToken cancellationToken = default)
        {
            try
            {
                var containerClient = _blobServiceClient.GetBlobContainerClient(ContainerName);
                var blobPath = $"{userId}_{jobId}/{taskType.ToLower()}.json";
                var blobClient = containerClient.GetBlobClient(blobPath);

                if (!await blobClient.ExistsAsync(cancellationToken))
                {
                    return null;
                }

                var response = await blobClient.DownloadContentAsync(cancellationToken);
                return response.Value.Content.ToString();
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex, 
                    "Failed to retrieve {TaskType} result for JobId: {JobId}", 
                    taskType, 
                    jobId);
                return null;
            }
        }

        public async Task<List<string>> ListJobFilesAsync(
            string userId, 
            string jobId, 
            CancellationToken cancellationToken = default)
        {
            var files = new List<string>();
            
            try
            {
                var containerClient = _blobServiceClient.GetBlobContainerClient(ContainerName);
                var prefix = $"{userId}_{jobId}/";

                await foreach (var blobItem in containerClient.GetBlobsAsync(
                    prefix: prefix, 
                    cancellationToken: cancellationToken))
                {
                    files.Add(blobItem.Name);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to list files for JobId: {JobId}", jobId);
            }

            return files;
        }

        public async Task DeleteJobResultsAsync(
            string userId, 
            string jobId, 
            CancellationToken cancellationToken = default)
        {
            try
            {
                var containerClient = _blobServiceClient.GetBlobContainerClient(ContainerName);
                var prefix = $"{userId}_{jobId}/";

                await foreach (var blobItem in containerClient.GetBlobsAsync(
                    prefix: prefix, 
                    cancellationToken: cancellationToken))
                {
                    await containerClient.DeleteBlobAsync(blobItem.Name, cancellationToken: cancellationToken);
                }

                _logger.LogInformation("Deleted all results for JobId: {JobId}", jobId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to delete results for JobId: {JobId}", jobId);
            }
        }
    }
}
