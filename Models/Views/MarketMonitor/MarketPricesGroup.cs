using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using System;
using Trakov.Backend.Logic.Scanner.MarketScanner.Types;
using Trakov.Backend.Logic.Tarkov;
using Trakov.Backend.Repositories;

namespace Trakov.Backend.Logic
{
    public class MarketPricesGroup
    {
        public ItemGroup _id { get; set; }
        public string en { get; set; }
        public string ru { get; set; }
        public int groupCount { get; set; }
        public double integralMax { get; set; }
        public double integralAvg { get; set; }
        public double integralMin { get; set; }
        public DateTime lastUpdate { get; set; }
    }
    [BsonIgnoreExtraElements]
    public class MarketPricesGroupFinal : AggregatedTarkovItemSystemData
    {
        public MarketPricesGroup[] weekGroups { get; set; }
        public MarketPricesGroup[] dayGroups { get; set; }
    }
    public class MarketPricesTableProjection : IEntity
    {
        public string _id { get; set; }
        public string name { get; set; }
        public MarketPricesGroup[] weekGroups { get; set; }
        public MarketPricesGroup[] dayGroups { get; set; }

        public static BsonDocument getProjectionWithLocale(string requiredLocale = "en")
        {
            var project = MongoHelper.IQProjectionBuilder<MarketPricesGroupFinal, MarketPricesTableProjection>();
            project[GE.PropertyName<MarketPricesTableProjection>(x => x.name)] = $"${requiredLocale}";
            return project;
        }
    }
    public class MarketPricesTimeProjectionIntermediate : PriceObject, ITimeGroup
    {
        [BsonIgnoreIfNull]
        public int? y { get; set; }
        [BsonIgnoreIfNull]
        public int? m { get; set; }
        [BsonIgnoreIfNull]
        public int? d { get; set; }
        [BsonIgnoreIfNull]
        public int? w { get; set; }
        [BsonIgnoreIfNull]
        public int? h { get; set; }
    }
}
