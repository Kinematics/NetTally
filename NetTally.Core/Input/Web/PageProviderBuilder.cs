using System;
using System.Diagnostics;
using System.Net;
using System.Net.Http;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using NetTally.Utility;

namespace NetTally.Web
{
    /// <summary>
    /// Builder class for page providers.
    /// </summary>
    public class PageProviderBuilder
    {
        #region Lazy singleton creation
        static readonly Lazy<PageProviderBuilder> lazy = new Lazy<PageProviderBuilder>(() => new PageProviderBuilder());
        public static PageProviderBuilder Instance => lazy.Value;
        PageProviderBuilder() { }
        #endregion

        #region Builder fields
        HttpClientHandler buildHandler;
        WebCache buildCache;
        IClock buildClock;
        Type providerType;
        #endregion

        public PageProviderBuilder HttpClientHandler(HttpClientHandler handler)
        {
            return new PageProviderBuilder() { buildHandler = handler, buildCache = this.buildCache, buildClock = this.buildClock, providerType = this.providerType };
        }
        public PageProviderBuilder PageCache(WebCache cache)
        {
            return new PageProviderBuilder() { buildHandler = this.buildHandler, buildCache = cache, buildClock = this.buildClock, providerType = this.providerType };
        }
        public PageProviderBuilder ActiveClock(IClock clock)
        {
            return new PageProviderBuilder() { buildHandler = this.buildHandler, buildCache = this.buildCache, buildClock = clock, providerType = this.providerType };
        }
        public PageProviderBuilder ProviderType(Type type)
        {
            return new PageProviderBuilder() { buildHandler = this.buildHandler, buildCache = this.buildCache, buildClock = this.buildClock, providerType = type };
        }

        public IPageProvider Build()
        {
            IPageProvider pageProvider;

            if (providerType is null)
            {
                pageProvider = new WebPageProvider(buildHandler, buildCache, buildClock);
            }
            else if (providerType.Equals(typeof(WebPageProvider)))
            {
                pageProvider = new WebPageProvider(buildHandler, buildCache, buildClock);
            }
            else
            {
                pageProvider = new WebPageProvider(buildHandler, buildCache, buildClock);
            }

            return pageProvider;
        }
    }
}
