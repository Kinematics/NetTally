using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NetTally.CustomEventArgs;
using NetTally.Forums;
using NetTally.Options;
using NetTally.Output;
using NetTally.Votes;

namespace NetTally.VoteCounting
{
    /// <summary>
    /// Class that links together the various pieces of the tally system.
    /// Call this to run a tally.
    /// </summary>
    public partial class Tally : INotifyPropertyChanged, IDisposable
    {
        #region Construction
        // Disposal
        bool _disposed;

        // State
        bool tallyIsRunning;
        string results = string.Empty;

        readonly IVoteCounter voteCounter;
        readonly IServiceProvider serviceProvider;
        readonly VoteConstructor voteConstructor;
        readonly IGeneralOutputOptions outputOptions;
        readonly ILogger<Tally> logger;

        public VoteConstructor VoteConstructor => voteConstructor;

        // Tracking cancellations
        readonly List<CancellationTokenSource> sources = new List<CancellationTokenSource>();

        public Tally(IServiceProvider serviceProvider, VoteConstructor constructor,
            IVoteCounter counter, IGeneralOutputOptions options, ILoggerFactory loggerFactory)
        {
            this.serviceProvider = serviceProvider;
            voteConstructor = constructor;
            voteCounter = counter;
            outputOptions = options;
            logger = loggerFactory.CreateLogger<Tally>();

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
                if (!TallyIsRunning)
                {
                    await RunWithTallyIsRunningFlagAsync(UpdateResults);
                }
            }
        }

        /// <summary>
        /// Keep watch for any status messasges from the forum reader, and add them
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
                        await RunWithTallyIsRunningFlagAsync(UpdateTally)
                            .ContinueWith(updatedTally => RunWithTallyIsRunningFlagAsync(UpdateResults), TaskContinuationOptions.NotOnCanceled)
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
                if (value != tallyIsRunning)
                {
                    tallyIsRunning = value;
                    OnPropertyChanged();
                }
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

                        var (threadTitle, posts) = await forumReader.ReadQuestAsync(quest, token).ConfigureAwait(false);

                        voteCounter.Title = threadTitle;

