using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using HtmlAgilityPack;
using NetTally.Output;
using NetTally.Web;

namespace NetTally
{
    /// <summary>
    /// Class that links together the various pieces of the tally system.
    /// Call this to run a tally.
    /// </summary>
    public class Tally : INotifyPropertyChanged, IDisposable
    {
        #region Local State
        // Disposal
        bool _disposed = false;

        // References
        IPageProvider PageProvider { get; } = new WebPageProvider2();
        ITextResultsProvider TextResults { get; } = new TallyOutput();

        // State
        DisplayMode displayMode = DisplayMode.Normal;
        bool tallyIsRunning = false;
        string results = string.Empty;

        // User data
        public HashSet<string> UserDefinedTasks { get; } = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        #endregion

        #region Construction and Disposal
        /// <summary>
        /// Default constructor.
        /// </summary>
        public Tally()
            : this(null)
        {
        }

        /// <summary>
        /// Constructor that allows overriding the default VoteCounter and PageProvider.
        /// </summary>
        /// <param name="altVoteCounter">Alternate VoteCounter.</param>
        /// <param name="altPageProvider">Alternate PageProvider.</param>
        public Tally(IPageProvider altPageProvider)
        {
            // Replace defaults if parameters are non-null.
            PageProvider = altPageProvider ?? PageProvider;

            PageProvider.StatusChanged += PageProvider_StatusChanged;
        }

        ~Tally()
        {
            Dispose(false);
        }

        public void Dispose()
        {
            Dispose(true); //I am calling you from Dispose, it's safe
            GC.SuppressFinalize(this); //Hey, GC: don't bother calling finalize later
        }

        protected virtual void Dispose(Boolean itIsSafeToAlsoFreeManagedObjects)
        {
            if (_disposed)
                return;

            if (itIsSafeToAlsoFreeManagedObjects)
            {
                PageProvider.Dispose();
            }

            _disposed = true;
        }
        #endregion

        #region Event handling
        /// <summary>
        /// Keep watch for any status messasges from the page provider, and add them
        /// to the TallyResults string so that they can be displayed in the UI.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void PageProvider_StatusChanged(object sender, MessageEventArgs e)
        {
            TallyResults = TallyResults + e.Message;
        }

        /// <summary>
        /// Event for INotifyPropertyChanged.
        /// </summary>
        public event PropertyChangedEventHandler PropertyChanged;

        /// <summary>
        /// Function to raise events when a property has been changed.
        /// </summary>
        /// <param name="propertyName">The name of the property that was modified.</param>
        protected void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
        #endregion

        #region Current Properties
        /// <summary>
        /// Flag whether the tally is currently running.
        /// </summary>
        public bool TallyIsRunning
        {
            get { return tallyIsRunning; }
            set
            {
                tallyIsRunning = value;
                OnPropertyChanged();
            }
        }

        /// <summary>
        /// The string containing the current tally progress or results.
        /// Creates a notification event if the contents change.
        /// </summary>
        public string TallyResults
        {
            get { return results; }
            set
            {
                results = value;
                OnPropertyChanged();
            }
        }

        /// <summary>
        /// Enum of the type of display composition methodology to use for the output display.
        /// Recalculates the display if changed.
        /// </summary>
        public DisplayMode DisplayMode
        {
            get { return displayMode; }
            set
            {
                displayMode = value;
                UpdateResults();
            }
        }
        #endregion

        #region Interface functions
        /// <summary>
        /// Run the tally for the specified quest.
        /// </summary>
        /// <param name="questTitle">The name of the quest thread to scan.</param>
        /// <param name="startPost">The starting post number.</param>
        /// <param name="endPost">The ending post number.</param>
        /// <returns></returns>
        public async Task Run(IQuest quest, CancellationToken token)
        {
            try
            {
                List<Task<HtmlDocument>> loadedPages;

                TallyIsRunning = true;
                TallyResults = string.Empty;

                if (VoteCounter.Instance.Quest?.DisplayName != quest.DisplayName)
                    UserDefinedTasks.Clear();

                await quest.InitForumAdapter(token).ConfigureAwait(false);

                // Figure out what page to start from
                var startInfo = await quest.GetStartInfo(PageProvider, token);

                // Load pages from the website
                loadedPages = await quest.LoadQuestPages(startInfo, PageProvider, token).ConfigureAwait(false);

                if (loadedPages != null && loadedPages.Count > 0)
                {
                    // Tally the votes from the loaded pages.
                    await VoteCounter.Instance.TallyVotes(quest, startInfo, loadedPages).ConfigureAwait(false);

                    // Compose the final result string from the compiled votes.
                    UpdateResults();
                }

                GC.Collect();
            }
            catch (Exception)
            {
                VoteCounter.Instance.Quest = null;
                throw;
            }
            finally
            {
                TallyIsRunning = false;
                PageProvider.DoneLoading();
            }
        }

        /// <summary>
        /// Process the results of the tally through the vote counter, and update the output.
        /// </summary>
        /// <param name="changedQuest"></param>
        public void UpdateTally(IQuest changedQuest)
        {
            if (VoteCounter.Instance.Quest != null && changedQuest == VoteCounter.Instance.Quest)
            {
                // Tally the votes from the loaded pages.
                VoteCounter.Instance.TallyPosts();

                // Compose the final result string from the compiled votes.
                UpdateResults();
            }
        }

        /// <summary>
        /// Compose the tallied results into a string to put in the TallyResults property,
        /// for display in the UI.
        /// </summary>
        public void UpdateResults()
        {
            if (VoteCounter.Instance.Quest != null)
                TallyResults = TextResults.BuildOutput(DisplayMode);
        }

        /// <summary>
        /// Allow manual clearing of the page cache.
        /// </summary>
        public void ClearPageCache()
        {
            PageProvider.ClearPageCache();
        }
        #endregion
    }
}
