using System;
using System.Net.Http;
using NetTally.Collections;
using NetTally.Output;
using NetTally.Utility;
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
        public ViewModelService LogErrorsUsing(IErrorLogger errorLogger)
        {
            this.errorLogger = errorLogger;
            return this;
        }
        public ViewModelService HashAgnosticStringsUsing(Func<string, CompareInfo, CompareOptions, int> hashFunction)
        {
            this.hashFunction = hashFunction;
            return this;
        }
        public MainViewModel Build()
        {
            var vm = new MainViewModel(quests, currentQuest, handler, pageProvider, resultsProvider, errorLogger, hashFunction);

            if (MainViewModel == null)
                MainViewModel = vm;

            return vm;
        }

        IPageProvider pageProvider;
        HttpClientHandler handler;
        ITextResultsProvider resultsProvider;
        QuestCollection quests;
        string currentQuest;
        IErrorLogger errorLogger;
        Func<string, CompareInfo, CompareOptions, int> hashFunction;

        public static MainViewModel MainViewModel { get; private set; }
    }
}
