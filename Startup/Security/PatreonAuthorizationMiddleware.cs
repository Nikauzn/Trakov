using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.Security.Principal;
using System.Text.Encodings.Web;
using System.Threading.Tasks;
using Trakov.Backend.Logic.PatreonAPI;

namespace Trakov.Backend
{
    public class PatreonAuthorizationMiddleware : AuthenticationHandler<PatreonSettings>
    {
        private readonly IPatreonCookieParser parser;

        public PatreonAuthorizationMiddleware(IOptionsMonitor<PatreonSettings> options,
            ILoggerFactory logger, UrlEncoder encoder, ISystemClock clock) : base(options, logger, encoder, clock)
        {
            this.parser = (IPatreonCookieParser)Startup.Resolve(typeof(IPatreonCookieParser));
        }

        private string extractToken(HttpRequest context)
        {
            try
            {
                var extractedToken = parser.parseFromCookie(context.Cookies);
                //TODO: Refresh, if dead
                return extractedToken.access_token;
            }
            catch(Exception ex)
            {
            }
            return null;
        }

        protected override async Task<AuthenticateResult> HandleAuthenticateAsync()
        {
            var token = extractToken(this.Request);
            if (token?.Length > 0)
            {
                /*
                TODO: make request and check subscription
                var claims = await tryAssignPrincipal(token); 
                if (claims != null)
                {
                    var ticket = new AuthenticationTicket(claims, "Default");
                    return await Task.FromResult(AuthenticateResult.Success(ticket));
                }
                */
                var principal = new GenericPrincipal(new GenericIdentity(token), new string[] { });
                return await Task.FromResult(AuthenticateResult.Success(
                    new AuthenticationTicket(new System.Security.Claims.ClaimsPrincipal(principal), 
                    "Patreon")));
            }
            return await Task.FromResult(AuthenticateResult.Fail($"Patreon token is not authorized"));
        }
    }
}
