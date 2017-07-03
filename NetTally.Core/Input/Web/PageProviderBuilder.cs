using System;
using System.Net.Http;
using NetTally.SystemInfo;

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
        IClock buildClock;
        Type providerType;
        #endregion

        public PageProviderBuilder HttpClientHandler(HttpClientHandler handler)
        {
            return new PageProviderBuilder() { buildHandler = handler, buildClock = this.buildClock, providerType = this.providerType };
        }
        public PageProviderBuilder ActiveClock(IClock clock)
        {
            return new PageProviderBuilder() { buildHandler = this.buildHandler, buildClock = clock, providerType = this.providerType };
        }
        public PageProviderBuilder ProviderType(Type type)
        {
            return new PageProviderBuilder() { buildHandler = this.buildHandler, buildClock = this.buildClock, providerType = type };
        }

        public IPageProvider Build()
        {
            IPageProvider pageProvider = new WebPageProvider(buildHandler, buildClock);

            buildHandler = null;
            buildClock = null;

            return pageProvider;
        }
    }
}
