using Microsoft.AspNetCore.Http;
using System.Collections.Generic;

namespace Trakov.Backend.Classes
{
    public class Paginator
    {
        private const int defaultItemsPerPage = 10;

        private const char defaultQuerySeparator = ',';
        private const char defaultParamsSeparator = ':';

        private readonly IQueryCollection query;

        public Paginator(IQueryCollection query)
        {
            this.query = query;
        }

        public int page
        {
            get
            {
                return (this.query.ContainsKey("page") && this.query["page"][0]?.Length > 0) ? int.Parse(this.query["page"][0]) : 0;
            }
        }
        public int itemsPerPage
        {
            get
            {
                return (this.query.ContainsKey("items") && this.query["items"][0]?.Length > 0) ? int.Parse(this.query["items"][0]) : defaultItemsPerPage;
            }
        }
        public string queryString
        {
            get
            {
                return (this.query.ContainsKey("query") && this.query["query"][0]?.Length > 0) ? this.query["query"][0] : string.Empty;
            }
        }
        public string filtersString
        {
            get
            {
                return (this.query.ContainsKey("filters") && this.query["filters"][0]?.Length > 0) ? this.query["filters"][0] : string.Empty;
            }
        }

        public Dictionary<string, string> queryStringToDictionary()
        {
            var result = new Dictionary<string, string>();
            if (this.queryString?.Length > 0)
            {
                var directives = this.queryString.Split(',');
                foreach (var directive in directives)
                {
                    var paramz = directive.Split(':');
                    if (paramz.Length > 0)
                        result.Add(paramz[0], paramz[1]);
                }
            }
            return result;
        }
    }
}
