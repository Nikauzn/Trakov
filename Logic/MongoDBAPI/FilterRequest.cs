namespace Trakov.Backend.Repositories.Recipes
{
    public class FilterRequest
    {
        public enum FilterType
        {
            Equivalent, In, GreaterThan
        }
        public enum RulesType
        {
            and, or, xor
        }

        public string fieldToFilter;
        public FilterType filterType;
        public RulesType rule = RulesType.and;
        public object request;
    }
}
