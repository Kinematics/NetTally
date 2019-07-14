using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using NetTally.Extensions;
using NetTally.Options;
using NetTally.Output;
using NetTally.SystemInfo;
using NetTally.Utility;
using NetTally.VoteCounting;
using NetTally.VoteCounting.RankVoteCounting.Utility;
using NetTally.Votes;

namespace NetTally.Experiment3
{
    public class TallyOutput : ITextResultsProvider
    {
        #region Constructor and private fields
        readonly IVoteCounter voteCounter;
        readonly IGeneralOutputOptions outputOptions;
        readonly VoteInfo voteInfo;
        readonly IRankVoteCounter2 rankVoteCounter;

        DisplayMode displayMode;
        IQuest quest = new Quest();

        StringBuilder sb = new StringBuilder();
        const string cancelled = "Cancelled!";

        static readonly string[] rankWinnerLabels = { "Winner", "First Runner Up", "Second Runner Up", "Third Runner Up", "Honorable Mention" };

        public TallyOutput(IVoteCounter counter, VoteInfo info, RankVoteCounterFactory factory, IGeneralOutputOptions options)
        {
            voteCounter = counter;
            voteInfo = info;
            outputOptions = options;

            rankVoteCounter = factory.CreateRankVoteCounter(options.RankVoteCounterMethod);
        }
        #endregion

        #region Public ITextResultsProvider functions
        /// <summary>
        /// Public function to initiate generating output for the information
        /// in the current Vote Counter.
        /// </summary>
        /// <param name="displayMode"></param>
        /// <param name="token"></param>
        /// <returns></returns>
        public async Task<string> BuildOutputAsync(DisplayMode displayMode, CancellationToken token)
        {
            if (voteCounter.Quest == null)
                return string.Empty;
            if (voteCounter.TallyWasCanceled)
                return cancelled;

            quest = voteCounter.Quest;
            this.displayMode = displayMode;

            sb = new StringBuilder();

            try
            {
                await Task.Run(() => BuildGlobal(token)).ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
                return cancelled;
            }

            return sb.ToString();
        }
        #endregion

