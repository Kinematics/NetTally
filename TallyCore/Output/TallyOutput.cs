using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using NetTally.Extensions;
using NetTally.Utility;
using NetTally.VoteCounting;
using NetTally.Votes;

namespace NetTally.Output
{
    // Task (string), Ordered list of ranked votes
    using RankResultsByTask = Dictionary<string, List<string>>;

    /// <summary>
    /// Class to handle generating the output of a tally run, for display in the text box.
    /// </summary>
    public class TallyOutput : ITextResultsProvider
    {
        #region Local Properties
        DisplayMode DisplayMode { get; set; }

        StringBuilder sb { get; set; }

        static string[] rankWinnerLabels = { "Winner", "First Runner Up", "Second Runner Up", "Third Runner Up", "Honorable Mention" };
        #endregion

        #region Public Interface
        /// <summary>
        /// Public function to generate the full output for the tally.
        /// </summary>
        /// <param name="displayMode">The mode requested for how to format the output.</param>
        /// <param name="token">Cancellation token so that processing can be cancelled.</param>
        /// <returns>Returns the full string to be displayed.</returns>
        public async Task<string> BuildOutputAsync(DisplayMode displayMode, CancellationToken token)
        {
            if (VoteCounter.Instance.Quest == null)
                return string.Empty;

            DisplayMode = displayMode;

            sb = new StringBuilder();

            await Task.Run(() => BuildGlobal()).ConfigureAwait(false);

            return sb.ToString();
        }
        #endregion

        #region Top-level functions
        /// <summary>
        /// General construction.  Add the header and any vote output.
        /// Surround by spoiler tags if requested by the display mode.
        /// </summary>
        private void BuildGlobal()
        {
            using (new Spoiler(sb, "Tally Results", DisplayMode == DisplayMode.SpoilerAll || AdvancedOptions.Instance.GlobalSpoilers))
            {
                AddHeader();

                ConstructRankedOutput();

                ConstructNormalOutput();
            }
        }

        /// <summary>
        /// Add the header indicating the title of the thread that was tallied,
        /// and the marker that this is a tally result (along with the program version number).
        /// </summary>
        private void AddHeader()
        {
            sb.Append("[b]Vote Tally");
            if (AdvancedOptions.Instance.DebugMode)
                sb.Append(" (DEBUG)");
            sb.Append("[/b] : ");
            sb.AppendLine(VoteCounter.Instance.Title);

            sb.AppendLine($"[color=transparent]##### {ProductInfo.Name} {ProductInfo.Version}[/color]");
            sb.AppendLine();
        }
        #endregion

        #region Ranked votes
        /// <summary>
        /// Generate output for any ranked votes that were tallied.
        /// </summary>
        private void ConstructRankedOutput()
        {
            if (VoteCounter.Instance.HasRankedVotes)
            {
                VoteCounting.IRankVoteCounter counter = VoteCounting.VoteCounterLocator.GetRankVoteCounter(AdvancedOptions.Instance.RankVoteCounterMethod);
                RankResultsByTask results = counter.CountVotes(VoteCounter.Instance.GetVotesCollection(VoteType.Rank));

                var orderedRes = results.OrderBy(a => a.Key);

                // Output the ranking results for each task
                foreach (var task in orderedRes)
                {
                    AddRankTask(task);
                    sb.AppendLine("");
                }

                // Output the total number of voters
                AddTotalVoterCount(VoteInfo.RankedVoterCount);
                sb.AppendLine("");
            }
        }

        /// <summary>
        /// Add a rank task.
        /// Choose which method to use based on the display mode.
        /// </summary>
        /// <param name="task">The task to output.</param>
        private void AddRankTask(KeyValuePair<string, List<string>> task)
        {
            if (DisplayMode == DisplayMode.Compact || DisplayMode == DisplayMode.CompactNoVoters)
            {
                AddCompactTask(task);
            }
            else
            {
                AddCompleteTask(task);
            }
        }

        /// <summary>
        /// Add a rank task in compact format.
        /// </summary>
        /// <param name="task">The task to output.</param>
        private void AddCompactTask(KeyValuePair<string, List<string>> task)
        {
            if (task.Key.Length > 0)
            {
                sb.AppendLine($"{task.Key}:");
            }

            int num = 1;
            foreach (var entry in task.Value)
            {
                sb.AppendLine($"[{num++}] {entry}");
            }
        }

