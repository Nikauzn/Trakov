using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Bson.Serialization.IdGenerators;
using MongoDB.Driver;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Trakov.Backend.Logic.PatreonAPI;
using Trakov.Backend.Mongo;

namespace Trakov.Backend.Repositories
{
    public abstract class LogRepositoryBase : BaseRepo
    {
        protected LogRepositoryBase(MongoService service) : base(service)
        {
        }

        public static void writeMessage(string message)
        {
            var logrepo = Startup.Resolve(typeof(LogRepositoryBase)) as LogRepositoryBase;
            logrepo.insertSingle(new LogMessage() { message = message });
        }
    }
    public class LogRepository : LogRepositoryBase
    {
        public LogRepository(MongoService service) : base(service)
        {
            this.ensureIndexes();
        }

        private void ensureIndexes()
        {
            var createRequestList = new List<CreateIndexModel<LogMessage>>
            {
                new CreateIndexModel<LogMessage>(
                new IndexKeysDefinitionBuilder<LogMessage>()
                .Descending(x => x.expiresAt),
                new CreateIndexOptions() { Unique = false, ExpireAfter = new TimeSpan(15, 0, 0, 0, 0) }),
                new CreateIndexModel<LogMessage>(
                new IndexKeysDefinitionBuilder<LogMessage>()
                .Descending(x => x.createdAt),
                new CreateIndexOptions() { Unique = false })
            };

            this.getCollection<LogMessage>().Indexes.CreateManyAsync(createRequestList);
        }

        public override string collectionName { get { return "logs"; } }

        public override Task<IList<T>> extractData<T>(int page, int elementsPerPage, Dictionary<string, SortDirection> sortParams = null, FilterDefinition<T> requestParams = null)
        {
            throw new NotImplementedException();
        }
    }
}
