using MongoDB.Bson;
using MongoDB.Driver;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Trakov.Backend.Logic.Scanner.MarketScanner.Types;
using Trakov.Backend.Logic.Tarkov;
using Trakov.Backend.Mongo;
using Trakov.Backend.Repositories;

namespace Trakov.Backend.Repositories
{
    public interface IRecipesRepository : IDefaultMongoRequests
    {
        Task<IEnumerable<string>> getRecipeItems();
        Task<List<RecipesDataView>> buildView(int page = 0, int itemsPerPage = 5,
                    Dictionary<string, SortDirection> sortParams = null,
                    FilterDefinition<RecipesData> requestParams = null);
    }
    public class RecipesRepositoryMongo : RecipesRepositoryMongoBase
    {
        public RecipesRepositoryMongo(MongoService mongoService)
            : base(mongoService)
        {
        }

        public override async Task<IList<T>> extractData<T>(int page, int elementsPerPage, 
            Dictionary<string, SortDirection> sortParams = null,
            FilterDefinition<T> requestParams = null)
        {
            var agregQuery = this.prepareRequest<T>(page, elementsPerPage, sortParams, requestParams);
            return await agregQuery.ToListAsync();
        }

        public override async Task<IEnumerable<string>> getRecipeItems()
        {
            var agregQuery = this.prepareRequest<RecipesData>();
            var projectQuery = await agregQuery.Project(this.getDefaultProjection()).ToListAsync();
            List<string> results = new List<string>();
            foreach(var res in projectQuery)
            {
                results.AddRange(res.requirementsIds);
                results.Add(res.outputId);
            }
            return results.Distinct();
        }

        public override Task<List<RecipesDataView>> buildView(int page = 0, int itemsPerPage = 5, 
            Dictionary<string, SortDirection> sortParams = null, FilterDefinition<RecipesData> requestParams = null)
        {
            IAggregateFluent<RecipesData> query = this.prepareRequest<RecipesData>(page, itemsPerPage, sortParams, requestParams);
            var aQuery = new RecipeViewBuilder().buildView(query);
            if (sortParams != null) aQuery = aQuery.Sort(sortParams.DictionaryToSortFilter<RecipesDataView>());
            return aQuery.ToListAsync();
        }
    }
    public class RecipeViewBuilder
    {
        private string letFilter(string let)
        {
            return (let.Contains('.')) ? let.Remove(0, let.IndexOf('.')+1) : let;
        }
        private IEnumerable<BsonElement> getPriceProjectionElements()
        {
            return new BsonElement[]
            {
                new BsonElement(GE.PropertyName<PriceObjectRecipeProjection>(x=>x.avgPrice), 1),
                new BsonElement(GE.PropertyName<PriceObjectRecipeProjection>(x=>x.createdAt), 1),
                new BsonElement(GE.PropertyName<PriceObject>(x=>x._id), 0)
            };
        }
        private IEnumerable<BsonElement> getBasePriceProjection()
        {
            return new BsonElement[]
            {
                new BsonElement(GE.PropertyName<ItemsData>(x=>x.creditsPrice), 1),
                new BsonElement(GE.PropertyName<Entity>(x=>x._id), 0)
            };
        }


        private BsonArray getLookupPipelineForItemsMarket(string let, string foreignField)
        {
            BsonArray subpipeline = new BsonArray();
            let = this.letFilter(let);
            subpipeline.Add(
              new BsonDocument("$match", new BsonDocument(
                "$expr", new BsonDocument("$eq", new BsonArray { $"${foreignField}", $"$${let}" })
            )));
            subpipeline.Add(
                new BsonDocument("$sort", new BsonDocument(
                    GE.PropertyName<PriceObject>(x => x.createdAt), -1)));
            subpipeline.Add(new BsonDocument("$limit", 1));

            subpipeline.Add(new BsonDocument("$project", new BsonDocument(this.getPriceProjectionElements())));

            return subpipeline;
        }
        private BsonArray getLookupForItemsBase(string let, string foreignField)
        {
            BsonArray subpipeline = new BsonArray();
            let = this.letFilter(let);
            subpipeline.Add(
              new BsonDocument("$match", new BsonDocument(
                "$expr", new BsonDocument("$eq", new BsonArray { $"${foreignField}", $"$${let}" })
            )));
            subpipeline.Add(new BsonDocument("$project", new BsonDocument(this.getBasePriceProjection())));

            return subpipeline;
        }

