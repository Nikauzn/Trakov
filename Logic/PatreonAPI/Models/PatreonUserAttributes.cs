using System;

namespace Trakov.Backend.Logic.PatreonAPI
{
    public class PatreonUserAttributes : PatreonExchangeResponse
    {
        public DateTime created { get; set; }
        public string default_country_code { get; set; }
        public object discord_id { get; set; }
        public string email { get; set; }
        public string first_name { get; set; }
        public string full_name { get; set; }
        public string google_id { get; set; }
        public bool has_password { get; set; }
        public string image_url { get; set; }
        public bool is_deleted { get; set; }
        public bool is_email_verified { get; set; }
        public bool is_nuked { get; set; }
        public bool is_suspended { get; set; }
        public string last_name { get; set; }
    }

}
