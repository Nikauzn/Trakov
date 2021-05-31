using System;
using Newtonsoft.Json;

namespace Trakov.Backend.Logic.PatreonAPI
{
    public class PatreonExchangeResponse
    {
        public string access_token;
        public string refresh_token;
        public long expires_in;
        [JsonIgnore]
        public DateTime receivedAt = DateTime.Now;
        public string scope;
        public string version;

        public void applyExternalToken(PatreonExchangeResponse other)
        {
            this.access_token = other.access_token;
            this.refresh_token = other.refresh_token;
            this.expires_in = other.expires_in;
            this.scope = other.scope;
            this.version = other.version;
        }
    }

}
