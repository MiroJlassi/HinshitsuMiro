using HinshitsuWebApis.Models;
using MongoDB.Driver;
using Microsoft.Extensions.Configuration;

namespace HinshitsuWebApis.Services
{
    public class AnalysisJobService
    {
        private readonly IMongoCollection<AnalysisJob> _jobs;

        public AnalysisJobService(IConfiguration config)
        {
            var client = new MongoClient(config["MongoSettings:ConnectionString"]);
            var database = client.GetDatabase(config["MongoSettings:DatabaseName"]);
            _jobs = database.GetCollection<AnalysisJob>("AnalysisJobs");
        }

        public async Task<AnalysisJob> CreateJobAsync(string userId, string websiteUrl)
        {
            var job = new AnalysisJob
            {
                UserId = userId,
                WebsiteUrl = websiteUrl,
                Status = "Pending",
                CreatedAt = DateTime.UtcNow
            };

            await _jobs.InsertOneAsync(job);
            return job;
        }

        public async Task<AnalysisJob?> GetJobByIdAsync(string jobId)
        {
            return await _jobs.Find(j => j.Id == jobId).FirstOrDefaultAsync();
        }

        public async Task<List<AnalysisJob>> GetUserJobsAsync(string userId)
        {
            return await _jobs.Find(j => j.UserId == userId)
                .SortByDescending(j => j.CreatedAt)
                .ToListAsync();
        }
    }
}
