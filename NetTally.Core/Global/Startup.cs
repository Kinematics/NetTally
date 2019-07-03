using System;
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
            services.AddSingleton<IGlobalOptions>(AdvancedOptions.Instance);
            services.AddSingleton<IGeneralInputOptions>(AdvancedOptions.Instance);
            services.AddSingleton<IGeneralOutputOptions>(AdvancedOptions.Instance);

            services.AddSingleton<MainViewModel>();
            services.AddSingleton<ViewModelService>();

            services.AddTransient<IPageProvider, WebPageProvider>();
            services.AddTransient<HttpClientHandler, HttpClientHandler>();
            services.AddSingleton<ICache<string>, PageCache>();
            services.AddSingleton<IClock, SystemClock>();
            services.AddTransient<ForumReader>();
            services.AddSingleton<IVoteCounter, VoteCounter>();
            services.AddSingleton<ITextResultsProvider, TallyOutput>();
            services.AddSingleton<Tally>();
            services.AddSingleton<VoteConstructor>();
            services.AddSingleton<IHash, NormalHash>();
            services.AddSingleton<CheckForNewRelease>();
        }
    }
}
