using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading;

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
        public void Build()
        {
            if (MainViewModel != null)
                throw new InvalidOperationException("Main view model has already been built.");

            MainViewModel = new MainViewModel(config, handler, pageProvider, resultsProvider);
        }

        IPageProvider pageProvider;
        HttpClientHandler handler;
        ITextResultsProvider resultsProvider;
        QuestCollectionWrapper config;

        public static MainViewModel MainViewModel { get; private set; }
    }
}
