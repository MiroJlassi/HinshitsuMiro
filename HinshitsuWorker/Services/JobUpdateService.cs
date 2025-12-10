using HinshitsuWorker.Models;
using MongoDB.Driver;

namespace HinshitsuWorker.Services
{
    public class JobUpdateService
    {
        private readonly IMongoCollection<AnalysisJob> _jobs;
        private readonly ILogger<JobUpdateService> _logger;
        private readonly NotificationService _notificationService;
        private readonly BlobStorageService _blobStorageService;

        public JobUpdateService(
            IConfiguration configuration, 
            ILogger<JobUpdateService> logger,
            NotificationService notificationService,
            BlobStorageService blobStorageService)
        {
            var client = new MongoClient(configuration["MongoSettings:ConnectionString"]);
            var database = client.GetDatabase(configuration["MongoSettings:DatabaseName"]);
            _jobs = database.GetCollection<AnalysisJob>("AnalysisJobs");
            _logger = logger;
            _notificationService = notificationService;
            _blobStorageService = blobStorageService;
        }

        public async Task UpdateSecurityResultAsync(string jobId, object data, CancellationToken cancellationToken)
        {
            // Ensure the task object exists
            var initUpdate = Builders<AnalysisJob>.Update
                .Set("Tasks.Security", new TaskResult());
            await _jobs.UpdateOneAsync(
                j => j.Id == jobId && j.Tasks.Security == null, 
                initUpdate,
                cancellationToken: cancellationToken);

            var update = Builders<AnalysisJob>.Update
                .Set("Tasks.Security.Status", "Completed")
                .Set("Tasks.Security.Data", data)
                .Set("Tasks.Security.CompletedAt", DateTime.UtcNow);

            await _jobs.UpdateOneAsync(j => j.Id == jobId, update, cancellationToken: cancellationToken);
            
            // Send task progress notification
            var job = await _jobs.Find(j => j.Id == jobId).FirstOrDefaultAsync(cancellationToken);
            if (job != null)
            {
                // Save to blob storage
                await _blobStorageService.SaveAnalysisResultAsync(
                    job.UserId, 
                    jobId, 
                    "security", 
                    data, 
                    cancellationToken);

                // Convert BsonDocument to JSON string for notification
                var dataForNotification = data is MongoDB.Bson.BsonDocument bsonDoc 
                    ? MongoDB.Bson.BsonExtensionMethods.ToJson(bsonDoc) 
                    : data;

                await _notificationService.SendTaskProgressAsync(new TaskProgressNotification
                {
                    JobId = jobId,
                    UserId = job.UserId,
                    TaskType = "Security",
                    Status = "Completed",
                    Data = dataForNotification
                }, cancellationToken);
            }
            
            await CheckAndUpdateJobStatusAsync(jobId, cancellationToken);
        }

        public async Task UpdatePerformanceResultAsync(string jobId, object data, CancellationToken cancellationToken)
        {
            // Ensure the task object exists
            var initUpdate = Builders<AnalysisJob>.Update
                .Set("Tasks.Performance", new TaskResult());
            await _jobs.UpdateOneAsync(
                j => j.Id == jobId && j.Tasks.Performance == null, 
                initUpdate,
                cancellationToken: cancellationToken);

            var update = Builders<AnalysisJob>.Update
                .Set("Tasks.Performance.Status", "Completed")
                .Set("Tasks.Performance.Data", data)
                .Set("Tasks.Performance.CompletedAt", DateTime.UtcNow);

            await _jobs.UpdateOneAsync(j => j.Id == jobId, update, cancellationToken: cancellationToken);
            
            // Send task progress notification
            var job = await _jobs.Find(j => j.Id == jobId).FirstOrDefaultAsync(cancellationToken);
            if (job != null)
            {
                // Save to blob storage
                await _blobStorageService.SaveAnalysisResultAsync(
                    job.UserId, 
                    jobId, 
                    "performance", 
                    data, 
                    cancellationToken);

                // Convert BsonDocument to JSON string for notification
                var dataForNotification = data is MongoDB.Bson.BsonDocument bsonDoc 
                    ? MongoDB.Bson.BsonExtensionMethods.ToJson(bsonDoc) 
                    : data;

                await _notificationService.SendTaskProgressAsync(new TaskProgressNotification
                {
                    JobId = jobId,
                    UserId = job.UserId,
                    TaskType = "Performance",
                    Status = "Completed",
                    Data = dataForNotification
                }, cancellationToken);
            }
            
            await CheckAndUpdateJobStatusAsync(jobId, cancellationToken);
        }

