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
using NetTally.VoteCounting;
using NetTally.Votes;

namespace NetTally.Experiment3
{
    public class TallyOutput : ITextResultsProvider
    {
        #region Constructor and private fields
        readonly IVoteCounter voteCounter;
        readonly IGeneralOutputOptions outputOptions;
        readonly VoteInfo voteInfo;

        DisplayMode displayMode;
        IQuest quest = new Quest();

        StringBuilder sb = new StringBuilder();
        const string cancelled = "Cancelled!";

        static readonly string[] rankWinnerLabels = { "Winner", "First Runner Up", "Second Runner Up", "Third Runner Up", "Honorable Mention" };

        public TallyOutput(IVoteCounter counter, VoteInfo info, IGeneralOutputOptions options)
        {
            voteCounter = counter;
            voteInfo = info;
            outputOptions = options;
        }
        #endregion


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

                ConstructRankedOutput(voteGroupings[MarkerType.Rank], token);
                if (voteGroupings[MarkerType.Rank].Count > 0)
                {
                    AddLineBreak();
                }
                ConstructScoredOutput(voteGroupings[MarkerType.Score], token);
                if (voteGroupings[MarkerType.Score].Count > 0)
                {
                    AddLineBreak();
                }
                ConstructApprovedOutput(voteGroupings[MarkerType.Approval], token);
                if (voteGroupings[MarkerType.Approval].Count > 0)
                {
                    AddLineBreak();
                }
                ConstructNormalOutput(voteGroupings[MarkerType.Vote], token);
            }
        }

        /// <summary>
        /// Collect all recorded votes from the VoteCounter into groups based on what
        /// type of vote each one was represented by (via MarkerType).
        /// Rank, Score, and Approval types are gathered when most of the votes are of
        /// that type.  Vote types are gathered when any votes are of that type.
        /// </summary>
        /// <returns>Returns the grouped collection.</returns>
        private Dictionary<MarkerType, Dictionary<VoteLineBlock, Dictionary<string, VoteLineBlock>>> GetVoteGroupings()
        {
            Dictionary<MarkerType, Dictionary<VoteLineBlock, Dictionary<string, VoteLineBlock>>> group1 =
                new Dictionary<MarkerType, Dictionary<VoteLineBlock, Dictionary<string, VoteLineBlock>>>
                {
                    [MarkerType.Rank] = new Dictionary<VoteLineBlock, Dictionary<string, VoteLineBlock>>(),
                    [MarkerType.Score] = new Dictionary<VoteLineBlock, Dictionary<string, VoteLineBlock>>(),
                    [MarkerType.Approval] = new Dictionary<VoteLineBlock, Dictionary<string, VoteLineBlock>>(),
                    [MarkerType.Vote] = new Dictionary<VoteLineBlock, Dictionary<string, VoteLineBlock>>()
                };

            MarkerType[] markers = { MarkerType.Rank, MarkerType.Score, MarkerType.Approval };

            foreach (var marker in markers)
            {
                foreach (var block in voteCounter.VoteBlockSupporters)
                {
                    // Using the default threshold for 'Most'.  Change if necessary.
                    if (block.Value.Most(v => v.Value.MarkerType == marker))
                    {
                        group1[marker].Add(block.Key, block.Value);
                    }
                }
            }

            foreach (var block in voteCounter.VoteBlockSupporters)
            {
                if (block.Value.Any(v => v.Value.MarkerType == MarkerType.Vote))
                {
                    group1[MarkerType.Vote].Add(block.Key, block.Value);
                }
            }

            return group1;
        }

        private IOrderedEnumerable<IGrouping<string, KeyValuePair<VoteLineBlock, Dictionary<string, VoteLineBlock>>>>
            GetVotesGroupedByTask(Dictionary<VoteLineBlock, Dictionary<string, VoteLineBlock>> votes)
        {
            var groupByTask = votes.GroupBy(a => a.Key.Task).OrderBy(a => a.Key);

            if (voteCounter.OrderedTaskList != null)
            {
                groupByTask = groupByTask.OrderBy(v => voteCounter.OrderedTaskList.IndexOf(v.Key));
            }

            return groupByTask;
        }



        #region Header Construction
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
        }

        /// <summary>
        /// Gets the post range of the votes that the VoteCounter tallied.
        /// Returns values of 0 if no valid posts are available.
        /// </summary>
        /// <param name="first">The first post number.</param>
        /// <param name="last">The last post number.</param>
        private (int first, int last) GetPostRange()
        {
            int first = 0;
            int last = 0;

            foreach (var post in voteCounter.Posts)
            {
                if (first == 0 || post.Number < first)
                    first = post.Number;
                if (post.Number > last)
                    last = post.Number;
            }

            return (first, last);
        }
        #endregion


        private void ConstructRankedOutput(Dictionary<VoteLineBlock, Dictionary<string, VoteLineBlock>> votes, CancellationToken token)
        {
            if (votes.Count == 0)
            {
                sb.AppendLine("No ranked votes");
                sb.AppendLine();
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

                    var orderedVotes = task.OrderByDescending(v => v.Value.Count).ThenBy(v => v.Key.First().CleanContent);

                    foreach (var vote in orderedVotes)
                    {
                        int rank = 2;

                        sb.AppendLine(vote.Key.ToStringWithMarker($"#{rank}"));
                        AddVoterCount(vote.Value.Count);
                        AddVoters(vote.Value);
                        sb.AppendLine();
                    }

                    sb.AppendLine();
                }
            }
        }

        private void ConstructScoredOutput(Dictionary<VoteLineBlock, Dictionary<string, VoteLineBlock>> votes, CancellationToken token)
        {
            if (votes.Count == 0)
            {
                sb.AppendLine("No scored votes");
                sb.AppendLine();
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

                    var orderedVotes = task.Select(v => new { vote = v, score = GetVoteScore(v) })
                        .OrderByDescending(a => a.score)
                        .ThenBy(b => b.vote.Key.First().CleanContent);

                    foreach (var vote in orderedVotes)
                    {
                        sb.AppendLine(vote.vote.Key.ToStringWithMarker($"{vote.score}%"));
                        AddVoterCount(vote.vote.Value.Count);
                        AddVoters(vote.vote.Value);
                        sb.AppendLine();
                    }

                    sb.AppendLine();
                }
            }
        }

        private void ConstructApprovedOutput(Dictionary<VoteLineBlock, Dictionary<string, VoteLineBlock>> votes, CancellationToken token)
        {
            if (votes.Count == 0)
            {
                sb.AppendLine("No approved votes");
                sb.AppendLine();
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

                    var orderedVotes = task.OrderByDescending(v => v.Value.Count).ThenBy(v => v.Key.First().CleanContent);

                    foreach (var vote in orderedVotes)
                    {
                        int approval = 200;

                        sb.AppendLine(vote.Key.ToStringWithMarker($"{approval}"));
                        AddVoterCount(vote.Value.Count);
                        AddVoters(vote.Value);
                        sb.AppendLine();
                    }

                    sb.AppendLine();
                }
            }
        }

        /// <summary>
        /// Handle general organization of outputting the tally results,
        /// grouped by task.  Use VoteNodes if displaying in a compact
        /// mode, or just use the original votes if displaying in a normal
        /// mode.
        /// Display the vote, the count, and the voters, as appropriate.
        /// </summary>
        private void ConstructNormalOutput(Dictionary<VoteLineBlock, Dictionary<string, VoteLineBlock>> votes, CancellationToken token)
        {
            if (votes.Count == 0)
            {
                sb.AppendLine("No standard votes");
                sb.AppendLine();
                return;
            }

            var votesGroupedByTask = GetVotesGroupedByTask(votes);

            bool firstTask = true;

            foreach (var task in votesGroupedByTask)
            {
                token.ThrowIfCancellationRequested();

                if (task.Any())
                {
                    if (!firstTask) AddLineBreak();

                    firstTask = false;

                    AddTaskLabel(task.Key);

                    var orderedVotes = task.OrderByDescending(v => v.Value.Count).ThenBy(v => v.Key.First().CleanContent);

                    foreach (var vote in orderedVotes)
                    {
                        sb.AppendLine(vote.Key.ToStringWithMarker());
                        AddVoterCount(vote.Value.Count);
                        AddVoters(vote.Value);
                        sb.AppendLine();
                    }

                    sb.AppendLine();
                }
            }
        }

        private void AddVoters(Dictionary<string, VoteLineBlock> voters, string spoilerLabel = "Voters")
        {
            if (outputOptions.DisplayMode == DisplayMode.NormalNoVoters || outputOptions.DisplayMode == DisplayMode.CompactNoVoters)
                return;

            using (new Spoiler(sb, spoilerLabel, outputOptions.DisplayMode != DisplayMode.Normal))
            {
                var orderedVoters = voteInfo.GetOrderedVoterList(voters);

                foreach (var voter in orderedVoters)
                {
                    AddVoter(voter);
                }
            }
        }

        private void AddVoter(KeyValuePair<string, VoteLineBlock> voter)
        {
            var (permalink, plan) = voteInfo.GetVoterPostPermalink(voter.Key);

            if (plan) sb.Append("[b]");

            sb.Append("[");
            sb.Append(voter.Value.Marker);
            sb.Append("] ");

            if (plan) sb.Append("Plan: ");

            sb.Append("[url=\"");
            sb.Append(permalink);
            sb.Append("\"]");
            sb.Append(voter.Key);
            sb.Append("[/url]");

            if (plan) sb.Append("[/b]");

            sb.AppendLine();
        }


        private int GetVoteScore(KeyValuePair<VoteLineBlock, Dictionary<string, VoteLineBlock>> vote)
        {
            int count = vote.Value.Count;

            int scoreTotal = vote.Value.Sum(v => v.Value.MarkerValue);

            return (int)Math.Round((double)scoreTotal / count, 0);
        }






        /// <summary>
        /// Add a line showing the specified number of voters.
        /// </summary>
        /// <param name="count">The count to display.</param>
        private void AddVoterCount(int count)
        {
            sb.Append("[b]No. of Votes: ");
            sb.Append(count);
            sb.AppendLine("[/b]");
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
                sb.AppendLine();
            }
        }


        /// <summary>
        /// Add a line break (for between tasks).
        /// Gets the line break text from the quest's forum adapter, since some
        /// can show hard rules, and some need to just use manual text.
        /// </summary>
        private void AddLineBreak()
        {
            if (displayMode == DisplayMode.Compact || displayMode == DisplayMode.CompactNoVoters)
                sb.AppendLine();

            sb.AppendLine(voteInfo.LineBreak);
            sb.AppendLine();
        }

    }
}
