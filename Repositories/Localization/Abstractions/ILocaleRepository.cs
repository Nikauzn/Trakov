using Trakov.Backend.Logic.Tarkov;

namespace Trakov.Backend.Repositories.Localization
{
    public interface ILocaleRepository
    {
        void cacheLocale(ITarkovService.Locales locale, ResponseLocale item);
        ResponseLocale retrieveCache(ITarkovService.Locales locale, ResponseLocale item);
        string searchForValue(ITarkovService.Locales locale, string itemId);
    }
}
