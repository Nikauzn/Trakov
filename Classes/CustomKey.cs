using Microsoft.Extensions.Configuration;

namespace Trakov.Backend.Logic.PatreonAPI
{
    public interface ICustomKey
    {
        public string key { get; set; }
    }
    public class CustomKey : ICustomKey
    {
        public string key { get; set; }

        public static CustomKey initFromSection(IConfigurationSection section)
        {
            return new CustomKey()
            {
                key = section.GetValue<string>("key")
            };
        }
    }
}
