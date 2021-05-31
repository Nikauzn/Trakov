using System;

namespace Trakov.Backend.Logic.PatreonAPI
{
    public class RefreshToken
    {
        public string userUid;
        public DateTime issued = DateTime.UtcNow;
        public long expiresIn = (long)TimeSpan.FromDays(15).TotalSeconds;
        public string refresh_token = GE.RandomString(25);
    }
}
