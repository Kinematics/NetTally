﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using NetTally.CustomEventArgs;
using NetTally.Forums;
using NetTally.Options;
using NetTally.Output;
using NetTally.Votes;
using NetTally.Experiment3;
using NetTally.Utility;

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
        readonly IGeneralOutputOptions outputOptions;

        public VoteConstructor VoteConstructor => voteConstructor;

        // Tracking cancellations
        readonly List<CancellationTokenSource> sources = new List<CancellationTokenSource>();
        #endregion

        #region Construction
        public Tally(IServiceProvider serviceProvider, VoteConstructor constructor,
            IVoteCounter counter, IGeneralOutputOptions options)
        {
            this.serviceProvider = serviceProvider;
            voteConstructor = constructor;
            voteCounter = counter;
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

                        var (threadTitle, posts) = await forumReader.ReadQuestAsync(quest, token).ConfigureAwait(false);

                        voteCounter.Title = threadTitle;

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

        #region From VoteCounter

        /// <summary>
        /// Run the tally using the provided posts, for the selected quest.
        /// </summary>
        /// <param name="posts">The posts to be tallied.</param>
        /// <param name="quest">The quest being tallied.</param>
        /// <param name="token">Cancellation token.</param>
        public async Task TallyPosts(IEnumerable<Experiment3.Post> posts, IQuest quest, CancellationToken token)
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

                await PreprocessVotes(token).ConfigureAwait(false);
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
        private async Task PreprocessVotes(CancellationToken token)
        {
            if (voteCounter.Quest is null)
                return;


            List<(bool asBlocks, Func<IEnumerable<VoteLine>, (bool isPlan, string planName)> isPlanFunction)> planProcesses =
                new List<(bool asBlocks, Func<IEnumerable<VoteLine>, (bool isPlan, string planName)> isPlanFunction)>()
                {
                    (asBlocks: true, isPlanFunction: VoteBlocks.IsBlockABasePlan),
                    (asBlocks: true, isPlanFunction: VoteBlocks.IsBlockAnExplicitPlan),
                    (asBlocks: false, isPlanFunction: VoteBlocks.IsBlockAnImplicitPlan),
                    (asBlocks: false, isPlanFunction: VoteBlocks.IsBlockASingleLinePlan)
                };

            // Run the above series of preprocessing functions to extract plans from the post list.
            var allPlans = RunPlanPreprocessing(voteCounter.PostsList, voteCounter.Quest, planProcesses, token);

            PrepPostsForProcessing();

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

                bool processedAny = false;

                foreach (var post in unprocessed)
                {
                    var filteredResults = voteConstructor.ProcessPostEx3(post, voteCounter.Quest);

                    if (post.Processed)
                        processedAny = true;

                    if (filteredResults != null)
                    {
                        // Add those to the vote counter.
                        voteCounter.AddVotes(filteredResults, post.Author, post.ID, VoteType.Vote);
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

            await Task.FromResult(0);
        }


        private Dictionary<string, IEnumerable<VoteLine>> RunPlanPreprocessing(List<Post> posts, IQuest quest,
            List<(bool asBlocks, Func<IEnumerable<VoteLine>, (bool isPlan, string planName)> isPlanFunction)> planProcesses,
            CancellationToken token)
        {
            Dictionary<string, IEnumerable<VoteLine>> allPlans = new Dictionary<string, IEnumerable<VoteLine>>();

            foreach (var (asBlocks, isPlanFunction) in planProcesses)
            {
                token.ThrowIfCancellationRequested();

                foreach (var post in posts)
                {
                    var plans = voteConstructor.PreprocessGetPlans(post, quest, asBlocks, isPlanFunction);

                    foreach (var plan in plans)
                    {
                        bool added = voteCounter.AddReferencePlan(plan.Key, plan.Value, post.ID);

                        if (added)
                        {
                            //var nPlan = NormalizePlanName(plan);

                            //var votePartitions = voteConstructor.GetVotePartitions(plan.Value, quest.PartitionMode, VoteType.Plan, post.Author);
                            //voteCounter.AddVotes(votePartitions, plan.Key, post.ID, VoteType.Plan);

                            allPlans.Add(plan.Key, plan.Value);
                        }
                    }
                }
            }

            return allPlans;
        }

        /// <summary>
        /// Run through each post and determine if any of them contain
        /// base/proposal plans (which are registered, but not voted for).
        /// If so, strip them from the list of vote lines we work on during
        /// processing.
        /// Also condense regular plan votes to just the plan vote line.
        /// That allows votes to be expanded later, as needed.
        /// </summary>
        private void PrepPostsForProcessing()
        {
            foreach (var post in voteCounter.PostsList)
            {
                // Reset the processed state of all the posts.
                post.Processed = false;
                post.ForceProcess = false;
                UpdateWorkingVoteLines(post);
            }

            foreach (var post in voteCounter.PostsList)
            {
                // And store all post authors and their last post ID with an actual vote for later.
                if (post.WorkingVoteLines.Count > 0)
                    voteCounter.AddReferenceVoter(post.Author, post.ID);
            }
        }

        /// <summary>
        /// Update the working vote based on filtering out components of preprocessed plans.
        /// </summary>
        /// <param name="post">The post to update.</param>
        private void UpdateWorkingVoteLines(Post post)
        {
            post.WorkingVoteLines.Clear();

            // Base plans are skipped entirely, if this is the original post that proposed the plan.
            // Keep everything else.
            var blocks = VoteBlocks.GetBlocks(post.VoteLines);

            foreach (var block in blocks)
            {
                var (isBasePlan, basePlanName) = VoteBlocks.IsBlockABasePlan(block);

                if (isBasePlan)
                {
                    string originalPostIdForPlan = voteCounter.GetPlanPostId(basePlanName);
                    if (originalPostIdForPlan == post.ID)
                    {
                        continue;
                    }
                }

                post.WorkingVoteLines.AddRange(block);
            }

            // An implicit plan takes up the entire working vote.
            // Save the first line that names it; we'll expand it again later.
            var (isImplicitPlan, _) = VoteBlocks.IsBlockAnImplicitPlan(post.WorkingVoteLines);

            if (isImplicitPlan)
            {
                var line = post.WorkingVoteLines.First();
                post.WorkingVoteLines.Clear();
                post.WorkingVoteLines.Add(line);
            }
            else
            {
                // Any other plans get condensed down to just the naming line.
                // Any non-plan lines are kept as-is.
                blocks = VoteBlocks.GetBlocks(post.WorkingVoteLines);
                List<VoteLine> tempVote = new List<VoteLine>();

                foreach (var block in blocks)
                {
                    var (isExplicitPlan, _) = VoteBlocks.IsBlockAnExplicitPlan(block);

                    if (isExplicitPlan)
                    {
                        tempVote.Add(block.First());
                    }
                    else
                    {
                        tempVote.AddRange(block);
                    }
                }

                post.WorkingVoteLines.Clear();
                post.WorkingVoteLines.AddRange(tempVote);
            }
        }
        #endregion
    }
}
