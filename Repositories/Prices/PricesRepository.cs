using MongoDB.Bson;
using MongoDB.Driver;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Trakov.Backend.Logic.Scanner.MarketScanner.Types;
using Trakov.Backend.Mongo;
using Trakov.Backend.Repositories;

namespace Trakov.Backend.Repositories
{
    internal interface IPricesRepository
    {
        Task<List<Entity>> disctinctAndSortNotFresh();
    }
    public abstract class PricesBaseRepo : BaseRepo, IPricesRepository
    {
        protected PricesBaseRepo(MongoService service) : base(service)
        {
            ensureIndexes();
        }

        private void ensureIndexes()
        {
            var createRequestList = new List<CreateIndexModel<PriceObject>>
            {
                new CreateIndexModel<PriceObject>(
                new IndexKeysDefinitionBuilder<PriceObject>()
                .Descending(x => x.expireAt),
                new CreateIndexOptions() { Unique = false, ExpireAfter = new TimeSpan(1000, 0, 0, 0, 0) })
            };

            this.getCollection<PriceObject>().Indexes.CreateManyAsync(createRequestList);
        }

        public Task<List<Entity>> disctinctAndSortNotFresh()
        {
            var filter = new BsonDocument();
            return this.getCollection<PriceObject>().Aggregate()
                .SortByDescending(x=>x.createdAt)
                .Group<Entity>(new BsonDocument(new BsonElement("$_id", GE.PropertyName<Entity>(x=>x._id))))
                .ToListAsync();
                
        }
    }
    public class PricesRepository : PricesBaseRepo
    {
        public override string collectionName { get { return "market-prices"; } }

        public PricesRepository(MongoService service) : base(service)
        {
        }

        public override Task<IList<T>> extractData<T>(int page, int elementsPerPage, 
            Dictionary<string, SortDirection> sortParams = null, FilterDefinition<T> requestParams = null)
        {
            throw new NotImplementedException();
        }
    }
}
