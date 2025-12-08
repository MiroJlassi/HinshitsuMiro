using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace HinshitsuService.Models
{
    public class TaskItem
    {
        [BsonId]
        [BsonRepresentation(BsonType.ObjectId)]
        public string Id { get; set; }

        public string Url { get; set; }
        public string UserId { get; set; }
        public string Status { get; set; } // Pending, Processing, Done
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
