using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Threading;
using System.Threading.Tasks;
using NetTally.CustomEventArgs;
using NetTally.Forums;
using NetTally.ViewModels;
using NetTally.Web;

namespace NetTally.VoteCounting
{
    /// <summary>
    /// Class that links together the various pieces of the tally system.
    /// Call this to run a tally.
    /// </summary>
    public partial class Tally : INotifyPropertyChanged, IDisposable
    {
        #region Local State
        // Disposal
        bool _disposed;

        // State
        bool tallyIsRunning;
        string results = string.Empty;
        string changingResults = string.Empty;

        IVoteCounter VoteCounter;
        MainViewModel? mainViewModel;
        ForumReader forumReader;

        // Tracking cancellations
        List<CancellationTokenSource> sources = new List<CancellationTokenSource>();
        #endregion

        #region Construction
        public Tally(IPageProvider pageProvider, IVoteCounter voteCounter, ForumReader reader)
        {
            VoteCounter = voteCounter;
            forumReader = reader;

            // Hook up to event notifications
            pageProvider.StatusChanged += PageProvider_StatusChanged;
            AdvancedOptions.Instance.PropertyChanged += Options_PropertyChanged;
        }

        public void Initialize(MainViewModel mainViewModel)
        {
            this.mainViewModel = mainViewModel;
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
                AdvancedOptions.Instance.PropertyChanged -= Options_PropertyChanged;
            }

            if (mainViewModel != null)
            {
                mainViewModel.PageProvider.StatusChanged -= PageProvider_StatusChanged;
            }

            _disposed = true;
        }
        #endregion

        #region Event monitoring
        /// <summary>
        /// Keep watch for any status messasges from the page provider, and add them
        /// to the TallyResults string so that they can be displayed in the UI.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e">Contains the text to be added to the output.</param>
        private void PageProvider_StatusChanged(object sender, MessageEventArgs e)
        {
            if (!string.IsNullOrEmpty(e.Message))
            {
                OnPropertyDataChanged(e.Message, "TallyResultsStatusChanged");
                TallyResults = TallyResults + e.Message;
            }
        }

