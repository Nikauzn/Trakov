using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Configuration;

namespace Trakov.Backend.Logic.PatreonAPI
{
    public class PatreonSettings : AuthenticationSchemeOptions, IPatreonSettings
    {
        public string client_id { get; set; }
        public string client_secret { get; set; }
        public string redirect_uri { get; set; }
        public int patreonSubscriptionId { get; set; }

        public static PatreonSettings initFromSection(IConfigurationSection section)
        {
            return new PatreonSettings()
            {
                client_id = section.GetValue<string>(GE.PropertyName<PatreonSettings>(x=>x.client_id)),
                client_secret = section.GetValue<string>(GE.PropertyName<PatreonSettings>(x => x.client_secret)),
                redirect_uri = section.GetValue<string>(GE.PropertyName<PatreonSettings>(x =>x.redirect_uri)),
                patreonSubscriptionId = section.GetValue<int>(GE.PropertyName<PatreonSettings>(x => x.patreonSubscriptionId))
            };
        }
    }
}
