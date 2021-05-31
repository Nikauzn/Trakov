using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using Trakov.Backend.Logic.Tarkov;

namespace Trakov.Backend.Repositories.Localization
{
    public class LocaleRepository : ILocaleRepository
    {
        private Dictionary<string, ResponseLocale> localeCache = new Dictionary<string, ResponseLocale>();

        public LocaleRepository()
        {
            retrieveFromLocalFileCache();
        }

        private void retriveFile(string localeCode)
        {
            string fname = $"tarkov-locales-{localeCode}.json";
            if (File.Exists(fname))
            {
                var locale = JsonConvert.DeserializeObject<ResponseLocale>(File.ReadAllText(fname));
                this.localeCache.Add(localeCode, locale);
            }
        }
        private void retrieveFromLocalFileCache()
        {
            retriveFile("en"); retriveFile("ru");
        }
        private string enumToString(ITarkovService.Locales locale)
        {
            switch (locale)
            {
                case ITarkovService.Locales.English:
                    return "en";
                case ITarkovService.Locales.Russian:
                    return "ru";
                default:
                    throw new Exception($"{nameof(locale)} is not supported");
            }
        }

        public string searchForValue(ITarkovService.Locales locale, string itemId)
        {
            string localeCode = enumToString(locale);
            this.localeCache.TryGetValue(localeCode, out ResponseLocale data);
            TemplateData result = null;
            data?.data?.templates?.TryGetValue(itemId, out result);
            if (result != null)
                return result.Name;
            else
                return "";
        }
        public void cacheLocale(ITarkovService.Locales locale, ResponseLocale item)
        {
            lock (this)
            {
                string localeCode = enumToString(locale);
                if (localeCache.ContainsKey(localeCode))
                    localeCache.Remove(localeCode);
                localeCache.Add(localeCode, item);
                File.WriteAllText(
                    $"tarkov-locales-{localeCode}.json",
                    JsonConvert.SerializeObject(item));
            }
        }
        public ResponseLocale retrieveCache(ITarkovService.Locales locale, ResponseLocale item)
        {
            this.localeCache.TryGetValue(enumToString(locale), out ResponseLocale data);
            return data;
        }
    }
}
