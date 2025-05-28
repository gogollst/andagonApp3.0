using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace andagonApp3.Models
{
    public class TimeEntry
    {
        [BsonId]
        [BsonRepresentation(BsonType.ObjectId)]
        public string? Id { get; set; }

        public string UserId { get; set; } = string.Empty;

        public DateTime Date { get; set; }

        public DateTime StartTime { get; set; }

        public DateTime EndTime { get; set; }

        public string? Description { get; set; }
    }
}