        /// <summary>
        /// Add a rank task in complete format.
        /// </summary>
        /// <param name="task">The task to output.</param>
        private void AddCompleteTask(KeyValuePair<string, List<string>> task)
        {
            AddTaskLabel(task.Key);

            AddRankedOptions(task.Key);

            int index = 0;
            foreach (var winner in task.Value)
            {
                sb.Append("[b]");
                sb.Append(rankWinnerLabels[index++]);
                sb.Append(":[/b] ");
                sb.AppendLine(winner);

                if (index > 4)
                    index = 4;

                if (DisplayMode != DisplayMode.NormalNoVoters)
                    AddRankedVoters(task.Key, winner);
                else
                    sb.AppendLine();
            }
        }

        /// <summary>
        /// Add the list of all possible voting options for this rank task.
        /// </summary>
        /// <param name="taskName">The name of the task.</param>
        private void AddRankedOptions(string taskName)
        {
            var votes = VoteCounter.Instance.GetVotesCollection(VoteType.Rank);
            var voteContents = votes.
                Where(v => VoteString.GetVoteTask(v.Key) == taskName).
                Select(v => VoteString.GetVoteContent(v.Key));

            HashSet<string> uniqueOptions = new HashSet<string>(voteContents, Agnostic.StringComparer);

            sb.AppendLine("[b]Options:[/b]");

            foreach (var option in uniqueOptions.OrderBy(a => a))
            {
                AddVoteStringLine(option);
            }

            sb.AppendLine();
        }

        /// <summary>
        /// Add the list of all voters for a given winning option, along with
        /// the ranking they gave that option.
        /// </summary>
        /// <param name="taskName">The name of the task.</param>
        /// <param name="choice">The name of the choice selected.</param>
        private void AddRankedVoters(string taskName, string choice)
        {
            var votes = VoteCounter.Instance.GetVotesCollection(VoteType.Rank);
            var voters = VoteCounter.Instance.GetVotersCollection(VoteType.Rank);

            var whoVoted = from v in votes
                           where VoteString.GetVoteTask(v.Key) == taskName &&
                                 Agnostic.StringComparer.Equals(VoteString.GetVoteContent(v.Key), choice)
                           select new { marker = VoteString.GetVoteMarker(v.Key), voters = v.Value };

            var whoDidNotVote = from v in voters
                                where whoVoted.Any(a => a.voters.Contains(v.Key)) == false
                                select v.Key;

            using (new Spoiler(sb, "Voters", DisplayMode == DisplayMode.SpoilerVoters || DisplayMode == DisplayMode.SpoilerAll))
            {
                foreach (var mark in whoVoted.OrderBy(a => a.marker))
                {
                    var sortedVoters = mark.voters.OrderBy(a => a);
                    foreach (var voter in sortedVoters)
                    {
                        AddVoter(voter, VoteType.Rank, mark.marker);
                    }
                }

                foreach (var nonVoter in whoDidNotVote.OrderBy(a => a))
                {
                    AddVoter(nonVoter, VoteType.Rank, "-");
                }
            }

            sb.AppendLine();
        }

        #endregion

        #region Normal votes
        /// <summary>
        /// Handle general organization of outputting the tally results,
        /// grouped by task.  Use VoteNodes if displaying in a compact
        /// mode, or just use the original votes if displaying in a normal
        /// mode.
        /// Display the vote, the count, and the voters, as appropriate.
        /// </summary>
        private void ConstructNormalOutput()
        {
            if (VoteInfo.NormalVoterCount == 0)
                return;

            var allVotes = VoteCounter.Instance.GetVotesCollection(VoteType.Vote);
            var votesGroupedByTask = VoteInfo.GroupVotesByTask(allVotes);

            bool firstTask = true;

            foreach (var taskGroup in votesGroupedByTask)
            {
                if (taskGroup.Any())
                {
                    if (!firstTask)
                    {
                        AddLineBreak();
                    }

                    firstTask = false;

                    AddTaskLabel(taskGroup.Key);

                    if (DisplayMode == DisplayMode.Compact || DisplayMode == DisplayMode.CompactNoVoters)
                    {
                        var nodes = VoteInfo.GetVoteNodes(taskGroup);

                        foreach (var vote in nodes)
                        {
                            if (vote.VoterCount > 0)
                            {
                                AddVote(vote);
                            }
                        }
                    }
                    else
                    {
                        foreach (var vote in taskGroup.OrderByDescending(v => VoteInfo.CountVote(v)))
                        {
                            AddVote(vote);
                            AddVoteCount(vote);
                            AddVoters(vote.Value, "Voters");
                        }
                    }

                }
            }

            AddTotalVoterCount(VoteInfo.NormalVoterCount);
        }

