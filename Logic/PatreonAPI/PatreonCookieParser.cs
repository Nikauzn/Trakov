using System;
using Microsoft.AspNetCore.Http;

namespace Trakov.Backend.Logic.PatreonAPI
{
    public interface IPatreonCookieParser
    {
        public PatreonExchangeResponse parseFromCookie(IRequestCookieCollection cookies);
        public void assignTokenToResponse(IResponseCookies cookies, PatreonExchangeResponse token, CookieOptions options = null);
    }
    public class PatreonCookieParser : IPatreonCookieParser
    {
        public void assignTokenToResponse(IResponseCookies cookies, PatreonExchangeResponse token, CookieOptions options = null)
        {
            if (options == null)
                options = new CookieOptions();
            cookies.Append($"patreon_{GE.PropertyName<PatreonExchangeResponse>(x => x.access_token)}", token.access_token, options);
            cookies.Append($"patreon_{GE.PropertyName<PatreonExchangeResponse>(x => x.refresh_token)}", token.refresh_token, options);
            cookies.Append($"patreon_{GE.PropertyName<PatreonExchangeResponse>(x => x.expires_in)}", token.expires_in.ToString(), options);
            cookies.Append($"patreon_{GE.PropertyName<PatreonExchangeResponse>(x => x.receivedAt)}", token.receivedAt.Ticks.ToString(), options);
        }
        public PatreonExchangeResponse parseFromCookie(IRequestCookieCollection cookies)
        {
            return new PatreonExchangeResponse()
            {
                access_token = cookies[$"patreon_{GE.PropertyName<PatreonExchangeResponse>(x => x.access_token)}"],
                refresh_token = cookies[$"patreon_{GE.PropertyName<PatreonExchangeResponse>(x => x.refresh_token)}"],
                expires_in = int.Parse(cookies[$"patreon_{GE.PropertyName<PatreonExchangeResponse>(x => x.expires_in)}"]),
                receivedAt = new DateTime(long.Parse(cookies[$"patreon_{GE.PropertyName<PatreonExchangeResponse>(x => x.receivedAt)}"]))
            };
        }
    }
}
