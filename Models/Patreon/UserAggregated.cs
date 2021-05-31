using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Bson.Serialization.IdGenerators;

namespace Trakov.Backend.Logic.PatreonAPI
{
    public class UserAggregated : PatreonUserAttributes
    {
        [BsonId(IdGenerator = typeof(StringObjectIdGenerator))]
        public string _id { get; set; }
        public string status { get; set; }

        public AuthorizedResponse getAuthorizedData()
        {
            return new AuthorizedResponse()
            {
                displayName = this.full_name,
                profileIcon = this.image_url,
                uid = this._id
            };
        }
    }
}
