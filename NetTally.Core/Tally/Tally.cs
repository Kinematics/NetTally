using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NetTally.CustomEventArgs;
using NetTally.Forums;
using NetTally.Output;
using NetTally.Types.Components;
using NetTally.Votes;

namespace NetTally.VoteCounting
{
    /// <summary>
    /// Class that links together the various pieces of the tally system.
    /// Call this to run a tally.
    /// </summary>
    public partial class Tally : ObservableObject
    {
        #region Construction
        private readonly ITextResultsProvider textResultsProvider;
        private readonly IServiceProvider serviceProvider;
        private readonly VoteConstructor voteConstructor;
        private readonly ILogger<Tally> logger;

        public VoteConstructor VoteConstructor => voteConstructor;

        public Tally(IServiceProvider serviceProvider,
                     VoteConstructor voteConstructor,
                     ITextResultsProvider textResultsProvider,
                     ILogger<Tally> logger)
        {
            this.serviceProvider = serviceProvider;
            this.voteConstructor = voteConstructor;
            this.textResultsProvider = textResultsProvider;
            this.logger = logger;
        }
        #endregion

        #region Current Properties
        [ObservableProperty]
        bool tallyIsRunning;

        /// <summary>
        /// The string containing the current tally progress or results.
        /// Creates a notification event if the contents change.
        /// If it changes to or from an empty string, the HasTallyResults property also changes.
        /// </summary>
        [ObservableProperty]
        private string tallyResults = string.Empty;
        [ObservableProperty]
        private bool hasTallyResults = false;

        partial void OnTallyResultsChanged(string? oldValue, string newValue)
        {
            if (string.IsNullOrEmpty(oldValue) ^ string.IsNullOrEmpty(newValue))
            {
                HasTallyResults = string.IsNullOrEmpty(newValue);
            }
        }
        #endregion


        #region Public Methods
        public async Task RunTallyAsync(Quest quest, CancellationToken cancellationToken)
        {
            TallyResults = string.Empty;

            try
            {
                await ReadPostsFromQuestAsync(quest, cancellationToken).ConfigureAwait(false);
                UpdateTally(quest);
            }
            finally
            {
                // Free memory used by loading pages as soon as we're done:
                GC.Collect();
            }
        }

        public void UpdateTally(Quest quest)
        {
            if (quest.VoteCounter.Posts.Count > 0)
            {
                ConstructVotesFromPosts(quest);
                UpdateOutput(quest);
            }
        }

        public void UpdateOutput(Quest quest)
        {
            if (quest.VoteCounter.VoteStorage.Count > 0)
            {
                TallyResults = textResultsProvider.BuildOutput(quest);
            }
        }

        public void ClearTallyResults()
        {
            TallyResults = string.Empty;
        }
        #endregion

        #region Support Methods
        /// <summary>
        /// Use the forum reader to read the posts from the quest.
        /// Posts are stored in the quest.
        /// </summary>
        /// <param name="quest">The quest to read.</param>
        /// <param name="cancellationToken">Cancellation token</param>
        private async Task ReadPostsFromQuestAsync(Quest quest, CancellationToken cancellationToken)
        {
            using var forumReader = serviceProvider.GetRequiredService<ForumReader>();

            try
            {
                forumReader.StatusChanged += ForumReader_StatusChanged;

                var (threadTitles, posts) = await forumReader.ReadQuestAsync(quest, cancellationToken)
                                                             .ConfigureAwait(false);

                quest.VoteCounter.SetThreadTitles(threadTitles);
                quest.VoteCounter.AddPosts(posts);
            }
            finally
            {
                forumReader.StatusChanged -= ForumReader_StatusChanged;
            }
        }

        /// <summary>
        /// Keep watch for any status messasges from the forum reader, and add them
        /// to the TallyResults string so that they can be displayed in the UI.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e">Contains the text to be added to the output.</param>
        private void ForumReader_StatusChanged(object? sender, MessageEventArgs e)
        {
            if (!string.IsNullOrEmpty(e.Message))
            {
                TallyResults += e.Message;
            }
        }

