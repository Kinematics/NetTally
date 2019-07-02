using System.Net.Http;
using Microsoft.Extensions.DependencyInjection;
using NetTally.Output;
using NetTally.SystemInfo;
using NetTally.ViewModels;
using NetTally.VoteCounting;
using NetTally.Web;

namespace NetTally
{
    public static class Startup
    {
        public static void ConfigureServices(IServiceCollection services)
        {
            services.AddSingleton<MainViewModel>();
            services.AddSingleton<AdvancedOptions>();

            services.AddTransient<ITextResultsProvider, TallyOutput>();
            services.AddTransient<IPageProvider, WebPageProvider>();
            services.AddTransient<IVoteCounter, VoteCounter>();
            services.AddTransient<HttpClientHandler, HttpClientHandler>();
            services.AddTransient<IClock, SystemClock>();
        }
    }
}