        #region Setup for generating output
        /// <summary>
        /// General construction.  Add the header and any vote output.
        /// Surround by spoiler tags if requested by the display mode.
        /// </summary>
        /// <param name="token">Cancellation token so that processing can be cancelled.</param>
        private void BuildGlobal(CancellationToken token)
        {
            token.ThrowIfCancellationRequested();

            var voteGroupings = GetVoteGroupings();

            using (new Spoiler(sb, "Tally Results", displayMode == DisplayMode.SpoilerAll || outputOptions.GlobalSpoilers))
            {
                AddHeader(token);

                ConstructOutput(voteGroupings[MarkerType.Rank], MarkerType.Rank, token);
                if (voteGroupings[MarkerType.Rank].Count > 0)
                {
                    AddDoubleLineBreak();
                }
                ConstructOutput(voteGroupings[MarkerType.Score], MarkerType.Score, token);
                if (voteGroupings[MarkerType.Score].Count > 0)
                {
                    AddDoubleLineBreak();
                }
                ConstructOutput(voteGroupings[MarkerType.Approval], MarkerType.Approval, token);
                if (voteGroupings[MarkerType.Approval].Count > 0)
                {
                    AddDoubleLineBreak();
                }
                ConstructOutput(voteGroupings[MarkerType.Vote], MarkerType.Vote, token);

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
                new Dictionary<MarkerType, VoteStorage>
                {
                    [MarkerType.Rank] = new VoteStorage(),
                    [MarkerType.Score] = new VoteStorage(),
                    [MarkerType.Approval] = new VoteStorage(),
                    [MarkerType.Vote] = new VoteStorage()
                };

            MarkerType[] markers = { MarkerType.Rank, MarkerType.Score, MarkerType.Approval };

            foreach (var marker in markers)
            {
                foreach (var block in voteCounter.VoteStorage)
                {
                    // Using the default threshold for 'Most'.  Change if necessary.
                    if (block.Value.Most(v => v.Value.MarkerType == marker))
                    {
                        group[marker].Add(block.Key, block.Value);
                    }
                }
            }

            foreach (var block in voteCounter.VoteStorage)
            {
                if (block.Value.Any(v => v.Value.MarkerType == MarkerType.Vote) ||
                    (outputOptions.DisplayPlansWithNoVotes && block.Value.Any(v => v.Value.MarkerType == MarkerType.Plan)))
                {
                    group[MarkerType.Vote].Add(block.Key, block.Value);
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
        private void AddHeader(CancellationToken token)
        {
            token.ThrowIfCancellationRequested();

            if (voteCounter.Quest is null)
                return;

            sb.Append("[b]Vote Tally");
            if (outputOptions.DebugMode)
                sb.Append(" (DEBUG)");
            sb.Append("[/b] : ");
            sb.Append(voteCounter.Title);

            (int first, int last) = GetPostRange();
            if (last > 0)
                sb.Append($" [Posts: {first}-{last}]");

            sb.AppendLine();

            sb.AppendLine($"[color=transparent]##### {ProductInfo.Name} {ProductInfo.Version}[/color]");

            if (voteCounter.Quest.UseCustomUsernameFilters && !string.IsNullOrEmpty(quest.CustomUsernameFilters))
            {
                sb.AppendLine($"[color=transparent]Username Filters: {quest.CustomUsernameFilters}[/color]");
            }

            if (voteCounter.Quest.UseCustomPostFilters && !string.IsNullOrEmpty(quest.CustomPostFilters))
            {
                sb.AppendLine($"[color=transparent]Post Filters: {quest.CustomPostFilters}[/color]");
            }

            sb.AppendLine();

            /// <summary>
            /// Gets the post range of the votes that the VoteCounter tallied.
            /// Returns values of 0 if no valid posts are available.
            /// </summary>
            /// <param name="first">The first post number.</param>
            /// <param name="last">The last post number.</param>
            (int first, int last) GetPostRange()
            {
                int first = 0;
                int last = 0;

                foreach (var post in voteCounter.Posts)
                {
                    if (first == 0 || post.Origin.ThreadPostNumber < first)
                        first = post.Origin.ThreadPostNumber;
                    if (post.Origin.ThreadPostNumber > last)
                        last = post.Origin.ThreadPostNumber;
                }

                return (first, last);
            }
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
        /// <param name="token">Cancellation token.</param>
        private void ConstructOutput(VoteStorage votes, MarkerType marker, CancellationToken token)
        {
            if (votes.Count == 0)
            {
                return;
            }

            var groupByTask = GetVotesGroupedByTask(votes);

            bool firstTask = true;

            foreach (var task in groupByTask)
            {
                token.ThrowIfCancellationRequested();

                if (task.Any())
                {
                    if (!firstTask)
                    {
                        AddLineBreak();
                    }

                    firstTask = false;

                    AddTaskLabel(task.Key);

                    switch (marker)
                    {
                        case MarkerType.Vote:
                            ConstructNormalOutput(task);
                            break;
                        case MarkerType.Score:
                            ConstructScoredOutput(task);
                            break;
                        case MarkerType.Approval:
                            ConstructApprovedOutput(task);
                            break;
                        case MarkerType.Rank:
                            ConstructRankedOutput(task);
                            break;
                        default:
                            throw new ArgumentOutOfRangeException($"Unknown marker type: {marker}", nameof(marker));
                    }

                    sb.AppendLine();
                }
            }

            /// <summary>
            /// Function to wrap the logic of grouping votes by task.
            /// </summary>
            /// <param name="votes">The original vote set.</param>
            /// <returns>Returns the votes grouped by task.</returns>
            IEnumerable<IGrouping<string, KeyValuePair<VoteLineBlock, VoterStorage>>>
                GetVotesGroupedByTask(VoteStorage votes)
            {
                var groupByTask = votes.GroupBy(a => a.Key.Task).OrderBy(a => a.Key);

                groupByTask = groupByTask.OrderBy(v => voteCounter.TaskList.IndexOf(v.Key));

                return groupByTask;
            }
        }

        /// <summary>
        /// Construct vote output per task for standard votes.
        /// </summary>
        /// <param name="votesInTask">The group of votes falling under a task.</param>
        /// <param name="token">Cancellation token.</param>
        private void ConstructNormalOutput(IGrouping<string, KeyValuePair<VoteLineBlock, VoterStorage>> votesInTask)
        {
            bool multiline = votesInTask.Any(a => a.Key.Lines.Count > 1);

            var voteResults = votesInTask.Select(v => new { vote = v, supportCount = voteInfo.GetVoteVoterCount(v) });

            var orderedResults = voteResults.OrderByDescending(a => a.supportCount).ThenBy(a => a.vote.Key.First().CleanContent);


            foreach (var result in orderedResults)
            {
                AddStandardVoteSupport(result.supportCount);
                AddStandardVoteDisplay(result.vote, result.supportCount);
                AddVoterCount(voteInfo.GetStandardVotersCount(result.vote));
                AddStandardVoters(result.vote.Value);

                if (displayMode != DisplayMode.CompactNoVoters || multiline)
                    sb.AppendLine();
            }
        }

        /// <summary>
        /// Construct vote output per task for scored votes.
        /// </summary>
        /// <param name="votesInTask">The group of votes falling under a task.</param>
        /// <param name="token">Cancellation token.</param>
        private void ConstructScoredOutput(IGrouping<string, KeyValuePair<VoteLineBlock, VoterStorage>> votesInTask)
        {
            bool multiline = votesInTask.Any(a => a.Key.Lines.Count > 1);

            var voteResults = votesInTask.Select(v => new { vote = v, score = voteInfo.GetVoteScoreResult(v) });

            var orderedResults = voteResults.OrderByDescending(a => a.score.limitScore).ThenBy(a => a.vote.Key.First().CleanContent);

            foreach (var result in orderedResults)
            {
                AddScoreVoteSupport(result.score);
                AddScoreVoteDisplay(result.vote, result.score);
                AddVoterCount(voteInfo.GetStandardVotersCount(result.vote));
                AddStandardVoters(result.vote.Value);

                if (displayMode != DisplayMode.CompactNoVoters || multiline)
                    sb.AppendLine();
            }
        }

        /// <summary>
        /// Construct vote output per task for approval votes.
        /// </summary>
        /// <param name="votesInTask">The group of votes falling under a task.</param>
        /// <param name="token">Cancellation token.</param>
        private void ConstructApprovedOutput(IGrouping<string, KeyValuePair<VoteLineBlock, VoterStorage>> votesInTask)
        {
            bool multiline = votesInTask.Any(a => a.Key.Lines.Count > 1);

            var voteResults = votesInTask.Select(v => new { vote = v, support = voteInfo.GetVoteApprovalResult(v) });

            var orderedResults = voteResults.OrderByDescending(a => a.support).ThenBy(a => a.vote.Key.First().CleanContent);

            foreach (var result in orderedResults)
            {
                AddApprovalVoteSupport(result.support);
                AddApprovalVoteDisplay(result.vote, result.support);
                AddVoterCount(voteInfo.GetStandardVotersCount(result.vote));
                AddStandardVoters(result.vote.Value);

                if (displayMode != DisplayMode.CompactNoVoters || multiline)
                    sb.AppendLine();
            }
        }

        /// <summary>
        /// Construct vote output per task for ranked votes.
        /// </summary>
        /// <param name="votesInTask">The group of votes falling under a task.</param>
        /// <param name="token">Cancellation token.</param>
        private void ConstructRankedOutput(IGrouping<string, KeyValuePair<VoteLineBlock, VoterStorage>> votesInTask)
        {
            var taskVotes = new VoteStorage(votesInTask.ToDictionary(a => a.Key, b => b.Value));
            var results = rankVoteCounter.CountVotesForTask(taskVotes);

            bool multiline = results.Any(a => a.vote.Key.Lines.Count> 1);

            foreach (var (ranking, vote) in results)
            {
                AddRankVoteSupport(ranking);
                AddRankVoteDisplay(vote, ranking);
                AddVoterCount(voteInfo.GetAllVotersCount(vote));
                AddRankedVoters(vote.Value);

                if (displayMode != DisplayMode.CompactNoVoters || multiline)
                    sb.AppendLine();
            }
        }
        #endregion

        #region Components for handling individual additions to the display.
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
                sb.AppendLine();
            }
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
            sb.Append($"+{support.positive}/-{support.negative}");
            sb.AppendLine("[/b]");
        }

        /// <summary>
        /// Add a line to show the score this vote achieved.
        /// </summary>
        /// <param name="score">The score for the vote.</param>
        private void AddScoreVoteSupport((int simpleScore, double limitScore) score)
        {
            if (displayMode == DisplayMode.Compact || displayMode == DisplayMode.CompactNoVoters)
                return;

            sb.Append("[b]Score: ");
            sb.Append($"{score.simpleScore}%");
            if (outputOptions.DebugMode)
            {
                sb.Append($" ({score.limitScore:F6})");
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
            sb.Append($"#{ranking.rank}");
            if (outputOptions.DebugMode)
            {
                sb.Append($" ({ranking.rankScore:F6})");
            }
            sb.AppendLine("[/b]");
        }

        /// <summary>
        /// Print the standard vote (<seealso cref="MarkerType.Vote"/>), with appropriate leading marker depending on whether the
        /// display is in compact mode.
        /// </summary>
        /// <param name="vote">The vote to display.</param>
        /// <param name="supportCount">The support the vote has.</param>
        private void AddStandardVoteDisplay(KeyValuePair<VoteLineBlock, VoterStorage> vote, int supportCount)
        {
            if (displayMode == DisplayMode.Compact || displayMode == DisplayMode.CompactNoVoters)
                sb.AppendLine(vote.Key.ToOutputString(supportCount.ToString()));
            else
                sb.AppendLine(vote.Key.ToOutputString("X"));
        }

        /// <summary>
        /// Print the approval vote (<seealso cref="MarkerType.Approval"/>), with appropriate leading marker depending on whether the
        /// display is in compact mode.
        /// </summary>
        /// <param name="vote">The vote to display.</param>
        /// <param name="approval">The approval the vote has.</param>
        private void AddApprovalVoteDisplay(KeyValuePair<VoteLineBlock, VoterStorage> vote, (int positive, int negative) approval)
        {
            if (displayMode == DisplayMode.Compact || displayMode == DisplayMode.CompactNoVoters)
                sb.AppendLine(vote.Key.ToOutputString($"+{approval.positive}/-{approval.negative}"));
            else
                sb.AppendLine(vote.Key.ToOutputString("±"));
        }

        /// <summary>
        /// Print the score vote (<seealso cref="MarkerType.Score"/>), with appropriate leading marker depending on whether the
        /// display is in compact mode.
        /// </summary>
        /// <param name="vote">The vote to display.</param>
        /// <param name="approval">The score the vote has.</param>
        private void AddScoreVoteDisplay(KeyValuePair<VoteLineBlock, VoterStorage> vote, (int simpleScore, double limitScore) score)
        {
            if (displayMode == DisplayMode.Compact || displayMode == DisplayMode.CompactNoVoters)
                sb.AppendLine(vote.Key.ToOutputString($"{score.simpleScore}%"));
            else
                sb.AppendLine(vote.Key.ToOutputString("%"));
        }

        /// <summary>
        /// Print the rank vote (<seealso cref="MarkerType.Rank"/>), with appropriate leading marker depending on whether the
        /// display is in compact mode.
        /// </summary>
        /// <param name="vote">The vote to display.</param>
        /// <param name="approval">The rank the vote has.</param>
        private void AddRankVoteDisplay(KeyValuePair<VoteLineBlock, VoterStorage> vote, (int rank, double rankScore) ranking)
        {
            if (displayMode == DisplayMode.Compact || displayMode == DisplayMode.CompactNoVoters)
                sb.AppendLine(vote.Key.ToOutputString($"#{ranking.rank}"));
            else
                sb.AppendLine(vote.Key.ToOutputString("#"));
        }

        /// <summary>
        /// Add the list of voters who voted using non-rank methods.
        /// </summary>
        /// <param name="voters">The voters to add.</param>
        /// <param name="spoilerLabel">An optional spoiler label to use.</param>
        private void AddStandardVoters(VoterStorage voters, string spoilerLabel = "Voters")
        {
            // LATER: Is spoilerLabel ever used?
            if (displayMode == DisplayMode.NormalNoVoters || displayMode == DisplayMode.CompactNoVoters)
                return;

            using (new Spoiler(sb, spoilerLabel, displayMode != DisplayMode.Normal))
            {
                var orderedVoters = voteInfo.GetOrderedStandardVoterList(voters);

                foreach (var voter in orderedVoters)
                {
                    AddVoter(voter);
                }
            }
        }

        /// <summary>
        /// Add the list of voters who voted using rank methods.
        /// Also displays non-rank-voting users with a non-rank vote marker.
        /// </summary>
        /// <param name="voters">List of voters.</param>
        /// <param name="spoilerLabel">Optional spoiler label.</param>
        private void AddRankedVoters(VoterStorage voters, string spoilerLabel = "Voters")
        {
            if (displayMode == DisplayMode.NormalNoVoters || displayMode == DisplayMode.CompactNoVoters)
                return;

            using (new Spoiler(sb, spoilerLabel, displayMode != DisplayMode.Normal))
            {
                var orderedVoters = voteInfo.GetOrderedRankedVoterList(voters);

                foreach (var voter in orderedVoters)
                {
                    AddVoter(voter, MarkerType.Rank);
                }
            }
        }

        /// <summary>
        /// Add an individual voter line, with permalink.
        /// </summary>
        /// <param name="voter">The voter to add.</param>
        private void AddVoter(KeyValuePair<Origin, VoteLineBlock> voter, MarkerType marker = MarkerType.None)
        {
            if (voter.Key.AuthorType == IdentityType.Plan) sb.Append("[b]");

            string markerToDisplay;
            if (voter.Key.AuthorType == IdentityType.Plan)
                markerToDisplay = Strings.PlanNameMarker;
            else if (marker == MarkerType.Rank && voter.Value.MarkerType != MarkerType.Rank)
                markerToDisplay = Strings.NoRankMarker;
            else
                markerToDisplay = voter.Value.Marker;

            sb.Append("[");
            sb.Append(markerToDisplay);
            sb.Append("] ");

            if (voter.Key.AuthorType == IdentityType.Plan) sb.Append("Plan: ");

            sb.Append("[url=\"");
            sb.Append(voter.Key.Permalink);
            sb.Append("\"]");
            sb.Append(voter.Key.Author);
            sb.Append("[/url]");

            if (voter.Key.AuthorType == IdentityType.Plan) sb.Append("[/b]");

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

            sb.AppendLine(voteInfo.LineBreak);
            sb.AppendLine();
        }

        /// <summary>
        /// Add a line break between marker categories.
        /// </summary>
        private void AddDoubleLineBreak()
        {
            sb.AppendLine(voteInfo.DoubleLineBreak);
            sb.AppendLine();
        }
        #endregion
    }
}
