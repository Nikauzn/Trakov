using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Trakov.Backend.Logic.Tarkov;
using Trakov.Backend.Repositories;

namespace Trakov.Backend.Classes
{
    public static class Localizator
    {
        private static string localeToString(ITarkovService.Locales locale)
        {
            switch (locale)
            {
                case ITarkovService.Locales.English:
                    return "en";
                case ITarkovService.Locales.Russian:
                    return "ru";
                default:
                    throw new NotImplementedException($"{nameof(locale)} is not implemented");
            }
        }
        static public Task localize(ITarkovService.Locales locale, ResponseLocale localeTable,
            IEnumerable<AggregatedTarkovItem> items, ItemsBaseRepo itemsRepo)
        {
            var updates = new Dictionary<string, string>();
            foreach (var item in items)
            {
                localeTable.data.templates.TryGetValue(item._id, out var templateData);
                if (templateData == null)
                    continue;
                var localeValue = templateData.Name;
                updates.Add(item._id, localeValue);
            }
            var localeField = localeToString(locale);
            return itemsRepo.updateLocale(localeField, updates);
        }
    }
}
