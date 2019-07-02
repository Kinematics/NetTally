using System.Net.Http;
using Microsoft.Extensions.DependencyInjection;
using NetTally.Forums;
using NetTally.Output;
using NetTally.SystemInfo;
using NetTally.Utility;
using NetTally.Utility.Comparers;
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

            services.AddSingleton<ITextResultsProvider, TallyOutput>();
            services.AddSingleton<IPageProvider, WebPageProvider>();
            services.AddSingleton<IVoteCounter, VoteCounter>();
            services.AddSingleton<HttpClientHandler, HttpClientHandler>();
            services.AddSingleton<IClock, SystemClock>();
            services.AddSingleton<IHash, NormalHash>();
            services.AddSingleton<Tally>();
            services.AddSingleton<CheckForNewRelease>();
            services.AddSingleton<ForumReader>();
            services.AddSingleton<ViewModelService>();
        }
    }
}