        #region Add by VoteNode
        /// <summary>
        /// Add the text of the provided vote to the output.
        /// In compact mode, adds the contents of the children as well, with
        /// their individual vote counts.
        /// </summary>
        /// <param name="vote"></param>
        private void AddVote(VoteNode vote)
        {
            if (DisplayMode == DisplayMode.Compact)
            {
                AddVoteStringLine(vote.GetLine(DisplayMode));

                var children = vote.Children.OrderByDescending(v => v.VoterCount);
                foreach (var child in children)
                {
                    AddVoters(child.Voters, child.GetLine(DisplayMode));
                }

                if (vote.Voters.Count > 0)
                {
                    AddVoters(vote.Voters, "Voters");
                }
            }
            else if (DisplayMode == DisplayMode.CompactNoVoters)
            {
                AddVoteStringLine(vote.GetLine(DisplayMode));

                var children = vote.Children.OrderByDescending(v => v.VoterCount);
                foreach (var child in children)
                {
                    AddVoteStringLine(child.GetLine(DisplayMode));
                }
            }
            else
            {
                sb.Append(vote.Text);
            }
        }
        #endregion

        #region Add by KeyValuePair
        /// <summary>
        /// Add or delegate adding the text of the provided vote to the output.
        /// </summary>
        /// <param name="vote">The vote to add.</param>
        private void AddVote(KeyValuePair<string, HashSet<string>> vote)
        {
            if (DisplayMode == DisplayMode.Compact || DisplayMode == DisplayMode.CompactNoVoters)
            {
                AddCompactVote(vote);
            }
            else
            {
                AddVoteString(vote.Key);
            }
        }

        /// <summary>
        /// Adds the vote string to the string builder, after reformatting any BBCode.
        /// </summary>
        /// <param name="vote">The vote.</param>
        private void AddVoteString(string vote)
        {
            sb.Append(VoteString.FormatBBCodeForOutput(vote));
        }

        /// <summary>
        /// Adds the vote string as a full line to the string builder, after reformatting any BBCode.
        /// </summary>
        /// <param name="vote">The vote.</param>
        private void AddVoteStringLine(string vote)
        {
            sb.AppendLine(VoteString.FormatBBCodeForOutput(vote));
        }

        /// <summary>
        /// Add the provided vote to the output in compact format.
        /// </summary>
        /// <param name="vote">The vote to add.</param>
        private void AddCompactVote(KeyValuePair<string, HashSet<string>> vote)
        {
            List<string> voteLines = vote.Key.GetStringLines();

            if (voteLines.Count == 0)
                return;

            int userCount = VoteInfo.CountVote(vote);
            string userCountMarker = userCount.ToString();

            // Single-line votes are always shown.
            if (voteLines.Count == 1)
            {
                sb.AppendLine(VoteString.ModifyVoteLine(voteLines.First(), marker: userCountMarker));
                return;
            }

            // Two-line votes can be shown if the second line is a sub-vote.
            if (voteLines.Count == 2 && !string.IsNullOrEmpty(VoteString.GetVotePrefix(voteLines.Last())))
            {
                sb.AppendLine(VoteString.ModifyVoteLine(voteLines.First(), marker: userCountMarker));
                sb.AppendLine(VoteString.ModifyVoteLine(voteLines.Last(), marker: userCountMarker));
                return;
            }


            // Longer votes get condensed down to a link to the original post (and named after the first voter)
            string firstVoter = VoteInfo.GetFirstVoter(vote.Value);

            string task = VoteString.GetVoteTask(vote.Key);
            sb.Append($"[{userCountMarker}]");
            if (!string.IsNullOrEmpty(task))
                sb.Append($"[{task}]");

            string link;

            if (firstVoter.StartsWith(StringUtility.PlanNameMarker, StringComparison.Ordinal))
            {
                link = VoteInfo.GetVoterUrl(firstVoter, VoteType.Plan);
            }
            else
            {
                link = VoteInfo.GetVoterUrl(firstVoter, VoteType.Vote);
            }

            sb.Append($" Plan: {firstVoter} — {link}\r\n");
        }