        private IAggregateFluent<RecipesDataView> executeLookups(IAggregateFluent<RecipesData> initedRequest)
        {
            var marketCollection = "market-prices"; var itemsCollection = "items";
            var endProductIdFieldName = GE.PropertyName<RecipesDataView>(x => x.endProduct);
            var endProductObjectFieldName = GE.PropertyName<RecipesDataView>(x=>x.endProdObj);
            var requitementsFieldName = GE.PropertyName<RecipesDataView>(x => x.requirements);
            var itemIdFieldName = GE.PropertyName<PriceObject>(x=>x.itemId);
            var idFieldName = GE.PropertyName<Entity>(x => x._id);

            var requirementItemPrice = GE.PropertyName<RecipesDataLookupedIntemediate>(x => x.requirements.price);
            var requirementItemIdFiledName = GE.PropertyName<RecipesDataLookupedIntemediate>(x => x.requirements.templateId);
            var requirementBaseItemField = GE.PropertyName<RecipesDataLookupedIntemediate>(x=>x.requirements.baseItem);

            BsonArray subpipelineForEndProduct = this.getLookupPipelineForItemsMarket(endProductIdFieldName, itemIdFieldName);
            BsonArray subpipeForRequirements = this.getLookupPipelineForItemsMarket($"{requitementsFieldName}.{requirementItemIdFiledName}",
                itemIdFieldName);
            BsonArray subpipeForRequirementBasePrice = this.getLookupForItemsBase($"{requitementsFieldName}.{requirementItemIdFiledName}",
                idFieldName);
            BsonArray subpipeForEndProductBasePrice = this.getLookupForItemsBase(endProductIdFieldName, idFieldName);

            var lookupForEndProduct = GE.prepareLookup(marketCollection, endProductIdFieldName, 
                GE.PropertyName<RecipesDataView>(x => x.endProdPrice),
                subpipelineForEndProduct);

            var lookupForRequirements = GE.prepareLookup(marketCollection,
                $"{requitementsFieldName}.{requirementItemIdFiledName}",
                $"{requitementsFieldName}.{requirementItemPrice}", subpipeForRequirements);
            var lookupForRequirementsBasePrice = GE.prepareLookup(itemsCollection,
                $"{requitementsFieldName}.{requirementItemIdFiledName}",
                $"{requitementsFieldName}.{requirementBaseItemField}",
                subpipeForRequirementBasePrice);


            var lookupForEndProductBasePrice = GE.prepareLookup(itemsCollection,
                endProductIdFieldName, endProductObjectFieldName, subpipeForEndProductBasePrice);
            var defaultUnwindOption = new AggregateUnwindOptions<RecipesDataView>() { PreserveNullAndEmptyArrays = true };

            return initedRequest
                .Unwind(GE.PropertyName<RecipesData>(x => x.requirements))
                .AppendStage<RecipesDataView>(lookupForEndProduct)
                .AppendStage<RecipesDataView>(lookupForRequirements)
                .AppendStage<RecipesDataView>(lookupForRequirementsBasePrice)
                .AppendStage<RecipesDataView>(lookupForEndProductBasePrice)
                .Unwind(endProductObjectFieldName, defaultUnwindOption)
                .Unwind($"{requitementsFieldName}.{requirementBaseItemField}", defaultUnwindOption)
                .Unwind(GE.PropertyName<RecipesDataView>(x => x.endProdPrice), defaultUnwindOption)
                .Unwind($"{requitementsFieldName}.{requirementItemPrice}", defaultUnwindOption)
                .AppendStage<RecipesDataView>(GE.buildAntiunwindRequest<RecipesDataView>());
        }

        public IAggregateFluent<RecipesDataView> buildView(IAggregateFluent<RecipesData> initedRequest)
        {
            return this.executeLookups(initedRequest);
        }
    }
    public class RecipeProjection
    {
        public string outputId;
        public string[] requirementsIds;
    }
}
