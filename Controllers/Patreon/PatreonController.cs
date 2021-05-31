using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Trakov.Backend.Logic.PatreonAPI;

namespace Trakov.Backend.Controllers.Patreon
{
    [Route("api/[controller]")]
    [ApiController]
    public class PatreonController : Controller
    {
        private readonly IPatreonService pService;
        private readonly IUserRepository userRepository;
        private readonly IPatreonCookieParser parser;
        //private readonly IAuthService authService;

        public PatreonController(IPatreonService pService, IUserRepository userRepo, IPatreonCookieParser parser)
        {
            this.pService = pService; this.userRepository = userRepo;  //this.authService = authService;
            this.parser = parser;
        }

        [HttpGet]
        public async Task<IActionResult> RegisterUser()
        {
            var code = this.Request.Query["code"].FirstOrDefault();
            if (code?.Length > 1)
            {
                var token = await pService.registerCode(code);
                if (token != null)
                {
                    var userData = await this.pService.getCurrentPatreonUser(token.access_token);
                    if (userData != null)
                    {
                        userData.applyExternalToken(token);
                        //var created = await this.userRepository.registerPatreonUser(userData);
                        //var localJWT = this.authService.issueNewJWTToken(created);
                    }
                    var options = new CookieOptions()
                    {
                        HttpOnly = false, // TODO: change in prod
                        Secure = false,
                        Path = "/",
                        Domain = this.Request.Host.Value
                    };
                    this.parser.assignTokenToResponse(this.Response.Cookies, token, null);
                    return Redirect("http://localhost:4200/callback");
                }
                else
                {
                }
            }
            return BadRequest();
        }
    }
}
