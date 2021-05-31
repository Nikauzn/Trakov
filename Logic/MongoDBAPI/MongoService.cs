using MongoDB.Bson.Serialization;
using MongoDB.Driver;
using System;
using System.Linq;
using Trakov.Backend.Logic.Tarkov;
using Trakov.Backend.Repositories.Recipes;

namespace Trakov.Backend.Mongo
{
    public class MongoService
    {
        public MongoClient client { get; private set; }
        public IMongoDatabase getMainDatabase { get { return this.client.GetDatabase("trakov-main"); } }

        public MongoService(string connectionStr)
        {
            this.client = new MongoClient(connectionStr);
        }
    }
}
