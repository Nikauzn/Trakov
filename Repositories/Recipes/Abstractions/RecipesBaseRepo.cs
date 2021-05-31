using MongoDB.Driver;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Trakov.Backend.Logic.Tarkov;
using Trakov.Backend.Mongo;

namespace Trakov.Backend.Repositories
{
    public abstract class RecipesRepositoryMongoBase : BaseRepo, IRecipesRepository
    {
        public override string collectionName { get { return "recipes"; } }
        public RecipesRepositoryMongoBase(MongoService mongoService) : base(mongoService)
        {
        }

        public abstract Task<IEnumerable<string>> getRecipeItems();
        public abstract Task<List<RecipesDataView>> buildView(int page = 0, int itemsPerPage = 5,
            Dictionary<string, SortDirection> sortParams = null,
            FilterDefinition<RecipesData> requestParams = null);

        protected ProjectionDefinition<RecipesData, RecipeProjection> getDefaultProjection()
        {
            return Builders<RecipesData>.Projection.Expression(p => new RecipeProjection
            {
                outputId = p.endProduct,
                requirementsIds = p.requirements.Where(x => x.templateId != null).Select(x => x.templateId).ToArray()
            });
        }
    }
}
