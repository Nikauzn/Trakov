using MongoDB.Bson.Serialization.Attributes;

namespace Trakov.Backend.Logic
{
    public interface ITimeGroup
    {
        [BsonIgnoreIfNull]
        public int? y { get; set; }
        [BsonIgnoreIfNull]
        public int? m { get; set; }
        [BsonIgnoreIfNull]
        public int? d { get; set; }
        [BsonIgnoreIfNull]
        public int? w { get; set; }
        [BsonIgnoreIfNull]
        public int? h { get; set; }
    }
    public class TimeGroup
    {
        [BsonIgnoreIfNull]
        public int? y { get; set; }
        [BsonIgnoreIfNull]
        public int? m { get; set; }
        [BsonIgnoreIfNull]
        public int? d { get; set; }
        [BsonIgnoreIfNull]
        public int? w { get; set; }
        [BsonIgnoreIfNull]
        public int? h { get; set; }
    }
}
