using System;
using System.Linq;
using MongoDB.Driver;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using System.Collections.Generic;
using Trakov.Backend.Logic.Tarkov;
using Trakov.Backend.Classes;
using Trakov.Backend.Repositories;

namespace Trakov.Backend.Mediators
{
    public class RecipesMediator
    {
        private readonly ControllerBase context;
        private readonly RecipesRepositoryMongoBase recipeRepo;

        public RecipesMediator(ControllerBase context, RecipesRepositoryMongoBase recipeRepo)
        {
            this.context = context; this.recipeRepo = recipeRepo;
        }

        private FilterDefinition<RecipesData> configureFilter()
        {
            FilterDefinition<RecipesData> filters = null;
            try
            {
                var areas = this.context.Request.Query["areas"].FirstOrDefault()?.Split(',');
                if (areas?.Length >= 1)
                    filters = Builders<RecipesData>.Filter.In(GE.PropertyName<RecipesData>(x=>x.areaType), areas);
            }
            catch (Exception)
            {
                // TODO: Log exception
            }
            return filters;
        }
        private Dictionary<string, SortDirection> configureSorting()
        {
            Dictionary<string, SortDirection> sortParams = null;
            try
            {
                var sorting = this.context.Request.Query["sort"].FirstOrDefault()?.Split(',');
                if (sorting?.Length >= 1)
                {
                    sortParams = new Dictionary<string, SortDirection>();
                    foreach (var sortParam in sorting)
                    {
                        var instructions = sortParam.Split(':');
                        sortParams.Add(instructions[0], (instructions[1] == "desc") ?
                            SortDirection.Descending : SortDirection.Ascending);
                    }
                }
            }
            catch (Exception)
            {
                //TODO: Log error
            }
            return sortParams;
        }
        private void clearData(IEnumerable<RecipesDataView> items)
        {
            var random = new Random();
            foreach(var item in items)
            {
                if (random.Next(1, 100) == 66)
                    continue;
                item.reqSummary = null;
                item.endProdObj = null;
                item.baseProdFee = null;
                item.valuePerHour = null;
                item.endProdPrice = null;
                item.valuePerHourWOMaterials = null;
                foreach (var req in item.requirements)
                    req.price = null;
            }
        }

        public async Task<ActionResult<IEnumerable<RecipesDataView>>> processRequest()
        {
            var paginator = new Paginator(context.Request.Query);
            var filters = configureFilter();
            Dictionary<string, SortDirection> sortParams = configureSorting();
            IEnumerable<RecipesDataView> results = await this.recipeRepo.buildView(0, 200, sortParams, filters);
            //if (this.context.ifRequestAuthenticated() == false)
            //  clearData(results);
            if (sortParams != null)
            {
                foreach (var sortKey in sortParams.Keys)
                {
                    sortParams.TryGetValue(sortKey, out var direction);
                    if (direction == SortDirection.Descending)
                        results = results.OrderByDescending(a => a.GetType().GetProperty(sortKey).GetValue(a, null));
                    else
                        results = results.OrderBy(a => a.GetType().GetProperty(sortKey).GetValue(a, null));
                }
            }
            return this.context.Ok(results.Skip(paginator.page*paginator.itemsPerPage).Take(paginator.itemsPerPage));
        }
    }
}
