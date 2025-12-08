using MongoDB.Driver;
using HinshitsuService.Models;

namespace HinshitsuService.Services
{
    public class TaskService
    {
        private readonly IMongoCollection<TaskItem> _tasks;

        public TaskService(IConfiguration config)
        {
            var client = new MongoClient(config["MongoSettings:ConnectionString"]);
            var db = client.GetDatabase(config["MongoSettings:DatabaseName"]);
            _tasks = db.GetCollection<TaskItem>(config["MongoSettings:TasksCollection"]);
        }

        public async Task<TaskItem> CreateAsync(TaskItem task)
        {
            await _tasks.InsertOneAsync(task);
            return task;
        }

        public async Task<List<TaskItem>> GetPendingTasksAsync() =>
            await _tasks.Find(t => t.Status == "Pending").ToListAsync();

        public async Task UpdateAsync(TaskItem task) =>
            await _tasks.ReplaceOneAsync(t => t.Id == task.Id, task);
    }
}
