using MongoDB.Bson;
using MongoDB.Driver;
using System.Collections.Generic;
using System.Threading.Tasks;
using Trakov.Backend.Repositories;
using Trakov.Backend.Repositories.Recipes;

namespace Trakov.Backend.Mongo
{
    public interface IDefaultMongoRequests
    {
        Task upsertSingle<T>(string tarkovID, T obj) where T: IEntity;
        Task upsertBatch<T>(Dictionary<string, T> values) where T : IEntity;
        IAggregateFluent<T> prepareRequest<T>(int page, int elementsPerPage = 0,
            Dictionary<string, SortDirection> sortParams = null,
            FilterDefinition<T> requestParams = null);
        Task<IList<T>> extractData<T>(int page, int elementsPerPage,
            Dictionary<string, SortDirection> sortParams = null,
            FilterDefinition<T> requestParams = null);
        Task insertSingle<T>(T obj);
        Task upsertSingle<T>(ObjectId id, T obj);
    }
    public abstract class BaseRepo : IDefaultMongoRequests
    {
        protected readonly MongoService service;
        public abstract string collectionName { get; }

        public BaseRepo(MongoService service)
        {
            this.service = service;
        }

        public IMongoCollection<T> getCollection<T>()
        {
            return this.service.getMainDatabase.GetCollection<T>(this.collectionName);
        }

        public abstract Task<IList<T>> extractData<T>(int page, int elementsPerPage, 
            Dictionary<string, SortDirection> sortParams = null,
            FilterDefinition<T> requestParams = null);

        public Task upsertBatch<T>(Dictionary<string, T> items) where T: IEntity
        {
            var bulkOps = new List<WriteModel<T>>();
            var filterFieldName = GE.PropertyName<T>(y => y._id);
            foreach (var key in items.Keys)
            {
                items.TryGetValue(key, out T record);
                var upsertOne = new UpdateOneModel<T>(
                    Builders<T>.Filter.Eq(filterFieldName, key), record.mergeUpdate(true))
                { IsUpsert = true };
                bulkOps.Add(upsertOne);
            }
            var collection = this.service.getMainDatabase.GetCollection<T>(collectionName);
            return collection.BulkWriteAsync(bulkOps, new BulkWriteOptions() { BypassDocumentValidation = false });
        }

        public Task upsertSingle<T>(string tarkovID, T obj) where T : IEntity
        {
            var filter = Builders<T>.Filter.Eq(x => x._id, tarkovID);
            var update = obj.mergeUpdate(true);

            var collection = this.service.getMainDatabase.GetCollection<T>(collectionName);
            return collection.UpdateOneAsync(filter, update, new UpdateOptions() { BypassDocumentValidation = false });
        }

        public Task insertSingle<T>(T obj)
        {
            return this.getCollection<T>().InsertOneAsync(obj);
        }

        public IAggregateFluent<T> prepareRequest<T>(int page = 0, int elementsPerPage = 0,
            Dictionary<string, SortDirection> sortParams = null,
            FilterDefinition<T> requestParams = null)
        {
            var collection = this.service.getMainDatabase.GetCollection<T>(collectionName);
            var agregQuery = collection.Aggregate<T>();
            if (sortParams != null) agregQuery = agregQuery.Sort(sortParams.DictionaryToSortFilter<T>());
            if (requestParams != null) agregQuery = agregQuery.Match(requestParams);
            if (page > 0 && elementsPerPage > 0)
                agregQuery = agregQuery.Skip(page * elementsPerPage);
            if (elementsPerPage > 0)
                agregQuery = agregQuery.Limit(elementsPerPage);
            return agregQuery;
        }

        public Task upsertSingle<T>(ObjectId id, T obj)
        {
            var upsertOne = new ReplaceOneModel<T>(
                Builders<T>.Filter.Eq("_id", id), obj)
                { IsUpsert = true };
            return this.getCollection<T>().BulkWriteAsync(new WriteModel<T>[] { upsertOne });
        }
    }
}