        private void ConstructVotesFromPosts(Quest quest)
        {
            if (quest.VoteCounter.Posts.Count > 0)
            {
                quest.VoteCounter.Reset();

                PreprocessPosts(quest);
                ProcessPosts(quest);
            }
        }
        #endregion

        /// <summary>
        /// Cancel any functions running under the above RunWithTallyFlag functions
        /// </summary>
        [Obsolete]
        public void Cancel()
        {
        }


        public IDictionary<string, VoteLineBlock> PreprocessPosts(Quest quest)
        {
            foreach (var post in quest.VoteCounter.Posts)
            {
                // Reset the processed state of all the posts.
                post.Processed = false;
                post.ForceProcess = false;
                post.WorkingVoteComplete = false;
                post.WorkingVote.Clear();
                quest.VoteCounter.AddReferenceVoter(post.Origin);
            }

            List<(bool asBlocks, Func<IEnumerable<VoteLine>, (bool isPlan, bool isImplicit, string planName)> isPlanFunction)> planProcesses =
                [
                    (asBlocks: true, isPlanFunction: VoteBlocks.IsBlockAProposedPlan),
                    (asBlocks: true, isPlanFunction: VoteBlocks.IsBlockAnExplicitPlan),
                    (asBlocks: false, isPlanFunction: VoteBlocks.IsBlockAnImplicitPlan),
                    (asBlocks: false, isPlanFunction: VoteBlocks.IsBlockASingleLinePlan)
                ];

            // Run the above series of preprocessing functions to extract plans from the post list.
            var allPlans = PreprocessPlans(quest.VoteCounter.Posts, quest, planProcesses, default);

            return allPlans;
        }


        /// <summary>
        /// Run the logic for the sequence of processing phases for plan examination and extraction.
        /// </summary>
        /// <param name="posts">The posts being examined for plans.</param>
        /// <param name="quest">The quest being tallied.</param>
        /// <param name="planProcesses">The list of functions to run on the posts.</param>
        /// <param name="token">The cancellation token.</param>
        /// <returns>Returns a collection of named plans, and the vote lines that comprise them.</returns>
        private Dictionary<string, VoteLineBlock> PreprocessPlans(
            IReadOnlyList<Post> posts,
            Quest quest,
            List<(bool asBlocks, Func<IEnumerable<VoteLine>, (bool isPlan, bool isImplicit, string planName)> isPlanFunction)> planProcesses,
            CancellationToken token)
        {
            Dictionary<string, VoteLineBlock> allPlans = new(StringComparer.Ordinal);

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

                        var planOrigin = post.Origin.GetPlanOrigin(normalPlanName);

                        bool added = quest.VoteCounter.AddReferencePlan(planOrigin, normalPlanContents);

                        if (added)
                        {
                            // Each new plan that gets added also needs to be run through partitioning,
                            // and have those results added as votes.
                            var planPartitions = voteConstructor.PartitionPlan(normalPlanContents, quest.PartitionMode);

                            quest.VoteCounter.AddVotes(planPartitions, planOrigin);

                            allPlans[normalPlanName] = normalPlanContents;
                        }
                    }
                }
            }

            return allPlans;
        }

        private void ProcessPosts(Quest quest)
        {
            var unprocessed = quest.VoteCounter.Posts;

            // Loop as long as there are any more to process.
            while (unprocessed.Any())
            {
                bool processedAny = false;

                foreach (var post in unprocessed)
                {
                    var filteredResults = voteConstructor.ProcessPostGetVotes(post, quest);

                    if (post.Processed)
                        processedAny = true;

                    if (filteredResults != null)
                    {
                        // Add those to the vote counter.
                        quest.VoteCounter.AddVotes(filteredResults, post.Origin);
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

            quest.VoteCounter.AddUserDefinedTasksToTaskList();

            quest.VoteCounter.RunMergeActions();
        }
    }
}
