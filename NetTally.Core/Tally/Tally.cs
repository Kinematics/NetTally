using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using NetTally.Cache;
using NetTally.CustomEventArgs;
using NetTally.Forums;
using NetTally.Options;
using NetTally.Output;
using NetTally.ViewModels;
using NetTally.Votes;
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

        readonly IVoteCounter voteCounter;
        readonly IServiceProvider serviceProvider;
        readonly VoteConstructor voteConstructor;
        readonly ITextResultsProvider textResultsProvider;
        readonly IGeneralOutputOptions outputOptions;

        public VoteConstructor VoteConstructor => voteConstructor;

        // Tracking cancellations
        readonly List<CancellationTokenSource> sources = new List<CancellationTokenSource>();
        #endregion

        #region Construction
        public Tally(IServiceProvider serviceProvider, VoteConstructor constructor,
            IVoteCounter counter, ITextResultsProvider textResults, IGeneralOutputOptions options)
        {
            this.serviceProvider = serviceProvider;
            voteConstructor = constructor;
            voteCounter = counter;
            textResultsProvider = textResults;
            outputOptions = options;

            // Hook up to event notifications
            outputOptions.PropertyChanged += Options_PropertyChanged;
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
                outputOptions.PropertyChanged -= Options_PropertyChanged;
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
        private void ForumReader_StatusChanged(object sender, MessageEventArgs e)
        {
            if (!string.IsNullOrEmpty(e.Message))
            {
                OnPropertyDataChanged(e.Message, "TallyResultsStatusChanged");
                TallyResults += e.Message;
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
            if (e.PropertyName == "DisplayMode" 
                || e.PropertyName == "RankVoteCounterMethod" 
                || e.PropertyName == "DebugMode")
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
                if (quest == voteCounter.Quest && e.PropertyName == "PartitionMode")
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

                voteCounter.ResetUserDefinedTasks(quest.DisplayName);

                using (var forumReader = serviceProvider.GetRequiredService<ForumReader>())
                {
                    try
                    {
                        forumReader.StatusChanged += ForumReader_StatusChanged;

                        var posts = await forumReader.ReadQuestAsync(quest, token).ConfigureAwait(false);

                        await TallyPosts(posts, quest, token).ConfigureAwait(false);
                    }
                    finally
                    {
                        forumReader.StatusChanged -= ForumReader_StatusChanged;
                    }
                }
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
        #endregion

        #region Private update methods
        /// <summary>
        /// Process the results of the tally through the vote counter, and update the output.
        /// </summary>
        private async Task UpdateTally(CancellationToken token)
        {
            // Tally the votes from the loaded pages.
            await TallyPosts(token).ConfigureAwait(false);
        }

        /// <summary>
        /// Compose the tallied results into a string to put in the TallyResults property,
        /// for display in the UI.
        /// </summary>
        private async Task UpdateResults(CancellationToken token)
        {
            TallyResults = await textResultsProvider
                .BuildOutputAsync(outputOptions.DisplayMode, token).ConfigureAwait(false);
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

        #region From VoteCounter

        /// <summary>
        /// Run the tally using the provided posts, for the selected quest.
        /// </summary>
        /// <param name="posts">The posts to be tallied.</param>
        /// <param name="quest">The quest being tallied.</param>
        /// <param name="token">Cancellation token.</param>
        public async Task TallyPosts(IEnumerable<PostComponents> posts, IQuest quest, CancellationToken token)
        {
            voteCounter.Quest = quest;
            voteCounter.PostsList.Clear();
            voteCounter.PostsList.AddRange(posts);
            await TallyPosts(token).ConfigureAwait(false);
        }

        /// <summary>
        /// Construct the tally results based on the stored list of posts.
        /// Run async so that it doesn't cause UI jank.
        /// </summary>
        /// <param name="token">Cancellation token.</param>
        public async Task TallyPosts(CancellationToken token)
        {
            if (voteCounter.Quest is null)
                return;

            try
            {
                voteCounter.VoteCounterIsTallying = true;
                voteCounter.TallyWasCanceled = false;

                voteCounter.Reset();

                if (voteCounter.PostsList == null || voteCounter.PostsList.Count == 0)
                    return;

                await PreprocessPlans(token).ConfigureAwait(false);
                await ProcessPosts(token).ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
                voteCounter.TallyWasCanceled = true;
            }
            finally
            {
                voteCounter.VoteCounterIsTallying = false;
            }

            voteCounter.OrderedTaskList.AddRange(voteCounter.KnownTasks);
            OnPropertyChanged("Tasks");
        }

        /// <summary>
        /// The first half of tallying posts involves doing the preprocessing
        /// work on the plans in the post list.
        /// </summary>
        private async Task PreprocessPlans(CancellationToken token)
        {
            token.ThrowIfCancellationRequested();

            if (voteCounter.Quest is null)
                return;

            // Preprocessing Phase 1 (Only plans with contents are counted as plans.)
            foreach (var post in voteCounter.PostsList)
            {
                voteCounter.ReferenceVoters.Add(post.Author);
                voteCounter.ReferenceVoterPosts[post.Author] = post.ID;
                voteConstructor.PreprocessPlansWithContent(post, voteCounter.Quest);
            }

            token.ThrowIfCancellationRequested();

            // Preprocessing Phase 2 (Full-post plans may be named (ie: where the plan name has no contents).)
            // Total vote must have multiple lines.
            foreach (var post in voteCounter.PostsList)
            {
                voteConstructor.PreprocessPlanLabelsWithContent(post, voteCounter.Quest);
            }

            token.ThrowIfCancellationRequested();

            // Preprocessing Phase 3 (Full-post plans may be named (ie: where the plan name has no contents).)
            // Total vote may be only one line.
            foreach (var post in voteCounter.PostsList)
            {
                voteConstructor.PreprocessPlanLabelsWithoutContent(post, voteCounter.Quest);
            }

            token.ThrowIfCancellationRequested();

            // Once all the plans are in place, set the working votes for each post.
            foreach (var post in voteCounter.PostsList)
            {
                post.SetWorkingVote(p => voteConstructor.GetWorkingVote(p));
            }

            await Task.FromResult(0);
        }

        /// <summary>
        /// The second half of tallying the posts involves cycling through for
        /// as long as future references need to be handled.
        /// </summary>
        private async Task ProcessPosts(CancellationToken token)
        {
            if (voteCounter.Quest is null)
                throw new InvalidOperationException("Quest is null.");

            var unprocessed = voteCounter.PostsList;

            // Loop as long as there are any more to process.
            while (unprocessed.Any())
            {
                token.ThrowIfCancellationRequested();

                // Get the list of the ones that were processed.
                var processed = unprocessed.Where(p => voteConstructor.ProcessPost(p, voteCounter.Quest!, token) == true).ToList();

                // As long as some got processed, remove those from the unprocessed list
                // and let the loop run again.
                if (processed.Any())
                {
                    unprocessed = unprocessed.Except(processed).ToList();
                }
                else
                {
                    // If none got processed (and there must be at least some waiting on processing),
                    // Set the ForceProcess flag on them to avoid pending FutureReference waits.
                    foreach (var p in unprocessed)
                    {
                        p.ForceProcess = true;
                    }
                }
            }

            await Task.FromResult(0);
        }


        #endregion
    }
}
