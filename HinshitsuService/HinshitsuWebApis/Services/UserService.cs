using MongoDB.Driver;
using HinshitsuService.Models;

namespace HinshitsuService.Services
{
    public class UserService
    {
        private readonly IMongoCollection<User> _users;

        public UserService(IConfiguration config)
        {
            var client = new MongoClient(config["MongoSettings:ConnectionString"]);
            var db = client.GetDatabase(config["MongoSettings:DatabaseName"]);
            _users = db.GetCollection<User>(config["MongoSettings:UsersCollection"]);
        }

        public async Task<User> GetByEmailAsync(string email) =>
            await _users.Find(u => u.Email == email).FirstOrDefaultAsync();

        public async Task<User> GetByVerificationCodeAsync(string email, string code) =>
            await _users.Find(u => u.Email == email && u.VerificationCode == code).FirstOrDefaultAsync();

        public async Task CreateAsync(User user) =>
            await _users.InsertOneAsync(user);

        public async Task UpdateAsync(User user) =>
            await _users.ReplaceOneAsync(u => u.Id == user.Id, user);
    }
}
