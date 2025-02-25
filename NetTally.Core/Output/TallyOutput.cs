using System.Globalization;
using System.Text;
using Microsoft.Extensions.Options;
using NetTally.Configure;
using NetTally.Enums;
using NetTally.Input.Forums.ForumAdapters;
using NetTally.Product;
using NetTally.Tally.Components.Counting;
using NetTally.Tally.Components.Posts;
using NetTally.Tally.Components.RankCounting;
using NetTally.Tally.Components.Storage;
using NetTally.Tally.Components.Votes;
using NetTally.Utility;

namespace NetTally.Output
{
    public class TallyOutput(
        ForumAdapterFactory forumAdapterFactory,
        IOptions<GlobalSettings> globalSettings) : ITextResultsProvider
    {
        #region Private fields
        private readonly GlobalSettings globalSettings = globalSettings.Value;
        private readonly ForumAdapterFactory forumAdapterFactory = forumAdapterFactory;

        private Quest quest = null!;
        private DisplayMode displayMode;
        private IVoteCounter voteCounter = null!;
        private IRankVoteCounter rankVoteCounter = null!;

        private readonly StringBuilder sb = new();

        /// <summary>
        /// Gets the line break text from the quest's forum adapter, since some
        /// can show hard rules, and some need to just use manual text.
        /// </summary>
        private string lineBreak = string.Empty;

        /// <summary>
        /// Get the double line break.  There are no alternate versions right now.
        /// </summary>
        private readonly string doubleLineBreak = "<==========================================================>";

        const string NoTask = "【NONE】";
        #endregion

        #region Delegates
        delegate void ConstructOutputFn(
            VotesGroupedByTaskF votesInTask,
            IEnumerable<CompactVoteType> compactVotesInTask,
            IEnumerable<Origin> allVoters);

        #endregion Delegates

        #region Public functions
        /// <summary>
        /// Public function to generate output for the VoteCounter results.
        /// </summary>
        /// <param name="quest">The quest we're building the output for.</param>
        /// <returns>The full quest tally results string.</returns>
        public string BuildOutput(Quest quest)
        {
            InitializeBuild(quest);

            BuildGlobal();

            return sb.ToString();
        }
        #endregion

        #region Setup for generating output

        private void InitializeBuild(Quest quest)
        {
            this.quest = quest;
            displayMode = quest.DisplayMode;
            voteCounter = quest.VoteCounter;
            rankVoteCounter = RankVoteCounterFactory.CreateRankVoteCounter(globalSettings.RankVoteCounterMethod);

            var forumAdapter = forumAdapterFactory.CreateForumAdapter(quest.ForumType, quest.ThreadUri);
            lineBreak = forumAdapter.GetDefaultLineBreak(quest.ThreadUri);

            sb.Clear();
        }

        /// <summary>
        /// General construction.  Add the header and any vote output.
        /// Surround by spoiler tags if requested by the display mode.
        /// </summary>
        private void BuildGlobal()
        {
            var voteGroupings = GetVoteGroupings();

            if (voteCounter.Titles.Count == 0)
                return;

            using (new Spoiler(sb, "Tally Results", displayMode == DisplayMode.SpoilerAll || globalSettings.GlobalSpoilers))
            {
                AddHeader();

                if (voteGroupings[MarkerType.Rank].Count > 0)
                {
                    ConstructOutput(voteGroupings[MarkerType.Rank], ConstructRankedOutput);
                    AddDoubleLineBreak();
                }
                if (voteGroupings[MarkerType.Score].Count > 0)
                {
                    ConstructOutput(voteGroupings[MarkerType.Score], ConstructScoredOutput);
                    AddDoubleLineBreak();
                }
                if (voteGroupings[MarkerType.Approval].Count > 0)
                {
                    ConstructOutput(voteGroupings[MarkerType.Approval], ConstructApprovedOutput);
                    AddDoubleLineBreak();
                }
                ConstructOutput(voteGroupings[MarkerType.Vote], ConstructNormalOutput);

                AddTotalVoters();
            }
        }

        /// <summary>
        /// Collect all recorded votes from the VoteCounter into groups based on what
        /// type of vote each one was represented by (via MarkerType).
        /// Rank, Score, and Approval types are gathered when most of the votes are of
        /// that type.  Vote types are gathered when any votes are of that type.
        /// </summary>
        /// <returns>Returns the grouped collection.</returns>
        private Dictionary<MarkerType, VoteStorage> GetVoteGroupings()
        {
            Dictionary<MarkerType, VoteStorage> group =
                new()
                {
                    [MarkerType.Rank] = [],
                    [MarkerType.Score] = [],
                    [MarkerType.Approval] = [],
                    [MarkerType.Vote] = []
                };

            MarkerType[] markers = [MarkerType.Rank, MarkerType.Score, MarkerType.Approval];

            var allVotes = voteCounter.VoteStorage.GetAllVotes();

            foreach (var vote in allVotes)
            {
                // If the vote category is Rank/Score/Approval, it already has most
                // votes in that category.  Add it to our group.
                if (markers.Contains(vote.Category))
                {
                    group[vote.Category].Add(vote, voteCounter.VoteStorage[vote]);
                }

                // If the vote category if Vote, or if there are any voters who used
                // standard voting, put it in our Vote group.
                if (vote.Category == MarkerType.Vote ||
                    voteCounter.VoteStorage[vote].Any(v => v.Value.Category == MarkerType.Vote))
                {
                    group[MarkerType.Vote].Add(vote, voteCounter.VoteStorage[vote]);
                }
            }

            return group;
        }
        #endregion

        #region Header and Footer Construction
        /// <summary>
        /// Add the header indicating the title of the thread that was tallied,
        /// and the marker that this is a tally result (along with the program version number).
        /// </summary>
        private void AddHeader()
        {
            sb.Append("[b]Vote Tally");
            if (globalSettings.DebugMode)
                sb.Append(" (DEBUG)");
            sb.Append("[/b] : ");

            foreach (var title in voteCounter.Titles)
            {
                sb.AppendLine(title);
            }

            sb.Append("[color=transparent]##### ");
            sb.Append(ProductInfo.Name);
            sb.Append(' ');
            sb.Append(ProductInfo.Version);
            sb.AppendLine("[/color]");

            if (quest.UseCustomUsernameFilters && !string.IsNullOrEmpty(quest.CustomUsernameFilters))
            {
                sb.Append("[color=transparent]Username Filters: ");
                sb.Append(quest.CustomUsernameFilters);
                sb.AppendLine("[/color]");
            }

            if (quest.UseCustomPostFilters && !string.IsNullOrEmpty(quest.CustomPostFilters))
            {
                sb.Append("[color=transparent]Post Filters: ");
                sb.Append(quest.CustomPostFilters);
                sb.AppendLine("[/color]");
            }

            sb.AppendLine();
        }

        /// <summary>
        /// Add the total number of voters to the output.
        /// </summary>
        private void AddTotalVoters()
        {
            int voterCount = voteCounter.GetTotalVoterCount();

            if (voterCount > 0)
            {
                if (displayMode == DisplayMode.Compact || displayMode == DisplayMode.CompactNoVoters)
                    sb.AppendLine();

                AddLineBreak();

                sb.Append("Total No. of Voters: ");
                sb.Append(voterCount);
                sb.AppendLine();
                sb.AppendLine();
            }
        }
        #endregion

        #region General construction of output
        /// <summary>
        /// Wrapper construction function to handle grouping votes by task,
        /// and then pass them on to the specific construction methods.
        /// </summary>
        /// <param name="votes">Votes to be tallied.</param>
        /// <param name="marker">Type of construction being done.</param>
        private void ConstructOutput(VoteStorage votes, ConstructOutputFn constructOutputFn)
        {
            var groupByTask = GetVotesGroupedByTask(votes);

            bool firstTask = true;

            foreach (var task in groupByTask)
            {
                if (task.Any())
                {
                    if (!firstTask)
                    {
                        AddLineBreak();
                    }

                    firstTask = false;

                    AddTaskInfo(task);

                    if (displayMode == DisplayMode.VoterSummary)
                    {
                        ConstructVoterSummary(task);
                        continue;
                    }

                    IEnumerable<CompactVoteType> compactTask = [];

                    if (displayMode == DisplayMode.Compact || displayMode == DisplayMode.CompactNoVoters)
                        compactTask = CompactVote.GetCompactVotes(task);

                    var allVoters = GetAllVotersInTask(task);

                    constructOutputFn(task, compactTask, allVoters);

                    sb.AppendLine();
                }
            }

            /// <summary>
            /// Function to wrap the logic of grouping votes by task.
            /// </summary>
            /// <param name="votes">The original vote set.</param>
            /// <returns>Returns the votes grouped by task.</returns>
            IEnumerable<VotesGroupedByTaskF>
                GetVotesGroupedByTask(VoteStorage votes)
            {
                var groupByTask = votes
                    .GroupBy(a => a.Key.Task, VoteTaskComparer.Instance)
                    //.OrderBy(a => a.Key, VoteTaskComparer.Instance)
                    .OrderBy(a => voteCounter.TaskListIndex(a.Key));

                return groupByTask;
            }
        }

        private static IEnumerable<Origin> GetAllVotersInTask(VotesGroupedByTaskF task)
        {
            return task
                .SelectMany(t => t.Value)
                .Select(u => u.Key)
                .Where(v => v is UserOrigin)
                .GroupBy(v => v, OriginNameComparer.Instance)
                .Select(g => g.MaxBy(v => v.PostId, PostIdComparer.Instance)!)
                .OrderBy(v => v, OriginNameComparer.Instance);
        }

        /// <summary>
        /// Display all voters for a task in a summary fashion.
        /// </summary>
        /// <param name="task">The task being displayed.</param>
        private void ConstructVoterSummary(VotesGroupedByTaskF task)
        {
            GetAllVotersInTask(task)
                .Distinct(OriginNameComparer.Instance)
                .ToList()
                .ForEach(v => AddVoter(v));

            sb.AppendLine();
            sb.AppendLine();
        }

        /// <summary>
        /// Construct vote output for standard votes, per task.
        /// </summary>
        /// <param name="votesInTask">The votes made for a given task.</param>
        /// <param name="compactVotesInTask">All the compact votes.</param>
        /// <param name="allVoters">All the voters</param>
        private void ConstructNormalOutput(
            VotesGroupedByTaskF votesInTask,
            IEnumerable<CompactVoteType> compactVotesInTask,
            IEnumerable<Origin> allVoters)
        {
            // Normal votes, either standard or compact
            if (displayMode != DisplayMode.Compact && displayMode != DisplayMode.CompactNoVoters)
            {
                ConstructStandardNormalOutput(votesInTask);
            }
            else
            {
                ConstructCompactNormalOutput(compactVotesInTask);
            }

            // Construct standard output
            void ConstructStandardNormalOutput(VotesGroupedByTaskF votesInTask)
            {
                votesInTask
                    .Select(v => new { vote = v, supportCount = v.Value.GetSupportCount() })
                    .OrderByDescending(a => a.supportCount)
                    .ThenBy(a => a.vote.Key.Lines[0].Content, VoteContentComparer.Instance)
                    .ToList()
                    .ForEach(a => ConstructStandardVote(a.vote, a.supportCount));

                // Handle each vote
                void ConstructStandardVote(VoteStorageEntryF entry, int supportCount)
                {
                    int voterCount = entry.Value.GetNonRankUserCount();
                    if (voterCount != supportCount)
                    {
                        AddStandardVoteSupport(supportCount);
                    }
                    AddStandardVoteDisplay(entry, supportCount);
                    AddVoterCount(voterCount);
                    AddNonRankVoters(entry.Value);

                    sb.AppendLine();
                }
            }

            // Construct compact output
            void ConstructCompactNormalOutput(IEnumerable<CompactVoteType> compactVotesInTask)
            {
                compactVotesInTask
                    .Select(v => new { vote = v, supportCount = VoterAnalysis.GetSupportCount(v.Voters) })
                    .OrderByDescending(a => a.supportCount)
                    .ThenBy(a => a.vote.Line.Content, VoteContentComparer.Instance)
                    .ToList()
                    .ForEach(a => ConstructCompactVote(a.vote, a.supportCount));

                // Handle each vote
                void ConstructCompactVote(CompactVoteType entry, int supportCount)
                {
                    var flattened = CompactVoteTransform.Flatten(entry);

                    foreach (var vote in flattened)
                    {
                        sb.AppendLine(CompactVoteDisplay.ToOutputString(vote, supportCount.ToString()));

                        if (displayMode != DisplayMode.CompactNoVoters)
                        {
                            AddCompactNormalVoteVoters(vote);
                        }
                    }

                    if (!(quest.PartitionMode == PartitionMode.ByLine || quest.PartitionMode == PartitionMode.ByLineTask))
                        sb.AppendLine();
                }
            }
        }


        /// <summary>
        /// Construct vote output per task for scored votes.
        /// </summary>
        /// <param name="votesInTask">The votes made for a given task.</param>
        /// <param name="compactVotesInTask">All the compact votes.</param>
        /// <param name="allVoters">All the voters</param>
        private void ConstructScoredOutput(
            VotesGroupedByTaskF votesInTask,
            IEnumerable<CompactVoteType> compactVotesInTask,
            IEnumerable<Origin> _)
        {
            // Scored votes, either standard or compact
            if (displayMode != DisplayMode.Compact && displayMode != DisplayMode.CompactNoVoters)
            {
                ConstructStandardScoredOutput(votesInTask);
            }
            else
            {
                ConstructCompactScoredOutput(compactVotesInTask);
            }

            void ConstructStandardScoredOutput(VotesGroupedByTaskF votesInTask)
            {
                votesInTask
                    .Select(v => new { vote = v, score = v.Value.GetScore() })
                    .OrderByDescending(a => a.score.lowerMargin)
                    .ThenByDescending(a => a.score.average)
                    .ThenBy(a => a.vote.Key.Lines[0].Content, VoteContentComparer.Instance)
                    .ToList()
                    .ForEach(a => ConstructStandardVote(a.vote, a.score));

                // Handle each vote
                void ConstructStandardVote(VoteStorageEntryF entry, (int score, double avg, double low) score)
                {
                    AddScoreVoteSupport(score);
                    AddScoreVoteDisplay(entry, score);
                    AddVoterCount(entry.Value.GetNonRankUserCount());
                    AddNonRankVoters(entry.Value);

                    sb.AppendLine();
                }
            }

            void ConstructCompactScoredOutput(IEnumerable<CompactVoteType> compactVotesInTask)
            {
                var orderedResults = compactVotesInTask
                    .OrderByDescending(a => VoterAnalysis.GetScore(a.Voters).lowerMargin)
                    .ThenBy(a => a.Line.Content, VoteContentComparer.Instance);

                foreach (var result in orderedResults)
                {
                    var flattened = CompactVoteTransform.Flatten(result);

                    foreach (var vote in flattened)
                    {
                        var (score, average, lowerMargin) = VoterAnalysis.GetScore(vote.Voters);
                        sb.AppendLine(CompactVoteDisplay.ToOutputString(vote, $"{score}%"));

                        if (displayMode != DisplayMode.CompactNoVoters)
                        {
                            AddCompactNormalVoteVoters(vote);
                        }
                    }

                    if (!(quest.PartitionMode == PartitionMode.ByLine || quest.PartitionMode == PartitionMode.ByLineTask))
                        sb.AppendLine();
                }
            }
        }

        /// <summary>
        /// Construct vote output per task for approval votes.
        /// </summary>
        /// <param name="votesInTask">The votes made for a given task.</param>
        /// <param name="compactVotesInTask">All the compact votes.</param>
        /// <param name="allVoters">All the voters</param>
        private void ConstructApprovedOutput(
            VotesGroupedByTaskF votesInTask,
            IEnumerable<CompactVoteType> compactVotesInTask,
            IEnumerable<Origin> allVoters)
        {
            // Scored votes, either standard or compact
            if (displayMode != DisplayMode.Compact && displayMode != DisplayMode.CompactNoVoters)
            {
                ConstructStandardApprovalOutput(votesInTask);
            }
            else
            {
                ConstructCompactApprovalOutput(compactVotesInTask);
            }

            void ConstructStandardApprovalOutput(VotesGroupedByTaskF votesInTask)
            {
                votesInTask
                    .Select(v => new { vote = v, support = v.Value.GetApproval() })
                    .OrderByDescending(a => a.support.positive)
                    .ThenBy(a => a.support.negative)
                    .ThenBy(a => a.vote.Key.First().Content, VoteContentComparer.Instance)
                    .ToList()
                    .ForEach(a => ConstructStandardVote(a.vote, a.support));

                void ConstructStandardVote(VoteStorageEntryF entry, (int pos, int neg) support)
                {
                    AddApprovalVoteSupport(support);
                    AddApprovalVoteDisplay(entry, support);
                    AddVoterCount(entry.Value.GetNonRankUserCount());
                    AddNonRankVoters(entry.Value);

                    sb.AppendLine();
                }
            }

            void ConstructCompactApprovalOutput(IEnumerable<CompactVoteType> compactVotesInTask)
            {
                compactVotesInTask
                    .Select(v => new { vote = v, support = VoterAnalysis.GetApproval(v.Voters) })
                    .OrderByDescending(a => a.support.positive)
                    .ThenBy(a => a.support.negative)
                    .ThenBy(a => a.vote.Line.Content, VoteContentComparer.Instance)
                    .ToList()
                    .ForEach(a => ConstructCompactVote(a.vote));


                void ConstructCompactVote(CompactVoteType entry)
                {
                    var flattened = CompactVoteTransform.Flatten(entry);

                    foreach (var vote in flattened)
                    {
                        var (positive, negative) = VoterAnalysis.GetApproval(vote.Voters);
                        sb.AppendLine(CompactVoteDisplay.ToOutputString(vote, $"+{positive}/-{negative}"));

                        if (displayMode != DisplayMode.CompactNoVoters)
                        {
                            AddCompactNormalVoteVoters(vote);
                        }
                    }

                    if (!(quest.PartitionMode == PartitionMode.ByLine || quest.PartitionMode == PartitionMode.ByLineTask))
                        sb.AppendLine();
                }
            }
        }

        /// <summary>
        /// Construct vote output per task for ranked votes.
        /// </summary>
        /// <param name="votesInTask">The votes made for a given task.</param>
        /// <param name="compactVotesInTask">All the compact votes.</param>
        /// <param name="allVoters">All the voters</param>
        private void ConstructRankedOutput(VotesGroupedByTaskF votesInTask, IEnumerable<CompactVoteType> _,
            IEnumerable<Origin> allVoters)
        {
            var taskVotes = VoteStorage.CopyFrom(votesInTask.ToDictionary(a => a.Key, b => b.Value));
            var results = rankVoteCounter.CountVotesForTask(taskVotes);

            bool multiline = results.Any(a => a.vote.Key.LineCount > 1);

            foreach (var (ranking, vote) in results)
            {
                AddRankVoteSupport(ranking);
                AddRankVoteDisplay(vote, ranking);
                AddVoterCount(vote.Value.GetUserCount());
                AddRankedVoters(vote.Value, allVoters);

                if (displayMode != DisplayMode.CompactNoVoters || multiline)
                    sb.AppendLine();
            }
        }
        #endregion

        #region Components for handling individual additions to the display.
        private void AddTaskInfo(VotesGroupedByTaskF task)
        {
            string taskName = VoteTaskComparer.Instance.Equals(task.Key, VoteTask.Empty)
                ? NoTask
                : task.Key.Name;

            AddTaskLabel(taskName);
            AddTaskVoterCount(task);
            sb.AppendLine();
        }

        /// <summary>
        /// Add a label for the specified task.
        /// </summary>
        /// <param name="taskName">The name of the task.</param>
        private void AddTaskLabel(string taskName)
        {
            if (taskName.Length > 0)
            {
                sb.Append("[b]Task: ");
                sb.Append(taskName);
                sb.AppendLine("[/b]");
            }
        }

        private void AddTaskVoterCount(VotesGroupedByTaskF task)
        {
            var voters = GetAllVotersInTask(task);

            sb.Append("— Voters: ");
            sb.Append(voters.Count());
            sb.AppendLine();
        }

        /// <summary>
        /// Add a line to indicate the degree of support this vote has.
        /// This may or may not be the same as the number of basic votes.
        /// </summary>
        /// <param name="supportCount">The number of supporters of this vote.</param>
        private void AddStandardVoteSupport(int supportCount)
        {
            if (displayMode == DisplayMode.Compact || displayMode == DisplayMode.CompactNoVoters)
                return;

            sb.Append("[b]Support: ");
            sb.Append(supportCount);
            sb.AppendLine("[/b]");
        }

        /// <summary>
        /// Add a line to indicate how much support this vote has, using approval balances.
        /// </summary>
        /// <param name="support">The amount of positive and negative support.</param>
        private void AddApprovalVoteSupport((int positive, int negative) support)
        {
            if (displayMode == DisplayMode.Compact || displayMode == DisplayMode.CompactNoVoters)
                return;

            sb.Append("[b]Support: ");
            sb.Append('+').Append(support.positive).Append("/-").Append(support.negative);
            sb.AppendLine("[/b]");
        }

        /// <summary>
        /// Add a line to show the score this vote achieved.
        /// </summary>
        /// <param name="score">The score for the vote.</param>
        private void AddScoreVoteSupport((int score, double average, double lowerMargin) score)
        {
            if (displayMode == DisplayMode.Compact || displayMode == DisplayMode.CompactNoVoters)
                return;

            sb.Append("[b]Score: ");
            sb.Append(score.score).Append('%');
            if (globalSettings.DebugMode)
            {
                sb.Append(" (")
                  .AppendFormat(CultureInfo.CurrentCulture, "{0:F4}", score.lowerMargin)
                  .Append(')');
            }
            sb.AppendLine("[/b]");
        }

        /// <summary>
        /// Add a line to show the ranking of this vote relative to others in the same task.
        /// </summary>
        /// <param name="rank">The rank the vote achieved.</param>
        private void AddRankVoteSupport((int rank, double rankScore) ranking)
        {
            if (displayMode == DisplayMode.Compact || displayMode == DisplayMode.CompactNoVoters)
                return;

            sb.Append("[b]Ranking: ");
            sb.Append('#').Append(ranking.rank);
            if (globalSettings.DebugMode)
            {
                sb.Append(" (")
                  .AppendFormat(CultureInfo.CurrentCulture, "{0:F6}", ranking.rankScore)
                  .Append(')');
            }
            sb.AppendLine("[/b]");
        }

        /// <summary>
        /// Print the standard vote (<seealso cref="MarkerType.Vote"/>), with appropriate leading marker depending on whether the
        /// display is in compact mode.
        /// </summary>
        /// <param name="vote">The vote to display.</param>
        /// <param name="supportCount">The support the vote has.</param>
        private void AddStandardVoteDisplay(VoteStorageEntryF vote, int supportCount)
        {
            if (displayMode == DisplayMode.Compact || displayMode == DisplayMode.CompactNoVoters)
                sb.AppendLine(VoteBlockDisplay.ToOutputString(vote.Key, supportCount.ToString()));
            else
                sb.AppendLine(VoteBlockDisplay.ToOutputString(vote.Key, Strings.VoteMarker, Strings.VoteMarker));
        }

        /// <summary>
        /// Print the approval vote (<seealso cref="MarkerType.Approval"/>), with appropriate leading marker depending on whether the
        /// display is in compact mode.
        /// </summary>
        /// <param name="vote">The vote to display.</param>
        /// <param name="approval">The approval the vote has.</param>
        private void AddApprovalVoteDisplay(VoteStorageEntryF vote, (int positive, int negative) approval)
        {
            if (displayMode == DisplayMode.Compact || displayMode == DisplayMode.CompactNoVoters)
                sb.AppendLine(VoteBlockDisplay.ToOutputString(vote.Key, $"+{approval.positive}/-{approval.negative}"));
            else
                sb.AppendLine(VoteBlockDisplay.ToOutputString(vote.Key, Strings.ApprovalMarker));
        }

        /// <summary>
        /// Print the score vote (<seealso cref="MarkerType.Score"/>), with appropriate leading marker depending on whether the
        /// display is in compact mode.
        /// </summary>
        /// <param name="vote">The vote to display.</param>
        /// <param name="approval">The score the vote has.</param>
        private void AddScoreVoteDisplay(VoteStorageEntryF vote,
            (int score, double average, double lowerMargin) score)
        {
            if (displayMode == DisplayMode.Compact || displayMode == DisplayMode.CompactNoVoters)
                sb.AppendLine(VoteBlockDisplay.ToOutputString(vote.Key, $"{score.score}%"));
            else
                sb.AppendLine(VoteBlockDisplay.ToOutputString(vote.Key, Strings.ScoreMarker));
        }

        /// <summary>
        /// Print the rank vote (<seealso cref="MarkerType.Rank"/>), with appropriate leading marker depending on whether the
        /// display is in compact mode.
        /// </summary>
        /// <param name="vote">The vote to display.</param>
        /// <param name="approval">The rank the vote has.</param>
        private void AddRankVoteDisplay(VoteStorageEntryF vote, (int rank, double rankScore) ranking)
        {
            if (displayMode == DisplayMode.Compact || displayMode == DisplayMode.CompactNoVoters)
                sb.AppendLine(VoteBlockDisplay.ToOutputString(vote.Key, $"#{ranking.rank}"));
            else
                sb.AppendLine(VoteBlockDisplay.ToOutputString(vote.Key, Strings.RankMarker));
        }

        /// <summary>
        /// Add the list of voters who voted using non-rank methods.
        /// </summary>
        /// <param name="voters">The voters to add.</param>
        /// <param name="spoilerLabel">An optional spoiler label to use.</param>
        private void AddNonRankVoters(VoterStorage voters)
        {
            if (displayMode == DisplayMode.NormalNoVoters || displayMode == DisplayMode.CompactNoVoters)
                return;

            using (new Spoiler(sb, "Voters", displayMode != DisplayMode.Normal))
            {
                var orderedVoters = voters.GetOrderedVoterList();

                foreach (var voter in orderedVoters)
                {
                    AddVoter(voter.Key, voter.Value);
                }
            }
        }

        /// <summary>
        /// Add the list of voters who voted using non-rank methods, from a compact vote source.
        /// </summary>
        /// <param name="voters">The voters to add.</param>
        /// <param name="spoilerLabel">An optional spoiler label to use.</param>
        private void AddCompactNormalVoteVoters(CompactVoteType vote)
        {
            using (new Spoiler(sb, "Voters", display: true))
            {
                var orderedVoters = VoterAnalysis.GetOrderedVoterList(vote.Voters);

                foreach (var voter in orderedVoters)
                {
                    AddVoter(voter.Key, voter.Value);
                }
            }
        }



        /// <summary>
        /// Add the list of voters who voted using rank methods.
        /// Also displays non-rank-voting users with a non-rank vote marker.
        /// </summary>
        /// <param name="voters">List of voters.</param>
        /// <param name="spoilerLabel">Optional spoiler label.</param>
        private void AddRankedVoters(VoterStorage voters, IEnumerable<Origin> allVoters)
        {
            if (displayMode == DisplayMode.NormalNoVoters || displayMode == DisplayMode.CompactNoVoters)
                return;

            using (new Spoiler(sb, "Voters", displayMode != DisplayMode.Normal))
            {
                var orderedVoters = voters.GetOrderedRankedVoterList();

                foreach (var voter in orderedVoters)
                {
                    AddVoter(voter.Key, voter.Value, MarkerType.Rank);
                }

                var didNotRankOption = allVoters.Except(orderedVoters.Select(v => v.Key));

                foreach (var voter in didNotRankOption)
                {
                    AddVoter(voter, vote: null, MarkerType.Rank);
                }
            }
        }

        /// <summary>
        /// Add an individual voter line, with permalink.
        /// </summary>
        /// <param name="voter">The voter to add.</param>
        private void AddVoter(Origin voter, VoteBlockType? vote, MarkerType marker = MarkerType.None)
        {
            string markerToDisplay;
            if (voter is PlanOrigin)
                markerToDisplay = Strings.PlanNameMarker;
            else if (marker == MarkerType.Rank && vote is not null && vote.Marker.MarkerType != MarkerType.Rank)
                markerToDisplay = Strings.NoRankMarker;
            else if (marker == MarkerType.Rank && (vote is null))
                markerToDisplay = Strings.NonVotingMarker;
            else if (vote is not null)
                markerToDisplay = vote.Marker.MarkerSymbol;
            else
                markerToDisplay = Strings.UnknownMarker;

            AddVoter(voter, markerToDisplay);
        }

        private void AddVoter(Origin voter, string marker = "")
        {
            if (voter is PlanOrigin) sb.Append("[b]");

            sb.Append('[');
            sb.Append(marker);
            sb.Append("] ");

            if (voter is PlanOrigin) sb.Append("Plan: ");

            sb.Append("[url=\"");
            sb.Append(voter.Permalink);
            sb.Append("\"]");
            sb.Append(voter.Author.Name);
            sb.Append("[/url]");

            if (voter is PlanOrigin) sb.Append("[/b]");

            sb.AppendLine();
        }

        /// <summary>
        /// Add a line showing the number of voters.
        /// </summary>
        /// <param name="count">The count to display.</param>
        private void AddVoterCount(int count)
        {
            if (displayMode == DisplayMode.Compact || displayMode == DisplayMode.CompactNoVoters)
                return;

            sb.Append("[b]No. of Votes: ");
            sb.Append(count);
            sb.AppendLine("[/b]");
        }

        /// <summary>
        /// Add a line break between tasks.
        /// </summary>
        private void AddLineBreak()
        {
            if (displayMode == DisplayMode.Compact || displayMode == DisplayMode.CompactNoVoters)
                sb.AppendLine();

            sb.AppendLine(lineBreak);
            sb.AppendLine();
            sb.AppendLine();
        }

        /// <summary>
        /// Add a line break between marker categories.
        /// </summary>
        private void AddDoubleLineBreak()
        {
            sb.AppendLine(doubleLineBreak);
            sb.AppendLine();
        }
        #endregion
    }
}
