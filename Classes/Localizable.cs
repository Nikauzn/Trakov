using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Bson.Serialization.IdGenerators;
using Newtonsoft.Json;
using Trakov.Backend.Repositories;

namespace Trakov.Backend.Classes
{
    public interface ILocalizable
    {
        public string ru { get; set; }
        public string en { get; set; }
    }
    public class Localizable : ILocalizable
    {
        [JsonIgnore] public string ru { get; set; }
        [JsonIgnore] public string en { get; set; }
    }
    public class LocalizableWithId : Localizable, IEntity
    {
        [BsonId(IdGenerator = typeof(StringObjectIdGenerator))]
        public string _id { get; set; }
        public string name 
        { 
            get 
            {
                return getName();
            } 
        }

        private string getName()
        {
            if (ru?.Length > 0)
                return ru;
            else if (en?.Length > 0)
                return en;
            else return null;
        }
    }
}
