namespace Trakov.Backend.Logic.PatreonAPI
{
    public class AuthorizedResponse
    {
        public string uid { get; set; }
        public string displayName { get; set; }
        public string profileIcon { get; set; }
        public string refresh_token { get; set; }
        public string status { get; set; }
        public string jwtToken { get; set; }
    }
}
