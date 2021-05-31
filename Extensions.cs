using System;
using System.Linq;
using MongoDB.Bson;
using MongoDB.Driver;
using System.Reflection;
using System.Collections;
using System.Linq.Expressions;
using Microsoft.AspNetCore.Mvc;
using System.Collections.Generic;
using Trakov.Backend.Repositories.Recipes;

namespace Trakov.Backend
{
    public static class GE
    {
        private static string letFilter(string let)
        {
            return (let.Contains('.')) ? let.Remove(0, let.IndexOf('.') + 1) : let;
        }
        public static BsonDocument prepareLookup(string collection, string localField, string asField, BsonArray subpipeline)
        {
            var let = letFilter(localField).Replace("_", string.Empty);
            return new BsonDocument("$lookup",
                new BsonDocument("from", collection)
                    .Add("let", new BsonDocument($"{let}", $"${localField}"))
                    .Add("pipeline", subpipeline)
                    .Add("as", asField)
            );
        }
        public static string RandomString(int length)
        {
            var random = new Random();
            const string chars = "zxcvbnmasdfghjklqwertyuiopABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
            return new string(Enumerable.Repeat(chars, length)
              .Select(s => s[random.Next(s.Length)]).ToArray());
        }
        static public Dictionary<string, object> getUpdateDictionary<T>(this T obj)
        {
            Dictionary<string, object> result = new Dictionary<string, object>();
            foreach(var property in obj.GetType().GetProperties())
            {
                foreach(var attribute in property.CustomAttributes)
                    if (attribute.AttributeType.FullName.ToLower().Contains("firestoreproperty"))
                        result.Add(property.Name, property.GetValue(obj));
            }
            return result;
        }
        public static string PropertyName<T>(this Expression<Func<T, object>> propertyExpression)
        {
            MemberExpression mbody = propertyExpression.Body as MemberExpression;

            if (mbody == null)
            {
                //This will handle Nullable<T> properties.
                UnaryExpression ubody = propertyExpression.Body as UnaryExpression;

                if (ubody != null)
                {
                    mbody = ubody.Operand as MemberExpression;
                }

                if (mbody == null)
                {
                    throw new ArgumentException("Expression is not a MemberExpression", "propertyExpression");
                }
            }

            return mbody.Member.Name;
        }
        public static BsonDocument buildAntiunwindRequest<T>()
        {
            var elements = new BsonDocument();
            var typeProperties = typeof(T).GetProperties(BindingFlags.Instance | BindingFlags.Public);
            foreach(var prop in typeProperties)
            {
                if (prop.CustomAttributes.Where(x=>x.AttributeType.FullName.ToLower().Contains("bsonid")).Count() > 0) 
                {
                    elements.Add(new BsonElement("_id", $"${prop.Name}"));
                }
                else
                {
                    BsonDocument instructions = new BsonDocument();
                    if (typeof(IEnumerable).IsAssignableFrom(prop.PropertyType)
                        && typeof(string).IsAssignableFrom(prop.PropertyType)==false)
                        instructions.Add(new BsonElement("$push", $"${prop.Name}"));
                    else
                        instructions.Add(new BsonElement("$first", $"${prop.Name}"));
                    elements.Add(prop.Name, instructions);
                }
            }
            return new BsonDocument("$group", elements);
        }
        public static UpdateDefinition<T> AddValuesToUpdate<T>(this T obj)
        {
            UpdateDefinitionBuilder<T> builder = Builders<T>.Update;
            var update = new List<UpdateDefinition<T>>();
            foreach(var fields in obj.GetType().GetProperties())
            {
                foreach(var attr in fields.CustomAttributes)
                {
                    if (attr.AttributeType.FullName.ToLower().Contains("bsonelement"))
                    {
                        var value = fields.GetValue(obj);
                        if (value != null)
                            update.Add(builder.Set(fields.Name, value));
                    }
                }
            }
            return builder.Combine(update); 
        }
        public static SortDefinition<T> DictionaryToSortFilter<T>(
            this Dictionary<string, SortDirection> instructions)
        {
            var builder = Builders<T>.Sort;
            var filters = new List<SortDefinition<T>>();
            foreach(var keyField in instructions.Keys)
            {
                instructions.TryGetValue(keyField, out var direction);
                FieldDefinition<T> field = keyField;
                switch (direction)
                {
                    case SortDirection.Ascending:
                        filters.Add(builder.Ascending(field));
                        continue;
                    case SortDirection.Descending:
                        filters.Add(builder.Descending(field));
                        continue;
                    default:
                        throw new NotImplementedException($"Sort type {nameof(direction)} is not implemented");
                }
            }
            return builder.Combine(filters);
        }
        public static FilterDefinition<T> EnumerableToFilter<T>(this IEnumerable<FilterRequest> request)
        {
            var builder = Builders<T>.Filter;
            FilterDefinition<T> filter = null;
            foreach(var req in request)
            {
                if (filter != null)
                {
                    switch (req.rule)
                    {
                        case FilterRequest.RulesType.and:
                            filter &= req.applyFilterRule<T>(builder);
                            continue;
                        case FilterRequest.RulesType.or:
                            filter |= req.applyFilterRule<T>(builder);
                            continue;
                        default:
                            throw new NotImplementedException($"{nameof(req.rule)} is not implemented");
                    }
                }
                else filter = req.applyFilterRule(builder);
            }
            return filter;
        }
        private static FilterDefinition<T> applyFilterRule<T>(this FilterRequest request, 
            FilterDefinitionBuilder<T> builder)
        {
            switch (request.filterType)
            {
                case FilterRequest.FilterType.Equivalent:
                    return builder.Eq(request.fieldToFilter, request.request);
                case FilterRequest.FilterType.In:
                    return builder.In(request.fieldToFilter, (IEnumerable<object>)request.request);
                case FilterRequest.FilterType.GreaterThan:
                    return builder.Gt(request.fieldToFilter, request.request);
                default:
                    throw new NotImplementedException($"{nameof(request.filterType)} is not implemented");
            }
        }
        public static bool ifRequestAuthenticated(this ControllerBase controller)
        {
            return (controller.HttpContext.User != null) && controller.HttpContext.User.Identity.IsAuthenticated;
        }

        public static int? stringToInt(this string obj)
        {
            bool success = int.TryParse(obj, out int result);
            if (success)
                return result; 
            else
                return null;
        }
        public static int deltarizer(this int original, double minDelta)
        {
            var randomizer = new Random();
            double coeff = -1;
            do
            {
                coeff = randomizer.NextDouble();
            } 
            while (coeff < minDelta);
            return (int)(original * coeff);
        }
    }
}
