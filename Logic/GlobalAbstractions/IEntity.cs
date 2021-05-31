using Google.Cloud.Firestore;
using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Bson.Serialization.IdGenerators;
using Newtonsoft.Json;

namespace Trakov.Backend.Repositories
{
    public interface IEntity
    {
        [FirestoreProperty(Name = "Id")]
        [JsonProperty("_id")]
        [BsonId(IdGenerator = typeof(StringObjectIdGenerator))]
        public string _id { get; set; }
    }
    public class Entity
    {
        [FirestoreProperty(Name = "Id")]
        [JsonProperty("_id")]
        [BsonId(IdGenerator = typeof(StringObjectIdGenerator))]
        public string _id { get; set; }
    }
}
