using System;
using System.Net.Http;
using NetTally.Collections;
using NetTally.Output;
using NetTally.VoteCounting;
using NetTally.Web;
using System.Globalization;

namespace NetTally.ViewModels
{
    public class ViewModelService
    {
        #region Lazy singleton creation
        static readonly Lazy<ViewModelService> lazy = new Lazy<ViewModelService>(() => new ViewModelService());

        public static ViewModelService Instance => lazy.Value;

        ViewModelService()
        {
        }
        #endregion

        public ViewModelService TextResultsProvider(ITextResultsProvider resultsProvider)
        {
            this.resultsProvider = resultsProvider;
            return this;
        }
        public ViewModelService PageProvider(IPageProvider pageProvider)
        {
            this.pageProvider = pageProvider;
            return this;
        }
        public ViewModelService VoteCounter(IVoteCounter voteCounter)
        {
            this.voteCounter = voteCounter;
            return this;
        }
        public ViewModelService HttpClient(HttpClientHandler handler)
        {
            this.handler = handler;
            return this;
        }
        public ViewModelService Configure(QuestCollection quests, string currentQuest)
        {
            if (this.quests == null)
                this.quests = quests;
            this.currentQuest = currentQuest;
            return this;
        }
        public ViewModelService HashAgnosticStringsUsing(Func<string, CompareInfo, CompareOptions, int> hashFunction)
        {
            this.hashFunction = hashFunction;
            return this;
        }
        public MainViewModel Build()
        {
            var vm = new MainViewModel(quests, currentQuest, handler, pageProvider, voteCounter, resultsProvider, hashFunction);

            if (MainViewModel == null)
                MainViewModel = vm;

            return vm;
        }

        IPageProvider pageProvider;
        HttpClientHandler handler;
        IVoteCounter voteCounter;
        ITextResultsProvider resultsProvider;
        QuestCollection quests;
        string currentQuest;
        Func<string, CompareInfo, CompareOptions, int> hashFunction;

        public static MainViewModel MainViewModel { get; private set; }
    }
}
