using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Trakov.Backend.Classes;
using Trakov.Backend.Logic;
using Trakov.Backend.Repositories;

namespace Trakov.Backend.Controllers.Market
{
    [ApiController]
    [Route("api/[controller]")]
    public class MarketController : Controller
    {
        private readonly MarketPricesTableViewBuilder viewBuilder;
        private readonly ItemsBaseRepo itemsRepo;

        public MarketController(MarketPricesTableViewBuilder viewBuilder, ItemsBaseRepo itemsRepo)
        {
            this.viewBuilder = viewBuilder; 
            this.itemsRepo = itemsRepo;
        }

        [HttpGet("{locale}")]
        public Task<List<MarketPricesTableProjection>> GetMainView(string locale = "en")
        {
            var paginator = new Paginator(Request.Query);
            var overrider = this.Request.Query["overrider"].FirstOrDefault();
            var queryString = paginator.queryString;
            return viewBuilder.buildView(paginator.page, paginator.itemsPerPage, queryString,
                ((overrider != null) ? DateTime.Parse(overrider) : DateTime.UtcNow), locale, paginator.filtersString);
        }

        [HttpGet("filters/{locale}/{parentId?}")]
        public async Task<IEnumerable<LocalizableWithId>> GetItemFiltersGroups(string parentId = null, string locale = "en")
        {
            var result = await itemsRepo.GetItemsGroups(locale, (parentId != null) ? parentId : Constant.topNodeId);
            return result.Where(x => x.name != null); 
        }
    }
}
