using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Bson.Serialization.IdGenerators;
using System;
using Trakov.Backend.Repositories;

namespace Trakov.Backend.Logic.PatreonAPI
{
    public class LogMessage : IEntity
    {
        [BsonId(IdGenerator = typeof(StringObjectIdGenerator))]
        public string _id { get; set; }
        public string message { get; set; }
        [BsonElement("expiresAt")]
        public DateTime expiresAt { get; set; }
        public DateTime createdAt { get; set; } = DateTime.UtcNow;
    }
}