        /// <summary>
        /// Add the vote count for the provided vote to the output.
        /// Does not add to the output in compact mode.
        /// </summary>
        /// <param name="vote">The vote to add.</param>
        private void AddVoteCount(KeyValuePair<string, HashSet<string>> vote)
        {
            if (DisplayMode != DisplayMode.Compact && DisplayMode != DisplayMode.CompactNoVoters)
            {
                AddVoterCount(VoteInfo.CountVote(vote));
            }
        }
        #endregion

        #endregion

        #region General functions to add to the string output
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
        /// Add the list of voters supporting the provided vote.
        /// </summary>
        /// <param name="voters">The list of voters to display.</param>
        /// <param name="spoilerLabel">The label to use for the spoiler (if used).</param>
        private void AddVoters(HashSet<string> voters, string spoilerLabel)
        {
            if (DisplayMode == DisplayMode.CompactNoVoters)
                return;

            if (DisplayMode == DisplayMode.NormalNoVoters)
            {
                sb.AppendLine();
                return;
            }

            using (new Spoiler(sb, spoilerLabel, DisplayMode != DisplayMode.Normal))
            {
                var orderedVoters = VoteInfo.GetOrderedVoterList(voters);

                foreach (var voter in orderedVoters)
                {
                    AddVoter(voter);
                }
            }

            if (DisplayMode != DisplayMode.Compact)
            {
                sb.AppendLine();
            }
        }

        /// <summary>
        /// Add a line containing a reference to a voter's post.
        /// Handles rank, plan, and normal voters.
        /// </summary>
        /// <param name="voterName">The name of the voter.</param>
        /// <param name="voteType">The type of vote this is for.</param>
        /// <param name="marker">The marker to use for rank votes.</param>
        private void AddVoter(string voterName, VoteType voteType = VoteType.Vote, string marker = null)
        {
            bool closeBold = false;

            if (voteType == VoteType.Rank && marker != null)
            {
                sb.Append("[");
                sb.Append(marker);
                sb.Append("] ");
            }
            else if (VoteCounter.Instance.PlanNames.Contains(voterName))
            {
                sb.Append("[b]Plan: ");
                closeBold = true;
            }

            sb.Append("[url=\"");
            sb.Append(VoteInfo.GetVoterUrl(voterName, voteType));
            sb.Append("\"]");
            sb.Append(voterName);
            sb.Append("[/url]");

            if (closeBold)
            {
                sb.Append("[/b]");
            }

            sb.AppendLine();
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
        /// Add a line showing the specified total number of voters.
        /// </summary>
        /// <param name="count">The count to display.</param>
        private void AddTotalVoterCount(int count)
        {
            if (DisplayMode == DisplayMode.Compact || DisplayMode == DisplayMode.CompactNoVoters)
                sb.AppendLine();

            sb.Append("Total No. of Voters: ");
            sb.Append(count);
            sb.AppendLine();
            sb.AppendLine();
        }

        /// <summary>
        /// Add a line break (for between tasks).
        /// Gets the line break text from the quest's forum adapter, since some
        /// can show hard rules, and some need to just use manual text.
        /// </summary>
        private void AddLineBreak()
        {
            if (DisplayMode == DisplayMode.Compact || DisplayMode == DisplayMode.CompactNoVoters)
                sb.AppendLine();

            sb.AppendLine(VoteCounter.Instance.Quest.ForumAdapter.LineBreak);
            sb.AppendLine();
        }
        #endregion
    }

}
