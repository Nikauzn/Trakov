namespace Trakov.Backend.Logic.PatreonAPI
{
    public interface IPatreonSettings
    {
        public string client_id { get; set; }
        public string client_secret { get; set; }
        public string redirect_uri { get; set; }
        public int patreonSubscriptionId { get; set; }
    }
}
