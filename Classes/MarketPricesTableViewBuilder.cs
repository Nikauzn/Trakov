using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Driver;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Trakov.Backend.Logic;
using Trakov.Backend.Logic.Scanner.MarketScanner.Types;
using Trakov.Backend.Logic.Tarkov;
using Trakov.Backend.Repositories;

namespace Trakov.Backend.Classes
{
    public class MarketPricesTableViewBuilder
    {
        private readonly ItemsBaseRepo itemsRepo;

        public MarketPricesTableViewBuilder(ItemsBaseRepo itemsRepo)
        {
            this.itemsRepo = itemsRepo;
        }

        private BsonArray generatePipelineStages(FilterDefinition<PriceObject> primaryFilter,
            BsonDocument projection, IAggregateFluent<MarketPricesGroup> reflect,
            string pathToItemId)
        {
            return new BsonArray
            {
                new BsonDocument("$match", primaryFilter.RenderToBsonDocument()),
                MongoHelper.generateMatchByLet(pathToItemId, GE.PropertyName<PriceObject>(x=>x.itemId)),
                new BsonDocument("$project", projection),
                BsonSerializer.Deserialize<BsonDocument>(reflect.Stages[reflect.Stages.Count-1].ToString())
            };
        }
        private IAggregateFluent<MarketPricesGroup> getReflectForLookup(bool weekScale, IAggregateFluent<MarketPricesGroupFinal> primaryQuery)
        {
            if (weekScale)
                return primaryQuery.Project<MarketPricesTimeProjectionIntermediate>(new BsonDocument())
                .Group(k => new ItemGroup() { itemId = k.itemId, y = k.y, w = k.w },
                g => new MarketPricesGroup()
                {
                    integralMin = g.Average(x => x.minPrice),
                    integralAvg = g.Average(x => x.avgPrice),
                    integralMax = g.Average(x => x.maxPrice),
                    groupCount = g.Count(),
                    lastUpdate = g.First().createdAt
                });
            else
                return primaryQuery.Project<MarketPricesTimeProjectionIntermediate>(new BsonDocument())
                .Group(k => new ItemGroup() { itemId = k.itemId, y = k.y, d = k.d },
                g => new MarketPricesGroup()
                {
                    integralMin = g.Average(x => x.minPrice),
                    integralAvg = g.Average(x => x.avgPrice),
                    integralMax = g.Average(x => x.maxPrice),
                    groupCount = g.Count(),
                    lastUpdate = g.First().createdAt
                });
        }
        private IAggregateFluent<MarketPricesGroupFinal> getDaysGroups(IAggregateFluent<MarketPricesGroupFinal> primaryQuery,
            DateTime today)
        {
            var everyDayProjection = MongoHelper.attachTimeProjection(GE.PropertyName<PriceObject>(x => x.createdAt),
                MongoHelper.IQProjectionBuilder<PriceObject, PriceObject>(), true, dayOfYearRequired: true);

            var reflect = getReflectForLookup(false, primaryQuery);
            var yesterday = today.AddDays(-1);
            var primaryFilter = Builders<PriceObject>.Filter.Gte(x => x.createdAt, yesterday);
            var pathToItemId = GE.PropertyName<AggregatedTarkovItem>(x => x._id);

            var result = primaryQuery.AppendStage<MarketPricesGroupFinal>(
                MongoHelper.prepareLookup("market-prices", pathToItemId,
                GE.PropertyName<MarketPricesGroupFinal>(x => x.dayGroups),
                generatePipelineStages(primaryFilter, everyDayProjection, reflect, pathToItemId)));

            return result;
        }
        private IAggregateFluent<MarketPricesGroupFinal> getWeekGroups(IAggregateFluent<MarketPricesGroupFinal> primaryQuery,
            DateTime today)
        {
            var weekProjection = MongoHelper.attachTimeProjection(GE.PropertyName<PriceObject>(x => x.createdAt),
                MongoHelper.IQProjectionBuilder<PriceObject, PriceObject>(), true, weekRequired: true);

            var thisWeek = today.AddDays(-7);
            var prevWeek = thisWeek.AddDays(-7);

            var primaryFilter = Builders<PriceObject>.Filter.Gte(x => x.createdAt, prevWeek);
            var reflect = getReflectForLookup(true, primaryQuery);
            var pathToItemId = GE.PropertyName<AggregatedTarkovItem>(x => x._id);

            var result = primaryQuery.AppendStage<MarketPricesGroupFinal>(
                MongoHelper.prepareLookup("market-prices", pathToItemId,
                GE.PropertyName<MarketPricesGroupFinal>(x => x.weekGroups), 
                generatePipelineStages(primaryFilter, weekProjection, reflect, pathToItemId)));

            return result;
        }

        private FilterDefinition<MarketPricesGroupFinal> getFilter(string textQuery = "", string filters = "")
        {
            var filtersList = new List<FilterDefinition<MarketPricesGroupFinal>>();
            if (textQuery?.Length > 0)
            {
                filtersList.Add(Builders<MarketPricesGroupFinal>.Filter.Text(textQuery, new TextSearchOptions() { CaseSensitive = false }));
            }
            if (filters.Length > 0)
            {
                var parentIds = filters.Split(',');
                if (parentIds.Length > 0)
                    filtersList.Add(Builders<MarketPricesGroupFinal>.Filter.AnyIn(x=>x.parents, parentIds));
            }
            filtersList.Add(Builders<MarketPricesGroupFinal>.Filter.Eq(x => x.Type, "Item"));
            filtersList.Add(Builders<MarketPricesGroupFinal>.Filter.Gt(x => x.size, 0));
            filtersList.Add(Builders<MarketPricesGroupFinal>.Filter.Eq(x => x.placebleRagfair, true));
            return Builders<MarketPricesGroupFinal>.Filter.And(filtersList);
        }
        private Task<List<MarketPricesTableProjection>> execute(FilterDefinition<MarketPricesGroupFinal> filter,
            int page, int itemsPerPage, string requiredLocale = "en", DateTime? overrider = null)
        {
            var today = (overrider == null) ? DateTime.UtcNow : overrider.Value;

            var primaryQuery = itemsRepo.getCollection<MarketPricesGroupFinal>().Aggregate()
                .Match(filter).Skip(page * itemsPerPage).Limit(itemsPerPage);
            var withWeeks = getWeekGroups(primaryQuery, today);
            var withDay = getDaysGroups(withWeeks, today);
            var finalProjection = withDay.Project<MarketPricesTableProjection>(MarketPricesTableProjection.getProjectionWithLocale(requiredLocale));
            return finalProjection.ToListAsync();
        }

        public Task<List<MarketPricesTableProjection>> buildView(int page = 0, int itemsPerPage = 10, 
            string query = "", DateTime? overrider = null, string requiredLocale = "en",
            string filters = "")
        {
            var filter = getFilter(query, filters);
            return execute(filter, page, itemsPerPage, requiredLocale, overrider);
        }
    }
}