        public async Task UpdateUiUxResultAsync(string jobId, object data, CancellationToken cancellationToken)
        {
            // Ensure the task object exists
            var initUpdate = Builders<AnalysisJob>.Update
                .Set("Tasks.UiUx", new TaskResult());
            await _jobs.UpdateOneAsync(
                j => j.Id == jobId && j.Tasks.UiUx == null, 
                initUpdate,
                cancellationToken: cancellationToken);

            var update = Builders<AnalysisJob>.Update
                .Set("Tasks.UiUx.Status", "Completed")
                .Set("Tasks.UiUx.Data", data)
                .Set("Tasks.UiUx.CompletedAt", DateTime.UtcNow);

            await _jobs.UpdateOneAsync(j => j.Id == jobId, update, cancellationToken: cancellationToken);
            
            // Send task progress notification
            var job = await _jobs.Find(j => j.Id == jobId).FirstOrDefaultAsync(cancellationToken);
            if (job != null)
            {
                // Save to blob storage
                await _blobStorageService.SaveAnalysisResultAsync(
                    job.UserId, 
                    jobId, 
                    "uiux", 
                    data, 
                    cancellationToken);

                // Convert BsonDocument to JSON string for notification
                var dataForNotification = data is MongoDB.Bson.BsonDocument bsonDoc 
                    ? MongoDB.Bson.BsonExtensionMethods.ToJson(bsonDoc) 
                    : data;

                await _notificationService.SendTaskProgressAsync(new TaskProgressNotification
                {
                    JobId = jobId,
                    UserId = job.UserId,
                    TaskType = "UiUx",
                    Status = "Completed",
                    Data = dataForNotification
                }, cancellationToken);
            }
            
            await CheckAndUpdateJobStatusAsync(jobId, cancellationToken);
        }

        public async Task UpdateTaskErrorAsync(string jobId, string taskType, string errorMessage, CancellationToken cancellationToken)
        {
            // Ensure the task object exists
            var filter = Builders<AnalysisJob>.Filter.And(
                Builders<AnalysisJob>.Filter.Eq(j => j.Id, jobId),
                Builders<AnalysisJob>.Filter.Eq<TaskResult?>($"Tasks.{taskType}", null)
            );
            var initUpdate = Builders<AnalysisJob>.Update
                .Set($"Tasks.{taskType}", new TaskResult());
            await _jobs.UpdateOneAsync(filter, initUpdate, cancellationToken: cancellationToken);

            var update = Builders<AnalysisJob>.Update
                .Set($"Tasks.{taskType}.Status", "Failed")
                .Set($"Tasks.{taskType}.ErrorMessage", errorMessage)
                .Set($"Tasks.{taskType}.CompletedAt", DateTime.UtcNow);

            await _jobs.UpdateOneAsync(j => j.Id == jobId, update, cancellationToken: cancellationToken);
            
            // Send task progress notification
            var job = await _jobs.Find(j => j.Id == jobId).FirstOrDefaultAsync(cancellationToken);
            if (job != null)
            {
                await _notificationService.SendTaskProgressAsync(new TaskProgressNotification
                {
                    JobId = jobId,
                    UserId = job.UserId,
                    TaskType = taskType,
                    Status = "Failed",
                    ErrorMessage = errorMessage
                }, cancellationToken);
            }
            
            await CheckAndUpdateJobStatusAsync(jobId, cancellationToken);
        }

        private async Task CheckAndUpdateJobStatusAsync(string jobId, CancellationToken cancellationToken)
        {
            var job = await _jobs.Find(j => j.Id == jobId).FirstOrDefaultAsync(cancellationToken);
            if (job == null) return;

            // Check if all tasks have completed (either succeeded or failed)
            var securityDone = job.Tasks.Security?.Status == "Completed" || job.Tasks.Security?.Status == "Failed";
            var performanceDone = job.Tasks.Performance?.Status == "Completed" || job.Tasks.Performance?.Status == "Failed";
            var uiuxDone = job.Tasks.UiUx?.Status == "Completed" || job.Tasks.UiUx?.Status == "Failed";

            var allTasksDone = securityDone && performanceDone && uiuxDone;

            if (allTasksDone)
            {
                // Count successful and failed tasks
                var successCount = 0;
                var failCount = 0;

                if (job.Tasks.Security?.Status == "Completed") successCount++;
                else if (job.Tasks.Security?.Status == "Failed") failCount++;

                if (job.Tasks.Performance?.Status == "Completed") successCount++;
                else if (job.Tasks.Performance?.Status == "Failed") failCount++;

                if (job.Tasks.UiUx?.Status == "Completed") successCount++;
                else if (job.Tasks.UiUx?.Status == "Failed") failCount++;

                // Determine final status:
                // - "Completed" if at least one task succeeded
                // - "Failed" only if all tasks failed
                string finalStatus;
                if (successCount > 0)
                {
                    finalStatus = "Completed";
                }
                else
                {
                    finalStatus = "Failed";
                    _logger.LogWarning("Job {JobId} failed - all tasks failed", jobId);
                }

                var update = Builders<AnalysisJob>.Update
                    .Set("Status", finalStatus)
                    .Set("CompletedAt", DateTime.UtcNow);

                await _jobs.UpdateOneAsync(j => j.Id == jobId, update, cancellationToken: cancellationToken);
                
                // Send job completion notification
                await _notificationService.SendJobCompletionAsync(new JobCompletionNotification
                {
                    JobId = jobId,
                    UserId = job.UserId,
                    Status = finalStatus,
                    Tasks = new TaskCompletionSummary
                    {
                        SecurityStatus = job.Tasks.Security?.Status ?? "Pending",
                        PerformanceStatus = job.Tasks.Performance?.Status ?? "Pending",
                        UiUxStatus = job.Tasks.UiUx?.Status ?? "Pending"
                    }
                }, cancellationToken);
            }
        }
    }
}
