﻿using System.Net.Http;
using Microsoft.Extensions.DependencyInjection;
using NetTally.Cache;
using NetTally.Forums;
using NetTally.Options;
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
            services.AddSingleton<IGlobalOptions>(AdvancedOptions.Instance);
            services.AddSingleton<IGeneralInputOptions>(AdvancedOptions.Instance);
            services.AddSingleton<IGeneralOutputOptions>(AdvancedOptions.Instance);

            services.AddSingleton<ViewModel>();
            services.AddSingleton<ICache<string>, PageCache>();
            services.AddSingleton<IClock, SystemClock>();
            services.AddSingleton<IHash, NormalHash>();
            services.AddSingleton<IAgnostic, Agnostic>();
            services.AddSingleton<CheckForNewRelease>();
            services.AddSingleton<Logger2>();

            services.AddTransient<HttpClientHandler, HttpClientHandler>();

            services.AddSingleton<Tally>();
            services.AddSingleton<ForumAdapterFactory>();
            services.AddSingleton<IVoteCounter, VoteCounter>();
            services.AddTransient<IPageProvider, WebPageProvider>();
            services.AddTransient<ForumReader>();

            services.AddSingleton<VoteConstructor>();
            services.AddSingleton<RankVoteCounterFactory>();
            services.AddTransient<ITextResultsProvider, TallyOutput>();

            services.AddSingleton<MainViewModel>();
            services.AddTransient<ManageVotesViewModel>();
            services.AddTransient<QuestOptionsViewModel>();
            services.AddTransient<TasksViewModel>();
        }
    }
}
