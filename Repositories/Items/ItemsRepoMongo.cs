using MongoDB.Bson;
using MongoDB.Driver;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Trakov.Backend.Logic.Scanner.MarketScanner.Types;
using Trakov.Backend.Logic.Tarkov;
using Trakov.Backend.Mongo;

namespace Trakov.Backend.Repositories
{
    public class ItemsRepoMongo : ItemsBaseRepo
    {

        public ItemsRepoMongo(MongoService service) : base(service)
        {
        }

        public override async Task<IEnumerable<string>> getItemsForGlobalScanning()
        {
            var collection = this.service.getMainDatabase.GetCollection<AggregatedTarkovItem>(collectionName);
            var filter = Builders<AggregatedTarkovItem>.Filter.And(
                Builders<AggregatedTarkovItem>.Filter.Eq(x=>x.Type, "Item"),
                Builders<AggregatedTarkovItem>.Filter.Eq(x=>x.placebleRagfair, true),
                Builders<AggregatedTarkovItem>.Filter.Eq(x=>x.fixedPrice, false),
                Builders<AggregatedTarkovItem>.Filter.Eq(x => x.questItem, false),
                Builders<AggregatedTarkovItem>.Filter.Gt(x=>x.size, 0)
                );
            
            var aggregation = this.prepareRequest(requestParams: filter);
            var externalCollection = this.service.getMainDatabase.GetCollection<PriceObject>("market-prices");
            var let = new BsonDocument(new BsonElement(GE.PropertyName<PriceObject>(x=>x.itemId), GE.PropertyName<Entity>(x=>x._id)));

            var replacement = new BsonDocument(
                new BsonElement("$mergeObjects",
                    new BsonArray(new object[] { new BsonDocument(), "$relatedPrices" } )));

            var projection = Builders<Entity>.Projection
                .Exclude(GE.PropertyName<Entity>(x => x._id))
                .Include(GE.PropertyName<PriceObject>(x => x.itemId));

            var arr = new BsonArray
            {
                MongoHelper.generateMatchByLet(GE.PropertyName<AggregatedTarkovItem>(x => x._id),
                GE.PropertyName<PriceObject>(x => x.itemId)),
                new BsonDocument("$sort", new BsonDocument(GE.PropertyName<PriceObject>(x => x.createdAt), -1)),
                new BsonDocument("$limit", 1)
            };

            var idAggr = this.getCollection<AggregatedTarkovItem>().Aggregate()
                .Match(filter)
                .AppendStage<ItemWithPrices>(GE.prepareLookup("market-prices", "_id", "relatedPrices", arr))
                .Unwind(x=>x.relatedPrices, new AggregateUnwindOptions<ItemWithPrices>() { PreserveNullAndEmptyArrays = true });
            var res = await idAggr.ToListAsync();
            return res
                .OrderBy(x=> x.relatedPrices == null)
                .ThenBy(x=>x?.relatedPrices?.createdAt)
                .Select(x=>x._id);
        }
    }
}
