using System.Net.Http;
using Microsoft.Extensions.DependencyInjection;
using NetTally.Cache;
using NetTally.Forums;
using NetTally.Global;
using NetTally.Configure.Legacy;
using NetTally.Output;
using NetTally.SystemInfo;
using NetTally.Utility.Comparers;
using NetTally.ViewModels;
using NetTally.VoteCounting;
using NetTally.VoteCounting.RankVotes;
using NetTally.Votes;
using NetTally.Web;

namespace NetTally
{
    public static class Startup
    {
        public static void ConfigureServices(IServiceCollection services)
        {
            services.AddSingleton<IGlobalOptions>(GlobalOptionsConfig.Instance);
            services.AddSingleton<IGeneralInputOptions>(GlobalOptionsConfig.Instance);
            services.AddSingleton<IGeneralOutputOptions>(GlobalOptionsConfig.Instance);

            services.AddSingleton<ICache<string>, PageCache>();
            services.AddSingleton<IClock, SystemClock>();
            services.AddSingleton<IHash, NormalHash>();
            services.AddSingleton<IAgnostic, Agnostic>();
            services.AddSingleton<CheckForNewRelease>();

            services.AddTransient<HttpClientHandler, HttpClientHandler>();

            services.AddSingleton<Tallyer>();
            services.AddTransient<IVoteCounter, VoteCounter>();
            services.AddTransient<IPageProvider, WebPageProvider>();
            services.AddTransient<ForumReader>();
            services.AddSingleton<ForumAdapterFactory>();
            services.AddSingleton<ForumIdentifier>();

            services.AddSingleton<VoteConstructor>();
            services.AddSingleton<RankVoteCounterFactory>();
            services.AddSingleton<ITextResultsProvider, TallyOutput>();

            services.AddSingleton<MainViewModel>();
            services.AddTransient<ManageVotesViewModel>();
            services.AddTransient<QuestOptionsViewModel>();
            services.AddTransient<TasksViewModel>();
            services.AddTransient<GlobalOptionsViewModel>();

            
            services.AddSingleton<QuestsInfo>();

            services.AddSingleton<IQuestsInfo>(x => x.GetRequiredService<QuestsInfo>());
            services.AddSingleton<IQuestsInfoMod>(x => x.GetRequiredService<QuestsInfo>());
        }
    }
}
