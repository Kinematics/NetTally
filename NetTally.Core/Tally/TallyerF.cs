using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.Extensions.Logging;
using NetTally.CustomEventArgs;
using NetTally.Input.Forums.Reading;
using NetTally.Output;
using NetTally.Tally.ComponentsF.Posts;
using NetTally.Tally.ComponentsF.Votes;
using NetTally.Tally.ComponentsF.Counting;

namespace NetTally.VoteCounting
{
    /// <summary>
    /// Class that links together the various pieces of the tally system.
    /// Call this to run a tally.
    /// </summary>
    public partial class TallyerF : ObservableObject
    {
        #region Construction
        private readonly IForumReaderF forumReader;
        private readonly ITextResultsProvider textResultsProvider;
        private readonly ILogger<TallyerF> logger;

        public TallyerF(IForumReaderF forumReader,
                     ITextResultsProvider textResultsProvider,
                     ILogger<TallyerF> logger)
        {
            this.forumReader = forumReader;
            this.textResultsProvider = textResultsProvider;
            this.logger = logger;

            this.forumReader.StatusChanged += ForumReader_StatusChanged;
        }
        #endregion

        #region Current Properties
        /// <summary>
        /// The string containing the current tally progress or results.
        /// Creates a notification event if the contents change.
        /// If it changes to or from an empty string, the HasTallyResults property also changes.
        /// </summary>
        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(HasTallyResults))]
        private string tallyResults = string.Empty;

        public bool HasTallyResults => !string.IsNullOrEmpty(TallyResults);
        #endregion

        #region Public Methods
        public async Task RunTallyAsync(Quest quest, CancellationToken cancellationToken)
        {
            TallyResults = string.Empty;

            try
            {
                quest.VoteCounterF.Reset();

                await ReadPostsFromQuestAsync(quest, cancellationToken)
                     .ConfigureAwait(false);

                UpdateTally(quest);

                logger.LogInformation("Tally for quest {questName} completed.", quest.DisplayName);
            }
            finally
            {
                // Free memory used by loading pages as soon as we're done:
                GC.Collect();
            }
        }

        public void UpdateTally(Quest quest)
        {
            ConstructVotesFromPosts(quest);
            UpdateOutput(quest);
        }

        public void UpdateOutput(Quest quest)
        {
            TallyResults = textResultsProvider.BuildOutput(quest);
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
            var (threadTitles, posts) = await forumReader.ReadQuestAsync(quest, cancellationToken)
                                                         .ConfigureAwait(false);

            quest.VoteCounterF.SetThreadTitles(threadTitles);
            quest.VoteCounterF.AddPosts(posts);
        }

        private static void ConstructVotesFromPosts(Quest quest)
        {
            if (quest.VoteCounterF.HasPosts)
            {
                quest.VoteCounterF.Reset();

                PreprocessPosts(quest);
                ProcessPosts(quest);
            }
        }

        public static IDictionary<string, VoteBlockType> PreprocessPosts(Quest quest)
        {
            foreach (var post in quest.VoteCounterF.Posts)
            {
                // Reset the processed state of all the posts.
                post.Reset();
                quest.VoteCounterF.AddReferenceVoter(post.Origin);
            }

            List<(bool asBlocks,
                  Func<VoteBlockType, PlanDescriptor> isPlanFunction)> planProcesses =
                [
                    (asBlocks: true, isPlanFunction: VoteBlocks.IsBlockAProposedPlan),
                    (asBlocks: true, isPlanFunction: VoteBlocks.IsBlockAnExplicitPlan),
                    (asBlocks: false, isPlanFunction: VoteBlocks.IsBlockAnImplicitPlan),
                    (asBlocks: false, isPlanFunction: VoteBlocks.IsBlockASingleLinePlan)
                ];

            // Run the above series of preprocessing functions to extract plans from the post list.
            var allPlans = PreprocessPlans(quest.VoteCounterF.Posts, quest, planProcesses, default);

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
        private static Dictionary<string, VoteBlockType> PreprocessPlans(
            IReadOnlyList<PostToProcess> posts,
            Quest quest,
            IEnumerable<(bool asBlocks, Func<VoteBlockType, PlanDescriptor> isPlanFunction)> planProcesses,
            CancellationToken token)
        {
            Dictionary<string, VoteBlockType> allPlans = new(StringComparer.Ordinal);

            foreach (var (asBlocks, isPlanFunction) in planProcesses)
            {
                token.ThrowIfCancellationRequested();

                foreach (var post in posts)
                {
                    var plans = VoteConstructor.PreprocessPostGetPlans(
                        quest, post.Origin.Author, isPlanFunction, VoteBlocks.GetBlocks(post.VoteLines));

                    foreach (var (planName, planContent) in plans)
                    {
                        // Convert "Base/Proposed Plan" to "Plan" before saving.
                        // Set to an undefined marker.
                        var normalized = quest.VoteCounterF.NormalizePlan(planName, planContent);

                        if (!normalized.HasValue) continue;

                        var (normalPlanName, normalPlanContents) = normalized.Value;

                        var planOrigin = Origin.CreatePlanOrigin(post.Origin, normalPlanName);

                        if (planOrigin == null) continue;

                        if (quest.VoteCounterF.AddReferencePlan(planOrigin, normalPlanContents))
                        {
                            // Each new plan that gets added also needs to be run through partitioning,
                            // and have those results added as votes.
                            var planPartitions = VoteConstructor.PartitionPlan(normalPlanContents, quest.PartitionMode);

                            quest.VoteCounterF.AddVotes(planPartitions, planOrigin);

                            allPlans[normalPlanName] = normalPlanContents;
                        }
                    }
                }
            }

            return allPlans;
        }

        private static void ProcessPosts(Quest quest)
        {
            RunProcessing(quest, quest.VoteCounterF.Posts);

            quest.VoteCounterF.AddUserDefinedTasksToTaskList();

            quest.VoteCounterF.RunMergeActions();


            static void RunProcessing(Quest quest, List<PostToProcess> postsToProcess)
            {
                // Loop as long as there are any more to process.
                while (postsToProcess.Count != 0)
                {
                    if (TryProcessPosts(quest, postsToProcess, out var unprocessed))
                    {
                        // If any posts were processed, replace the list with any
                        // remaining posts that are unprocessed.
                        postsToProcess = unprocessed;
                    }
                    else
                    {
                        // If none got processed, set the ForceProcess flag on them
                        // to avoid pending FutureReference waits.
                        postsToProcess.ForEach(p => p.ForceProcess = true);
                    }
                }
            }

            static bool TryProcessPosts(Quest quest, List<PostToProcess> postsToProcess, out List<PostToProcess> unprocessed)
            {
                unprocessed = [];

                foreach (var post in postsToProcess)
                {
                    if (VoteConstructor.TryProcessPostGetVotes(post, quest, out var votes))
                    {
                        quest.VoteCounterF.AddVotes(votes, post.Origin);
                    }
                    else
                    {
                        unprocessed.Add(post);
                    }
                }

                return unprocessed.Count < postsToProcess.Count;
            }
        }
        #endregion

        #region Events
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
        #endregion Events
    }
}
