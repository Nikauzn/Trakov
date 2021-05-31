using MongoDB.Driver;
using System.Collections.Generic;
using System.Threading.Tasks;
using Trakov.Backend.Classes;
using Trakov.Backend.Logic.Tarkov;
using Trakov.Backend.Mongo;
using Trakov.Backend.Repositories.Recipes;

namespace Trakov.Backend.Repositories
{
    internal interface IItemRepositoryMongo
    {
        Task<IEnumerable<string>> getItemsForGlobalScanning();
        Task updateLocale(string locale, Dictionary<string, string> updates);
        Task<List<LocalizableWithId>> GetItemsGroups(string requiredLocale, string parentId);
    }
    public abstract class ItemsBaseRepo : BaseRepo, IItemRepositoryMongo
    {
        public override string collectionName { get { return "items"; } }

        public ItemsBaseRepo(MongoService service) : base(service)
        {
            ensureIndexes();
        }

        private void ensureIndexes()
        {
            var createRequestList = new List<CreateIndexModel<AggregatedTarkovItem>>
            {
                new CreateIndexModel<AggregatedTarkovItem>(
                new IndexKeysDefinitionBuilder<AggregatedTarkovItem>()
                .Text("en").Text("ru"),
                new CreateIndexOptions() { Unique = false }),
                new CreateIndexModel<AggregatedTarkovItem>(
                new IndexKeysDefinitionBuilder<AggregatedTarkovItem>()
                .Ascending(x => x.Type),
                new CreateIndexOptions() { Unique = false })
            };

            this.getCollection<AggregatedTarkovItem>().Indexes.CreateManyAsync(createRequestList);
        }

        public Task updateLocale(string locale, Dictionary<string, string> updates)
        {
            var bulkOps = new List<WriteModel<Entity>>();
            foreach(var update in updates.Keys)
            {
                updates.TryGetValue(update, out var localeValue);
                var updateOne = new UpdateOneModel<Entity>(
                    Builders<Entity>.Filter.Eq(x => x._id, update),
                    Builders<Entity>.Update.Set(locale, localeValue));
                bulkOps.Add(updateOne);
            }
            return this.getCollection<Entity>().BulkWriteAsync(bulkOps);
        }

        public override async Task<IList<T>> extractData<T>(int page, int elementsPerPage, 
            Dictionary<string, SortDirection> sortParams = null, FilterDefinition<T> requestParams = null)
        {
            var aggregate = this.prepareRequest<T>(page, elementsPerPage, sortParams, requestParams);
            return await aggregate.ToListAsync();
        }

        public abstract Task<IEnumerable<string>> getItemsForGlobalScanning();

        public Task<List<LocalizableWithId>> GetItemsGroups(string requiredLocale, string parentId = Constant.topNodeId)
        {
            var filter = Builders<AggregatedTarkovItemSystemData>.Filter.Eq(x => x.isPrimaryFilter, true);
            var projection = Builders<AggregatedTarkovItemSystemData>.Projection.Include(x => x._id).Include(requiredLocale);
            return getCollection<AggregatedTarkovItemSystemData>().Aggregate().Match(filter).Project<LocalizableWithId>(projection).ToListAsync();
        }
    }
}
