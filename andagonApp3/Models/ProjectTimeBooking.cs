using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace andagonApp3.Models
{
    public class ProjectTimeBooking
    {
        [BsonId]
        [BsonRepresentation(BsonType.ObjectId)]
        public string? Id { get; set; }

        public string UserId { get; set; } = string.Empty;

        public long ProjectId { get; set; }

        public string? ProjectName { get; set; }

        public DateTime Date { get; set; }

        public double Hours { get; set; }

        public string? Description { get; set; }
    }
}
