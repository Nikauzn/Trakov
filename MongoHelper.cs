using System;
using System.Collections.Generic;
using System.Linq;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Driver;
using Trakov.Backend.Logic;
using Trakov.Backend.Repositories;

namespace Trakov.Backend
{
    public static class MongoHelper
    {
        public static string letFilter(string let)
        {
            let = let.Replace("_", string.Empty);
            return (let.Contains('.')) ? let.Remove(0, let.IndexOf('.') + 1) : let;
        }
        public static BsonDocument prepareLookup(string collection, string localField, string asField, BsonArray subpipeline)
        {
            var let = letFilter(localField);
            return new BsonDocument("$lookup",
                new BsonDocument("from", collection)
                    .Add("let", new BsonDocument($"{let}", $"${localField}"))
                    .Add("pipeline", subpipeline)
                    .Add("as", asField)
            );
        }
        public static BsonDocument prepareLookup(string collection, BsonDocument lets, string asField, BsonArray subpipeline)
        {
            return new BsonDocument("$lookup",
                new BsonDocument("from", collection)
                    .Add("let", lets)
                    .Add("pipeline", subpipeline)
                    .Add("as", asField)
            );
        }
        public static BsonDocument generateMatchByLet(string let, string foreignField, string matcher = "$eq")
        {
            let = letFilter(let).Replace("_", string.Empty);
            return new BsonDocument("$match", new BsonDocument("$expr",
                new BsonDocument(new BsonElement(matcher, new BsonArray(new string[] { $"$${let}", $"${foreignField}" })))));
        }
        public static BsonDocument IQProjectionBuilder<Source, Output>()
        {
            var sourceProps = typeof(Source).GetProperties().Select(x => x.Name).ToArray();
            var outputProps = typeof(Output).GetProperties().Select(x => x.Name).ToArray();
            var projection = new BsonDocument();
            foreach (var prop in sourceProps)
            {
                if (outputProps.Contains(prop))
                    projection.Add(new BsonElement(prop, 1));
                else if (outputProps.Contains(prop) == false && prop == GE.PropertyName<Entity>(x => x._id))
                    projection.Add(new BsonElement(prop, 0));
            }
            return projection;
        }
        public static BsonDocument replaceCurrentDocumentToRoot(string targetField)
        {
            return new BsonDocument(new BsonElement("$mergeObjects",
                new BsonArray(new object[] { "{}", $"${targetField}" })));
        }
        public static UpdateDefinition<T> updateCertainFields<T>(T updateDocumet)
        {
            var updates = new List<UpdateDefinition<T>>();
            foreach(var property in typeof(T).GetProperties())
            {
                var field = property.Name;
                var value = property.GetValue(updateDocumet);
                updates.Add(Builders<T>.Update.Set(field, value));
            }
            return Builders<T>.Update.Combine(updates);
        }
        public static BsonDocument RenderToBsonDocument<T>(this FilterDefinition<T> filter)
        {
            var serializerRegistry = BsonSerializer.SerializerRegistry;
            var documentSerializer = serializerRegistry.GetSerializer<T>();
            return filter.Render(documentSerializer, serializerRegistry);
        }
        public static BsonDocument RenderToBsonDocument<T>(this SortDefinition<T> filter)
        {
            var serializerRegistry = BsonSerializer.SerializerRegistry;
            var documentSerializer = serializerRegistry.GetSerializer<T>();
            return filter.Render(documentSerializer, serializerRegistry);
        }
        public static UpdateDefinition<T> mergeUpdate<T>(this T obj, bool ignoreNulls)
        {
            var updates = new List<UpdateDefinition<T>>();
            foreach(var property in typeof(T).GetProperties())
            {
                var propValue = property.GetValue(obj);
                if (ignoreNulls == true && propValue == null)
                {
                    updates.Add(Builders<T>.Update.Unset(property.Name));
                    continue;
                }
                updates.Add(Builders<T>.Update.Set(property.Name, propValue));
            }
            return Builders<T>.Update.Combine(updates);
        }
        public static BsonDocument attachTimeProjection(string dateTimeFieldName, 
            BsonDocument originalProjection = null,
            bool yearRequired = false, bool monthRequired = false,
            bool dayOfMonthRequired = false, bool dayOfYearRequired = false,
            bool weekRequired = false, bool hourRequired = false)
        {
            var elements = new List<BsonElement>();
            if (yearRequired) 
                elements.Add(new BsonElement(GE.PropertyName<TimeGroup>(x=>x.y), new BsonDocument("$year", $"${dateTimeFieldName}")));
            if (monthRequired)
                elements.Add(new BsonElement(GE.PropertyName<TimeGroup>(x => x.m), new BsonDocument("$month", $"${dateTimeFieldName}")));
            if (weekRequired)
                elements.Add(new BsonElement(GE.PropertyName<TimeGroup>(x => x.w), new BsonDocument("$week", $"${dateTimeFieldName}")));
            if (dayOfMonthRequired)
                elements.Add(new BsonElement(GE.PropertyName<TimeGroup>(x => x.d), new BsonDocument("$dayOfMonth", $"${dateTimeFieldName}")));
            if (dayOfYearRequired)
                elements.Add(new BsonElement(GE.PropertyName<TimeGroup>(x => x.d), new BsonDocument("$dayOfYear", $"${dateTimeFieldName}")));
            if (hourRequired)
                elements.Add(new BsonElement(GE.PropertyName<TimeGroup>(x => x.h), new BsonDocument("$hour", $"${dateTimeFieldName}")));
            if (originalProjection == null)
                originalProjection = new BsonDocument();
            originalProjection.AddRange(elements);
            return originalProjection;
        }
    }
}
