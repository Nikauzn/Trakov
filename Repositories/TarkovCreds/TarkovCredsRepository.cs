using MongoDB.Driver;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Trakov.Backend.Logic.Tarkov;
using Trakov.Backend.Mongo;

namespace Trakov.Backend.Repositories
{
    internal interface ITarkovCredsRepository
    {
        Task<TarkovCreds> getCredsByEmail(string email);
    }
    public abstract class TarkovCredsRepoBase : BaseRepo, ITarkovCredsRepository
    {
        protected TarkovCredsRepoBase(MongoService service) : base(service)
        {
            this.ensureIndexes();
        }

        public Task<TarkovCreds> getCredsByEmail(string email)
        {
            var filter = Builders<Logic.Tarkov.TarkovCreds>.Filter.Eq(GE.PropertyName<ITarkovCreds>(x=>x.email), email);
            return this.getCollection<Logic.Tarkov.TarkovCreds>().Find(filter).FirstOrDefaultAsync();
        }

        private void ensureIndexes()
        {
            string emailField = GE.PropertyName<ITarkovCreds>(x => x.email);

            var createRequestList = new List<CreateIndexModel<ITarkovCreds>>
            {
                new CreateIndexModel<ITarkovCreds>(
                new IndexKeysDefinitionBuilder<ITarkovCreds>()
                .Ascending(emailField),
                new CreateIndexOptions() { Unique = true })
            };

            this.getCollection<ITarkovCreds>().Indexes.CreateMany(createRequestList);
        }
    }
    public class TarkovCredsRepository : TarkovCredsRepoBase
    {
        public TarkovCredsRepository(MongoService service) : base(service)
        {
        }

        public override string collectionName { get { return "tarkov-creds"; } }

        public override Task<IList<T>> extractData<T>(int page, int elementsPerPage,
            Dictionary<string, SortDirection> sortParams = null, FilterDefinition<T> requestParams = null)
        {
            throw new NotImplementedException();
        }
    }
}
