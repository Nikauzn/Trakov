using MongoDB.Driver;
using System.Collections.Generic;
using System.Threading.Tasks;
using Trakov.Backend.Logic.Tarkov;
using Trakov.Backend.Mongo;

namespace Trakov.Backend.Repositories
{
    public interface IItemsMetricsRepository
    {
        Task increaseMetric(string itemId, string metricField);
    }
    public abstract class ItemsMetricsRepositoryBase : BaseRepo, IItemsMetricsRepository
    {
        public ItemsMetricsRepositoryBase(MongoService service) : base(service)
        {
        }

        public override string collectionName { get { return "items-metrics"; } }

        public override Task<IList<T>> extractData<T>(int page, int elementsPerPage, Dictionary<string, SortDirection> sortParams = null, FilterDefinition<T> requestParams = null)
        {
            throw new System.NotImplementedException();
        }

        public Task increaseMetric(string itemId, string metricField)
        {
            var filter = Builders<ItemMetrics>.Filter.Eq(x=>x._id, itemId);
            var update = Builders<ItemMetrics>.Update.Inc(metricField, 1);
            return this.getCollection<ItemMetrics>().UpdateOneAsync(filter, update, new UpdateOptions() { IsUpsert = true });
        }
    }
    public class ItemsMetricsRepository : ItemsMetricsRepositoryBase
    {
        public ItemsMetricsRepository(MongoService service) : base(service)
        {
        }
    }
}
