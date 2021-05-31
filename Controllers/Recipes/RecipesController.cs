using System.Threading.Tasks;
using System.Collections.Generic;
using Trakov.Backend.Logic.Tarkov;
using Trakov.Backend.Mediators;
using Microsoft.AspNetCore.Mvc;
using Trakov.Backend.Repositories;

namespace Trakov.Backend.Controllers.Recipes
{
    [Route("api/[controller]")]
    [ApiController]
    public class RecipesController : Controller
    {
        private readonly RecipesRepositoryMongoBase recipeRepo;


        public RecipesController(RecipesRepositoryMongoBase recipeRepo)
        {
            this.recipeRepo = recipeRepo;
        }

        [HttpGet]
        public Task<ActionResult<IEnumerable<RecipesDataView>>> loadRecipesData()
        {
            var mediator = new RecipesMediator(this, this.recipeRepo);
            return mediator.processRequest();
        }
    }
}
