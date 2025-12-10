using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Azure.Storage.Sas;

namespace HinshitsuWebApis.Services;

/// <summary>
/// Service for interacting with Azure Blob Storage
/// </summary>
public class BlobStorageService
{
    private readonly string _connectionString;
    private readonly ILogger<BlobStorageService> _logger;

    public BlobStorageService(string connectionString, ILogger<BlobStorageService> logger)
    {
        _connectionString = connectionString;
        _logger = logger;
    }

    /// <summary>
    /// Downloads a blob as a string
    /// </summary>
    /// <param name="containerName">Container name</param>
    /// <param name="blobPath">Blob path (folder/filename)</param>
    /// <returns>Blob content as string</returns>
    public async Task<string> DownloadBlobAsStringAsync(string containerName, string blobPath)
    {
        try
        {
            var blobServiceClient = new BlobServiceClient(_connectionString);
            var containerClient = blobServiceClient.GetBlobContainerClient(containerName);
            var blobClient = containerClient.GetBlobClient(blobPath);

            _logger.LogInformation("Downloading blob: {ContainerName}/{BlobPath}", containerName, blobPath);

            var response = await blobClient.DownloadContentAsync();
            var content = response.Value.Content.ToString();

            _logger.LogInformation("Successfully downloaded blob: {BlobPath} ({Size} bytes)", blobPath, content.Length);
            return content;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to download blob: {ContainerName}/{BlobPath}", containerName, blobPath);
            throw;
        }
    }

    /// <summary>
    /// Downloads a blob as byte array
    /// </summary>
    /// <param name="containerName">Container name</param>
    /// <param name="blobPath">Blob path (folder/filename)</param>
    /// <returns>Blob content as byte array</returns>
    public async Task<byte[]> DownloadBlobAsBytesAsync(string containerName, string blobPath)
    {
        try
        {
            var blobServiceClient = new BlobServiceClient(_connectionString);
            var containerClient = blobServiceClient.GetBlobContainerClient(containerName);
            var blobClient = containerClient.GetBlobClient(blobPath);

            _logger.LogInformation("Downloading blob: {ContainerName}/{BlobPath}", containerName, blobPath);

            var response = await blobClient.DownloadContentAsync();
            var content = response.Value.Content.ToArray();

            _logger.LogInformation("Successfully downloaded blob: {BlobPath} ({Size} bytes)", blobPath, content.Length);
            return content;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to download blob: {ContainerName}/{BlobPath}", containerName, blobPath);
            throw;
        }
    }

    /// <summary>
    /// Uploads a blob from byte array
    /// </summary>
    /// <param name="containerName">Container name</param>
    /// <param name="blobPath">Blob path (folder/filename)</param>
    /// <param name="content">Content to upload</param>
    /// <param name="contentType">Content type (e.g., "application/pdf")</param>
    public async Task UploadBlobAsync(string containerName, string blobPath, byte[] content, string contentType = "application/octet-stream")
    {
        try
        {
            var blobServiceClient = new BlobServiceClient(_connectionString);
            var containerClient = blobServiceClient.GetBlobContainerClient(containerName);
            
            // Create container if it doesn't exist
            await containerClient.CreateIfNotExistsAsync(PublicAccessType.None);
            
            var blobClient = containerClient.GetBlobClient(blobPath);

            _logger.LogInformation("Uploading blob: {ContainerName}/{BlobPath} ({Size} bytes)", containerName, blobPath, content.Length);

            using var stream = new MemoryStream(content);
            await blobClient.UploadAsync(stream, new BlobHttpHeaders { ContentType = contentType }, conditions: null);

            _logger.LogInformation("Successfully uploaded blob: {BlobPath}", blobPath);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to upload blob: {ContainerName}/{BlobPath}", containerName, blobPath);
            throw;
        }
    }

    /// <summary>
    /// Generates a SAS URL for a blob with read permissions
    /// </summary>
    /// <param name="containerName">Container name</param>
    /// <param name="blobPath">Blob path (folder/filename)</param>
    /// <param name="expirationHours">Hours until the SAS token expires (default 168 = 7 days)</param>
    /// <returns>URL with SAS token</returns>
    public string GenerateSasUrl(string containerName, string blobPath, int expirationHours = 168)
    {
        try
        {
            var blobServiceClient = new BlobServiceClient(_connectionString);
            var containerClient = blobServiceClient.GetBlobContainerClient(containerName);
            var blobClient = containerClient.GetBlobClient(blobPath);

            // Check if the blob client can generate SAS tokens (requires account key)
            if (!blobClient.CanGenerateSasUri)
            {
                _logger.LogWarning("Cannot generate SAS URL - connection string may not contain account key");
                return blobClient.Uri.ToString();
            }

            var sasBuilder = new BlobSasBuilder
            {
                BlobContainerName = containerName,
                BlobName = blobPath,
                Resource = "b", // "b" for blob
                StartsOn = DateTimeOffset.UtcNow.AddMinutes(-5), // Allow 5 minutes clock skew
                ExpiresOn = DateTimeOffset.UtcNow.AddHours(expirationHours)
            };

            // Grant read permissions
            sasBuilder.SetPermissions(BlobSasPermissions.Read);

            var sasUri = blobClient.GenerateSasUri(sasBuilder);
            
            _logger.LogInformation("Generated SAS URL for blob: {BlobPath} (expires in {Hours} hours)", blobPath, expirationHours);
            
            return sasUri.ToString();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to generate SAS URL: {ContainerName}/{BlobPath}", containerName, blobPath);
            throw;
        }
    }

    /// <summary>
    /// Checks if a blob exists
    /// </summary>
    /// <param name="containerName">Container name</param>
    /// <param name="blobPath">Blob path (folder/filename)</param>
    /// <returns>True if blob exists</returns>
    public async Task<bool> BlobExistsAsync(string containerName, string blobPath)
    {
        try
        {
            var blobServiceClient = new BlobServiceClient(_connectionString);
            var containerClient = blobServiceClient.GetBlobContainerClient(containerName);
            var blobClient = containerClient.GetBlobClient(blobPath);

            return await blobClient.ExistsAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to check blob existence: {ContainerName}/{BlobPath}", containerName, blobPath);
            return false;
        }
    }

    /// <summary>
    /// Lists all blobs in a folder (prefix)
    /// </summary>
    /// <param name="containerName">Container name</param>
    /// <param name="prefix">Folder prefix (e.g., "user123_req456/")</param>
    /// <returns>List of blob names</returns>
    public async Task<List<string>> ListBlobsAsync(string containerName, string prefix)
    {
        try
        {
            var blobServiceClient = new BlobServiceClient(_connectionString);
            var containerClient = blobServiceClient.GetBlobContainerClient(containerName);

            var blobs = new List<string>();
            
            await foreach (var blobItem in containerClient.GetBlobsAsync(prefix: prefix))
            {
                blobs.Add(blobItem.Name);
            }

            _logger.LogInformation("Listed {Count} blobs in {ContainerName} with prefix {Prefix}", blobs.Count, containerName, prefix);
            return blobs;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to list blobs: {ContainerName}/{Prefix}", containerName, prefix);
            throw;
        }
    }
}
