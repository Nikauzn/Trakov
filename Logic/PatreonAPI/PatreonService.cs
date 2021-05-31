using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;
using Microsoft.IdentityModel.Tokens;
using MongoDB.Bson;
using MongoDB.Driver;
using Newtonsoft.Json;
using RestSharp;
using Trakov.Backend.Mongo;

namespace Trakov.Backend.Logic.PatreonAPI
{
    public interface IAuthService
    {
        public AuthorizedResponse issueNewJWTToken(UserAggregated userData);
        public AuthorizedResponse refreshJWT(string refreshToken);
    }
    public class AuthService : BaseRepo, IAuthService
    {
        private readonly SecurityKey signingKey;

        public AuthService(MongoService mService, ICustomKey signingKey) : base(mService)
        {
            this.signingKey = new SymmetricSecurityKey(Encoding.ASCII.GetBytes(signingKey.key));
        }

        public override string collectionName { get { return "sessions"; } }

        public override Task<IList<T>> extractData<T>(int page, int elementsPerPage, 
            Dictionary<string, SortDirection> sortParams = null, FilterDefinition<T> requestParams = null)
        {
            throw new NotImplementedException();
        }

        public AuthorizedResponse issueNewJWTToken(UserAggregated userData)
        {
            return this.writeTokenForUser(userData, this.signingKey);
        }

        public AuthorizedResponse refreshJWT(string refreshToken)
        {
            throw new NotImplementedException();
        }

        private AuthorizedResponse writeTokenForUser(UserAggregated user, SecurityKey signKey)
        {
            var authorized = user.getAuthorizedData();
            var claims = new List<Claim> { new Claim("User", JsonConvert.SerializeObject(authorized)) };

            ClaimsIdentity claimsIdentity = new ClaimsIdentity(claims);
            var Token = new JwtSecurityToken(
                    issuer: "DrAnaconda Labs",
                    audience: "Build Support",
                    notBefore: DateTime.Now,
                    claims: claimsIdentity.Claims,
                    // TODO Dynamic time generation
                    expires: DateTime.Now.Add(TimeSpan.FromDays(14)),
                    signingCredentials: new SigningCredentials(signKey, SecurityAlgorithms.HmacSha256));

            RefreshToken rt = new RefreshToken()
            {
                userUid = user._id
            };
            _ = this.service.getMainDatabase.GetCollection<RefreshToken>(this.collectionName)
                .InsertOneAsync(rt, new InsertOneOptions() { BypassDocumentValidation = false });

            authorized.refresh_token = rt.refresh_token;
            authorized.jwtToken =  new JwtSecurityTokenHandler().WriteToken(Token);
            return authorized;
        }
    }
    public interface IUserRepository
    {
        public Task<UserAggregated> registerPatreonUser(PatreonUserAttributes patreonUser);
    }
    public class UserRepository : BaseRepo, IUserRepository
    {
        public UserRepository(MongoService service) : base(service)
        {
            _ = this.ensureEmailIndex();
        }

        private async Task ensureEmailIndex()
        {
            var options = new CreateIndexOptions() { Unique = true };
            string emailFieldName = GE.PropertyName<UserAggregated>(x => x.email);
            var index = new BsonDocument(new BsonElement(emailFieldName, 1));
            IndexKeysDefinition<UserAggregated> keyCode = index;
            var codeIndexModel = new CreateIndexModel<UserAggregated>(keyCode, options);
            await this.service.getMainDatabase.GetCollection<UserAggregated>(this.collectionName)
                .Indexes.CreateOneAsync(codeIndexModel);
        }

        public override string collectionName { get { return "users"; } }

        public override Task<IList<T>> extractData<T>(int page, int elementsPerPage,
            Dictionary<string, SortDirection> sortParams = null, FilterDefinition<T> requestParams = null)
        {
            throw new NotImplementedException();
        }

        public async Task<UserAggregated> registerPatreonUser(PatreonUserAttributes patreonUser)
        {
            var collection = this.service.getMainDatabase.GetCollection<UserAggregated>(this.collectionName);
            try
            {
                var result = (UserAggregated)patreonUser;
                await collection.InsertOneAsync(result, new InsertOneOptions() { BypassDocumentValidation = false } );
                return result;
            }
            catch (MongoWriteException)
            {
                // TODO: Find user and update in background
                throw new NotImplementedException();
            }
        }
    }


    public interface IPatreonService
    {
        public Task<PatreonExchangeResponse> registerCode(string code);
        public Task<PatreonUserAttributes> getCurrentPatreonUser(string user_access_token);
        public Task<bool> isContainsPledge(string user_access_token);
    }
    public class PatreonService : IPatreonService
    {
        private readonly RestClient http = new RestClient("https://www.patreon.com/api");
        private readonly IPatreonSettings patreonSettings;

        public PatreonService(IPatreonSettings patreonSettings)
        {
            this.patreonSettings = patreonSettings;
        }

        public async Task<bool> isContainsPledge(string user_access_token)
        {
            string result = await this.checkUserPledgeStatus(user_access_token);
            return (result.ToLower().Contains("error"));
        }
        public Task<PatreonExchangeResponse> registerCode(string code)
        {
            return this.exchangeCode(code);
        }
        public Task<PatreonUserAttributes> getCurrentPatreonUser(string user_access_token)
        {
            return this.getUserByToken(user_access_token);
        }

        private async Task<PatreonExchangeResponse> exchangeCode(string code)
        {
            RestRequest req = new RestRequest("/oauth2/token", Method.POST, DataFormat.Json);
            req.AddParameter("code", code, ParameterType.GetOrPost);
            req.AddParameter("grant_type", "authorization_code", ParameterType.GetOrPost);
            req.AddParameter("client_id", patreonSettings.client_id, ParameterType.GetOrPost);
            req.AddParameter("client_secret", patreonSettings.client_secret, ParameterType.GetOrPost);
            req.AddParameter("redirect_uri", patreonSettings.redirect_uri, ParameterType.GetOrPost);
            req.AddParameter("Content-Type", "application/x-www-form-urlencoded", ParameterType.HttpHeader);
            var resp = await http.ExecuteAsync(req);
            if (resp.IsSuccessful && resp.StatusCode == System.Net.HttpStatusCode.OK)
            {
                return JsonConvert.DeserializeObject<PatreonExchangeResponse>(resp.Content);
            }
            return null;
            // TODO: Log error
        }
        private async Task<string> checkUserPledgeStatus(string user_access_token)
        {
            var req = new RestRequest($"/api/oauth2/v2/campaigns/{this.patreonSettings.patreonSubscriptionId}", 
                Method.GET, DataFormat.Json);
            req.AddParameter("Authorization", $"Bearer {user_access_token}", ParameterType.HttpHeader);
            var resp = await http.ExecuteAsync(req);
            if (resp.IsSuccessful && resp.StatusCode == System.Net.HttpStatusCode.OK)
            {
                //var test = 
                return resp.Content;
            }
            return null;
            //TODO: Log error
        }        
        private async Task<PatreonUserAttributes> getUserByToken(string user_access_token)
        {
            var req = new RestRequest($"/oauth2/api/current_user", Method.GET, DataFormat.Json);
            req.AddParameter("Authorization", $"Bearer {user_access_token}", ParameterType.HttpHeader);
            var resp = await http.ExecuteAsync(req);
            if (resp.IsSuccessful && resp.StatusCode == System.Net.HttpStatusCode.OK)
            {
                var respObj = JsonConvert.DeserializeObject<PatreonCurrentUserResponse>(resp.Content);
                return respObj.data.attributes;
            }
            return null;
        }
    }
}
