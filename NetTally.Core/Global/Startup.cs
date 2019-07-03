using System.Net.Http;
using Microsoft.Extensions.DependencyInjection;
using NetTally.Cache;
using NetTally.Forums;
using NetTally.Options;
using NetTally.Output;
using NetTally.SystemInfo;
using NetTally.Utility;
using NetTally.Utility.Comparers;
using NetTally.ViewModels;
using NetTally.VoteCounting;
using NetTally.Votes;
using NetTally.Web;

namespace NetTally
{
    public static class Startup
    {
        public static void ConfigureServices(IServiceCollection services)
        {
            services.AddSingleton<MainViewModel>();

            services.AddSingleton<IGlobalOptions>(AdvancedOptions.Instance);
            services.AddSingleton<IGeneralInputOptions>(AdvancedOptions.Instance);
            services.AddSingleton<IGeneralOutputOptions>(AdvancedOptions.Instance);

            services.AddSingleton<ITextResultsProvider, TallyOutput>();
            services.AddSingleton<IPageProvider, WebPageProvider>();
            services.AddSingleton<IVoteCounter, VoteCounter>();
            services.AddSingleton<HttpClientHandler, HttpClientHandler>();
            services.AddSingleton<ICache<string>, PageCache>();
            services.AddSingleton<IClock, SystemClock>();
            services.AddSingleton<IHash, NormalHash>();
            services.AddSingleton<Tally>();
            services.AddSingleton<VoteConstructor>();
            services.AddSingleton<CheckForNewRelease>();
            services.AddSingleton<ForumReader>();
            services.AddSingleton<ViewModelService>();
        }
    }
}