        /// <summary>
        /// Listener for if any global options change.
        /// If the display mode changes, update the output results.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e">Contains info about which program option was updated.</param>
        private async void Options_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == "DisplayMode" || e.PropertyName == "RankVoteCounterMethod" || e.PropertyName == "DebugMode")
            {
                await RunWithTallyFlagAsync(UpdateResults);
            }
        }

        /// <summary>
        /// Listener for if any quest options change.
        /// Update the tally if needed, and update the output results afterwards.
        /// </summary>
        /// <param name="sender">The quest that sent the notification.</param>
        /// <param name="e">Info about a property of the quest that changed.</param>
        private async void Quest_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (sender is IQuest quest)
            {
                if (quest == VoteCounter.Quest && e.PropertyName == "PartitionMode")
                {
                    try
                    {
                        await RunWithTallyFlagAsync(UpdateTally)
                            .ContinueWith(updatedTally => RunWithTallyFlagAsync(UpdateResults), TaskContinuationOptions.NotOnCanceled)
                            .ContinueWith(updatedTally => TallyResults = "Canceled!", TaskContinuationOptions.OnlyOnCanceled);
                    }
                    catch (OperationCanceledException)
                    {
                    }
                }
            }
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
        /// If it changes to or from an empty string, the HasTallyResults property also changes.
        /// </summary>
        public string TallyResults
        {
            get { return results; }
            set
            {
                bool hasResultsChanged = string.IsNullOrEmpty(results) ^ string.IsNullOrEmpty(value);

                results = value;
                OnPropertyChanged();
                if (hasResultsChanged)
                    OnPropertyChanged(nameof(HasTallyResults));
            }
        }

        public bool HasTallyResults => !string.IsNullOrEmpty(results);
        #endregion

        #region Interface functions
        /// <summary>
        /// Run the tally for the specified quest.
        /// </summary>
        /// <param name="quest">The quest to scan.</param>
        /// <param name="token">Cancellation token.</param>
        public async Task RunAsync(IQuest quest, CancellationToken token)
        {
            if (quest == null)
                throw new ArgumentNullException(nameof(quest));

            try
            {
                TallyIsRunning = true;
                TallyResults = string.Empty;

                // Mark the quest as one that we will listen for changes from.
                quest.PropertyChanged -= Quest_PropertyChanged;
                quest.PropertyChanged += Quest_PropertyChanged;

                VoteCounter.ResetUserDefinedTasks(quest.DisplayName);

                var posts = await forumReader.ReadQuestAsync(quest, token).ConfigureAwait(false);

                await VoteCounter.TallyPosts(posts, quest, token).ConfigureAwait(false);
            }
            catch (InvalidOperationException e)
            {
                TallyResults += $"\n{e.Message}";
                return;
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception)
            {
                //VoteCounter.Instance.Quest = null;
                throw;
            }
            finally
            {
                TallyIsRunning = false;

                // Notify the page provider that we're done, and that the cache
                // can be cleared out as needed:
                mainViewModel?.PageProvider.DoneLoading();

                // Free memory used by loading pages as soon as we're done:
                GC.Collect();
            }

            await UpdateResults(token).ConfigureAwait(false);
        }

        /// <summary>
        /// Compose the tallied results into a string to put in the TallyResults property,
        /// for display in the UI.
        /// </summary>
        public async Task UpdateResults()
        {
            await RunWithTallyFlagAsync(UpdateResults);
        }

        /// <summary>
        /// Allow manual clearing of the page cache.
        /// </summary>
        public void ClearPageCache()
        {
            mainViewModel?.PageProvider.ClearPageCache();
            VoteCounter.ResetUserMerges();
        }
        #endregion

        #region Private update methods
        /// <summary>
        /// Process the results of the tally through the vote counter, and update the output.
        /// </summary>
        private async Task UpdateTally(CancellationToken token)
        {
            // Tally the votes from the loaded pages.
            await VoteCounter.TallyPosts(token).ConfigureAwait(false);
        }

        /// <summary>
        /// Compose the tallied results into a string to put in the TallyResults property,
        /// for display in the UI.
        /// </summary>
        private async Task UpdateResults(CancellationToken token)
        {
            if (mainViewModel != null)
            {
                TallyResults = await mainViewModel.TextResultsProvider
                    .BuildOutputAsync(AdvancedOptions.Instance.DisplayMode, token).ConfigureAwait(false);
            }
        }
        #endregion

        #region Cancellable function calls that signal the TallyIsRunning flag.
        /// <summary>
        /// Run the specified function with the TallyIsRunning flag active.
        /// Provide a cancellation token to the specified function.
        /// </summary>
        /// <param name="action">A cancellable function.</param>
        private void RunWithTallyFlag(Action<CancellationToken> action)
        {
            if (action == null)
                throw new ArgumentNullException(nameof(action));

            CancellationTokenSource? cts = null;

            try
            {
                using (cts = new CancellationTokenSource())
                {
                    sources.Add(cts);

                    bool rememberDuringRun = TallyIsRunning;

                    try
                    {
                        TallyIsRunning = true;

                        action(cts.Token);
                    }
                    finally
                    {
                        TallyIsRunning = rememberDuringRun;
                    }
                }
            }
            finally
            {
                if (cts != null)
                    sources.Remove(cts);
            }
        }

        /// <summary>
        /// Run the specified async function with the TallyIsRunning flag active.
        /// Provide a cancellation token to the specified function.
        /// </summary>
        /// <param name="action">A cancellable async function.</param>
        private async Task RunWithTallyFlagAsync(Func<CancellationToken, Task> action)
        {
            if (action == null)
                throw new ArgumentNullException(nameof(action));

            CancellationTokenSource? cts = null;

            try
            {
                using (cts = new CancellationTokenSource())
                {
                    sources.Add(cts);

                    bool rememberDuringRun = TallyIsRunning;

                    try
                    {
                        TallyIsRunning = true;

                        await action(cts.Token).ConfigureAwait(false);
                    }
                    finally
                    {
                        TallyIsRunning = rememberDuringRun;
                    }
                }
            }
            finally
            {
                if (cts != null)
                    sources.Remove(cts);
            }
        }

        /// <summary>
        /// Cancel any functions running under the above RunWithTallyFlag functions
        /// </summary>
        public void Cancel()
        {
            foreach (var cts in sources)
            {
                cts.Cancel();
            }
        }
        #endregion
    }
}
