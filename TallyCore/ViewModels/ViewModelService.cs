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
        public ViewModelService Configure(QuestCollectionWrapper config)
        {
            this.config = config;
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
        public void Build()
        {
            if (MainViewModel != null)
                throw new InvalidOperationException("Main view model has already been built.");

            MainViewModel = new MainViewModel(config, handler, pageProvider, resultsProvider, errorLogger, hashFunction);
        }

        IPageProvider pageProvider;
        HttpClientHandler handler;
        ITextResultsProvider resultsProvider;
        QuestCollectionWrapper config;
        IErrorLogger errorLogger;
        Func<string, CompareInfo, CompareOptions, int> hashFunction;

        public static MainViewModel MainViewModel { get; private set; }
    }
}