                        Stopwatch stopwatch = new Stopwatch();
                        stopwatch.Start();
                        await TallyPosts(posts, quest, token).ConfigureAwait(false);
                        stopwatch.Stop();
                        logger.LogDebug($"Time to process posts: {stopwatch.ElapsedMilliseconds} ms.");
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
            await RunWithTallyIsRunningFlagAsync(UpdateResults);
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
            var textResultsProvider = serviceProvider.GetRequiredService<ITextResultsProvider>();

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
        private void RunWithTallyIsRunningFlag(Action<CancellationToken> action)
        {
            if (action == null)
                throw new ArgumentNullException(nameof(action));

            CancellationTokenSource cts = null;

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
        private async Task RunWithTallyIsRunningFlagAsync(Func<CancellationToken, Task> action)
        {
            if (action == null)
                throw new ArgumentNullException(nameof(action));

            CancellationTokenSource cts = null;

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
                    catch (InvalidOperationException)
                    {
                        // This might be called on startup, in which case an error is thrown because there's no quest set yet.
                        System.Diagnostics.Debug.WriteLine("InvalidOperationException");
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

        #region Handle running the actual tally
        /// <summary>
        /// Run the tally using the provided posts, for the selected quest.
        /// </summary>
        /// <param name="posts">The posts to be tallied.</param>
        /// <param name="quest">The quest being tallied.</param>
        /// <param name="token">Cancellation token.</param>
        public async Task TallyPosts(IEnumerable<Post> posts, IQuest quest, CancellationToken token)
        {
            voteCounter.Quest = quest;
            voteCounter.AddPosts(posts);
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

                if (voteCounter.Posts.Count == 0)
                    return;

                await PreprocessPosts(token).ConfigureAwait(false);
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
        }
        #endregion

        #region Preprocessing
        /// <summary>
        /// The first half of tallying posts involves doing the preprocessing
        /// work on the plans in the post list.
        /// </summary>
        public async Task PreprocessPosts(CancellationToken token)
        {
            if (voteCounter.Quest is null)
                return;

            foreach (var post in voteCounter.Posts)
            {
                // Reset the processed state of all the posts.
                post.Processed = false;
                post.ForceProcess = false;
                post.WorkingVoteComplete = false;
                post.WorkingVote.Clear();
                voteCounter.AddReferenceVoter(post.Origin);
            }

            List<(bool asBlocks, Func<IEnumerable<VoteLine>, (bool isPlan, bool isImplicit, string planName)> isPlanFunction)> planProcesses =
                new List<(bool asBlocks, Func<IEnumerable<VoteLine>, (bool isPlan, bool isImplicit, string planName)> isPlanFunction)>()
                {
                    (asBlocks: true, isPlanFunction: VoteBlocks.IsBlockAProposedPlan),
                    (asBlocks: true, isPlanFunction: VoteBlocks.IsBlockAnExplicitPlan),
                    (asBlocks: false, isPlanFunction: VoteBlocks.IsBlockAnImplicitPlan),
                    (asBlocks: false, isPlanFunction: VoteBlocks.IsBlockASingleLinePlan)
                };

            // Run the above series of preprocessing functions to extract plans from the post list.
            var allPlans = RunPlanPreprocessing(voteCounter.Posts, voteCounter.Quest, planProcesses, token);

            await Task.FromResult(0);
        }


        /// <summary>
        /// Run the logic for the sequence of processing phases for plan
        /// examination and extraction.
        /// </summary>
        /// <param name="posts">The posts being examined for plans.</param>
        /// <param name="quest">The quest being tallied.</param>
        /// <param name="planProcesses">The list of functions to run on the posts.</param>
        /// <param name="token">The cancellation token.</param>
        /// <returns>Returns a collection of named plans, and the vote lines that comprise them.</returns>
        private Dictionary<string, VoteLineBlock> RunPlanPreprocessing(IReadOnlyList<Post> posts, IQuest quest,
            List<(bool asBlocks, Func<IEnumerable<VoteLine>, (bool isPlan, bool isImplicit, string planName)> isPlanFunction)> planProcesses,
            CancellationToken token)
        {
            Dictionary<string, VoteLineBlock> allPlans = new Dictionary<string, VoteLineBlock>();

            foreach (var (asBlocks, isPlanFunction) in planProcesses)
            {
                token.ThrowIfCancellationRequested();

                foreach (var post in posts)
                {
                    var plans = voteConstructor.PreprocessPostGetPlans(post, quest, asBlocks, isPlanFunction);

                    foreach (var plan in plans)
                    {
                        // Convert "Base/Proposed Plan" to "Plan" before saving.
                        // Set to an undefined marker.
                        (string normalPlanName, VoteLineBlock normalPlanContents) = voteConstructor.NormalizePlan(plan.Key, plan.Value);

                        bool added = voteCounter.AddReferencePlan(post.Origin.GetPlanOrigin(normalPlanName), normalPlanContents);

                        if (added)
                        {
                            // Each new plan that gets added also needs to be run through partitioning,
                            // and have those results added as votes.
                            var planPartitions = voteConstructor.PartitionPlan(normalPlanContents, quest.PartitionMode);

                            var planOrigin = post.Origin.GetPlanOrigin(normalPlanName);

                            voteCounter.AddVotes(planPartitions, planOrigin);

                            allPlans.Add(normalPlanName, normalPlanContents);
                        }
                    }
                }
            }

            return allPlans;
        }
        #endregion

        #region Processing
        /// <summary>
        /// The second half of tallying the posts involves cycling through for
        /// as long as future references need to be handled.
        /// </summary>
        private async Task ProcessPosts(CancellationToken token)
        {
            if (voteCounter.Quest is null)
                throw new InvalidOperationException("Quest is null.");

            var unprocessed = voteCounter.Posts;

            // Loop as long as there are any more to process.
            while (unprocessed.Any())
            {
                token.ThrowIfCancellationRequested();

                bool processedAny = false;

                foreach (var post in unprocessed)
                {
                    var filteredResults = voteConstructor.ProcessPostGetVotes(post, voteCounter.Quest);

                    if (post.Processed)
                        processedAny = true;

                    if (filteredResults != null)
                    {
                        // Add those to the vote counter.
                        voteCounter.AddVotes(filteredResults, post.Origin);
                    }
                }

                if (processedAny)
                {
                    // As long as some got processed, remove those from the unprocessed list
                    // and let the loop run again.
                    unprocessed = unprocessed.Where(p => !p.Processed).ToList();
                }
                else
                {
                    // If none got processed (and there must be at least some waiting on processing),
                    // Set the ForceProcess flag on them to avoid pending FutureReference waits.
                    foreach (var post in unprocessed)
                    {
                        post.ForceProcess = true;
                    }
                }
            }

            voteCounter.AddUserDefinedTasksToTaskList();

            voteCounter.RunMergeActions();

            await Task.FromResult(0);
        }
        #endregion
    }
}
