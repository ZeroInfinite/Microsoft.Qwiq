using Microsoft.Qwiq.Exceptions;
using Microsoft.Qwiq.Proxies;
using Tfs = Microsoft.TeamFoundation.WorkItemTracking.Client;

namespace Microsoft.Qwiq
{
    internal interface IQueryFactory
    {
        IQuery Create(string wiql, bool dayPrecision);
    }

    internal class QueryFactory : IQueryFactory
    {
        private readonly Tfs.WorkItemStore _store;

        private QueryFactory(Tfs.WorkItemStore store)
        {
            _store = store;
        }

        public static QueryFactory GetInstance(Tfs.WorkItemStore store)
        {
            return new QueryFactory(store);
        }

        public IQuery Create(string wiql, bool dayPrecision)
        {
            return ExceptionHandlingDynamicProxyFactory.Create<IQuery>(new QueryProxy(new Tfs.Query(_store, wiql, null, dayPrecision)));
        }
    }
}

