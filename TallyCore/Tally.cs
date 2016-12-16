using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using NetTally.Output;
using NetTally.CustomEventArgs;
using NetTally.ViewModels;

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
        bool _disposed;

        // State
        bool tallyIsRunning;
        string results = string.Empty;

        // User data
        public HashSet<string> UserDefinedTasks { get; } = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        #endregion

        #region Construction
        public Tally(IPageProvider pageProvider)
        {
            // Hook up to event notifications
            pageProvider.StatusChanged += PageProvider_StatusChanged;
            AdvancedOptions.Instance.PropertyChanged += Options_PropertyChanged;
        }
        #endregion

        #region Disposal
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
                ViewModelService.MainViewModel.PageProvider.StatusChanged -= PageProvider_StatusChanged;
                AdvancedOptions.Instance.PropertyChanged -= Options_PropertyChanged;
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
        /// Listener for if any global options change.
        /// If the display mode changes, update the output results.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Options_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == "DisplayMode" || e.PropertyName == "RankVoteCounterMethod")
                UpdateResults();
        }

        /// <summary>
        /// Listener for if any quest options change.  Update the tally if needed.
        /// </summary>
        /// <param name="sender">The quest that sent the notification.</param>
        /// <param name="e">Info about a property of the quest that changed.</param>
        private void Quest_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            IQuest quest = sender as IQuest;
            if (quest != null && quest == VoteCounter.Instance.Quest)
            {
                if (e.PropertyName == "PartitionMode")
                    UpdateTally();
            }
        }
        #endregion

        #region Implement INotifyPropertyChanged interface
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
        #endregion

        #region Interface functions
        /// <summary>
        /// Run the tally for the specified quest.
        /// </summary>
        /// <param name="quest">The quest to scan.</param>
        /// <param name="token">Cancellation token.</param>
        public async Task RunAsync(IQuest quest, CancellationToken token)
        {
            try
            {
                // Mark the quest as one that we will listen for changes from.
                quest.PropertyChanged -= Quest_PropertyChanged;
                quest.PropertyChanged += Quest_PropertyChanged;

                TallyIsRunning = true;
                TallyResults = string.Empty;

                if (VoteCounter.Instance.Quest?.DisplayName != quest.DisplayName)
                    UserDefinedTasks.Clear();

                var posts = await Forums.ForumReader.Instance.ReadQuestAsync(quest, token);
                VoteCounter.Instance.TallyPosts(posts, quest);
                UpdateResults();
            }
            catch (Exception)
            {
                VoteCounter.Instance.Quest = null;
                throw;
            }
            finally
            {
                TallyIsRunning = false;
                ViewModelService.MainViewModel.PageProvider.DoneLoading();

                GC.Collect();
            }
        }

        /// <summary>
        /// Compose the tallied results into a string to put in the TallyResults property,
        /// for display in the UI.
        /// </summary>
        public void UpdateResults()
        {
            if (VoteCounter.Instance.Quest != null)
                TallyResults = ViewModelService.MainViewModel.TextResultsProvider.BuildOutput(AdvancedOptions.Instance.DisplayMode);
        }

        /// <summary>
        /// Allow manual clearing of the page cache.
        /// </summary>
        public void ClearPageCache()
        {
            ViewModelService.MainViewModel.PageProvider.ClearPageCache();
        }
        #endregion

        #region Private update methods
        /// <summary>
        /// Process the results of the tally through the vote counter, and update the output.
        /// </summary>
        private void UpdateTally()
        {
            if (VoteCounter.Instance.Quest != null)
            {
                // Tally the votes from the loaded pages.
                VoteCounter.Instance.TallyPosts();

                // Compose the final result string from the compiled votes.
                UpdateResults();
            }
        }
        #endregion
    }
}
