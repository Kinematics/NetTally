using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using NetTally.Utility;

namespace NetTally.Output
{
    /// <summary>
    /// Class that can be put in a using() block to place a spoiler string in
    /// the provided string builder at construction and disposal.
    /// </summary>
    public struct Spoiler : IDisposable
    {
        StringBuilder SB;
        bool Display;

        /// <summary>
        /// Constructor.  Initialize required values so we know what and whether
        /// to display the spoiler tags.
        /// </summary>
        /// <param name="sb">The string builder that the text will be added to.</param>
        /// <param name="label">The label for the spoiler.  No spoiler will be displayed if it's null.</param>
        /// <param name="display">Whether we should display the text.</param>
        internal Spoiler(StringBuilder sb, string label, bool display)
        {
            SB = sb;
            Display = display && label != null;

            if (Display)
            {
                sb?.AppendLine($"[spoiler={label}]");
            }
        }

        /// <summary>
        /// Called when the using() is completed.  Close the tag if we're displaying text.
        /// </summary>
        public void Dispose()
        {
            if (Display)
            {
                SB?.AppendLine($"[/spoiler]");
            }
            SB = null;
        }
    }



    public class TallyOutput : ITextResultsProvider
    {
        #region Local Properties
        IQuest Quest { get; set; }
        IVoteCounter VoteCounter { get; set; }
        DisplayMode DisplayMode { get; set; }

        StringBuilder sb { get; set; }

        static string[] rankWinnerLabels = { "Winner", "First Runner Up", "Second Runner Up", "Third Runner Up", "Honorable Mention" };
        #endregion

        #region Public Interface
        public string BuildOutput(IQuest quest, IVoteCounter voteCounter, DisplayMode displayMode)
        {
            Quest = quest;
            VoteCounter = voteCounter;
            DisplayMode = displayMode;

            sb = new StringBuilder();

            BuildGlobal();

            return sb.ToString();
        }
        #endregion

        #region Top-level functions
        private void BuildGlobal()
        {
            using (var resultsSpoiler = new Spoiler(sb, "Tally Results", DisplayMode == DisplayMode.SpoilerAll))
            {
                AddHeader();

                ConstructRankedOutput();

                ConstructNormalOutput();
            }
        }

        private void AddHeader()
        {
            var assembly = Assembly.GetExecutingAssembly();
            var product = (AssemblyProductAttribute)assembly.GetCustomAttribute(typeof(AssemblyProductAttribute));
            var version = (AssemblyInformationalVersionAttribute)assembly.GetCustomAttribute(typeof(AssemblyInformationalVersionAttribute));

            sb.Append("[b]Vote Tally");
            if (DebugMode.Active)
                sb.Append(" (DEBUG)");
            sb.Append("[/b] : ");
            sb.AppendLine(VoteCounter.Title);

            sb.Append("[color=transparent]##### ");
            sb.Append(product.Product);
            sb.Append(" ");
            sb.Append(version.InformationalVersion);
            sb.AppendLine("[/color]");
            sb.AppendLine();
        }
        #endregion

        #region Ranked votes
        /// <summary>
        /// Generate output for any ranked votes that were tallied.
        /// </summary>
        private void ConstructRankedOutput()
        {
            if (VoteCounter.HasRankedVotes)
            {
                // Get ranked results, and order them by task name
                var results = RankVotes.Rank(VoteCounter).OrderBy(a => a.Key);

                // Output the ranking results for each task
                foreach (var task in results)
                {
                    AddRankTask(task);
                    sb.AppendLine("");
                }

                // Output the total number of voters
                AddTotalVoterCount(RankedVoterCount);
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

                AddRankedVoters(task.Key, winner);
            }
        }

        /// <summary>
        /// Add the list of all possible voting options for this rank task.
        /// </summary>
        /// <param name="taskName">The name of the task.</param>
        private void AddRankedOptions(string taskName)
        {
            var votes = VoteCounter.GetVotesCollection(VoteType.Rank);
            var voteContents = votes.
                Where(v => VoteString.GetVoteTask(v.Key) == taskName).
                Select(v => VoteString.GetVoteContent(v.Key));

            HashSet<string> uniqueOptions = new HashSet<string>(voteContents, StringComparer.OrdinalIgnoreCase);

            sb.AppendLine("[b]Options:[/b]");

            foreach (var option in uniqueOptions.OrderBy(a => a))
            {
                sb.AppendLine(option);
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
            var votes = VoteCounter.GetVotesCollection(VoteType.Rank);
            var voters = VoteCounter.GetVotersCollection(VoteType.Rank);

            var whoVoted = from v in votes
                           where VoteString.GetVoteTask(v.Key) == taskName &&
                                 VoteString.GetVoteContent(v.Key) == choice
                           select new { marker = VoteString.GetVoteMarker(v.Key), voters = v.Value };

            var whoDidNotVote = from v in voters
                                where whoVoted.Any(a => a.voters.Contains(v.Key)) == false
                                select v.Key;

            using (var spoilerVoters = new Spoiler(sb, "Voters", DisplayMode == DisplayMode.SpoilerVoters || DisplayMode == DisplayMode.SpoilerAll))
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
        private void ConstructNormalOutput()
        {
            var allVotes = VoteCounter.GetVotesCollection(VoteType.Vote);
            var votesGroupedByTask = GroupVotesByTask(allVotes);

            bool firstTask = true;

            foreach (var taskGroup in votesGroupedByTask)
            {
                if (taskGroup.Count() > 0)
                {
                    if (!firstTask)
                    {
                        AddLineBreak();
                    }

                    firstTask = false;

                    AddTaskLabel(taskGroup.Key);

                    // Get all votes, ordered by a count of the user votes (ie: don't count plan references)
                    var votesForTask = taskGroup.OrderByDescending(v => CountVote(v));

                    foreach (var vote in votesForTask)
                    {
                        AddVote(vote);
                        AddVoteCount(vote);
                        AddVoters(vote);
                    }
                }
            }

            AddTotalVoterCount(NormalVoterCount);
        }

        private void AddVote(KeyValuePair<string, HashSet<string>> vote)
        {
            if (DisplayMode == DisplayMode.Compact || DisplayMode == DisplayMode.CompactNoVoters)
            {
                AddCompactVote(vote);
            }
            else
            {
                sb.Append(vote.Key);
            }
        }

        private void AddCompactVote(KeyValuePair<string, HashSet<string>> vote)
        {
            List<string> voteLines = Text.GetStringLines(vote.Key);

            if (voteLines.Count == 0)
                return;

            int userCount = CountVote(vote);
            string userCountMarker = userCount.ToString();

            // Single-line votes are always shown.
            if (voteLines.Count == 1)
            {
                sb.AppendLine(VoteString.ModifyVoteLine(voteLines.First(), marker: userCountMarker));
                return;
            }

            // Two-line votes can be shown if the second line is a sub-vote.
            if (voteLines.Count == 2 && VoteString.GetVotePrefix(voteLines.Last()) != string.Empty)
            {
                sb.AppendLine(VoteString.ModifyVoteLine(voteLines.First(), marker: userCountMarker));
                sb.AppendLine(VoteString.ModifyVoteLine(voteLines.Last(), marker: userCountMarker));
                return;
            }


            // Longer votes get condensed down to a link to the original post (and named after the first voter)
            string firstVoter = GetFirstVoter(vote.Value);

            string task = VoteString.GetVoteTask(vote.Key);
            sb.Append($"[{userCountMarker}]");
            if (task != string.Empty)
                sb.Append($"[{task}]");

            string link;

            if (firstVoter.StartsWith(Text.PlanNameMarker, StringComparison.Ordinal))
            {
                link = GetVoterUrl(firstVoter, VoteType.Plan);
            }
            else
            {
                link = GetVoterUrl(firstVoter, VoteType.Vote);
            }

            sb.Append($" Plan: {firstVoter} — {link}\r\n");
        }

        private void AddVoteCount(KeyValuePair<string, HashSet<string>> vote)
        {
            if (DisplayMode != DisplayMode.Compact && DisplayMode != DisplayMode.CompactNoVoters)
            {
                AddVoterCount(CountVote(vote));
            }
        }

        private void AddVoters(KeyValuePair<string, HashSet<string>> vote)
        {
            if (DisplayMode == DisplayMode.CompactNoVoters)
                return;

            using (var spoilerVoters = new Spoiler(sb, "Voters", DisplayMode != DisplayMode.Normal))
            {
                var orderedVoters = GetOrderedVoterList(vote.Value);

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

        #endregion

        #region General functions to add to the string output
        private void AddVoter(string voterName, VoteType voteType = VoteType.Vote, string marker = null)
        {
            bool closeBold = false;

            if (voteType == VoteType.Rank && marker != null)
            {
                sb.Append("[");
                sb.Append(marker);
                sb.Append("] ");
            }
            else if (VoteCounter.PlanNames.Contains(voterName))
            {
                sb.Append("[b]Plan: ");
                closeBold = true;
            }

            sb.Append("[url=\"");
            sb.Append(GetVoterUrl(voterName, voteType));
            sb.Append("\"]");
            sb.Append(voterName);
            sb.Append("[/url]");

            if (closeBold)
            {
                sb.Append("[/b]");
            }

            sb.AppendLine();
        }

        private void AddVoterCount(int count)
        {
            sb.Append("[b]No. of Votes: ");
            sb.Append(count);
            sb.AppendLine("[/b]");
        }

        private void AddTotalVoterCount(int count)
        {
            if (DisplayMode == DisplayMode.Compact || DisplayMode == DisplayMode.CompactNoVoters)
                sb.AppendLine();

            sb.Append("Total No. of Voters: ");
            sb.Append(count);
            sb.AppendLine();
            sb.AppendLine();
        }

        private void AddLineBreak()
        {
            if (DisplayMode == DisplayMode.Compact || DisplayMode == DisplayMode.CompactNoVoters)
                sb.AppendLine();

            sb.AppendLine(Quest.ForumAdapter.LineBreak);
            sb.AppendLine();
        }

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

        #endregion

        #region General assist functions
        private string GetVoterUrl(string voter, VoteType voteType)
        {
            Dictionary<string, string> idLookup = VoteCounter.GetVotersCollection(voteType);

            string voteID;
            if (idLookup.TryGetValue(voter, out voteID))
                return Quest.ForumAdapter.GetPermalinkForId(voteID);

            return string.Empty;
        }

        private int RankedVoterCount => VoteCounter.GetVotersCollection(VoteType.Rank).Count;

        private int NormalVoterCount => VoteCounter.GetVotersCollection(VoteType.Vote).Count - VoteCounter.PlanNames.Count;

        private int CountVote(KeyValuePair<string, HashSet<string>> vote) => vote.Value?.Count(vc => VoteCounter.PlanNames.Contains(vc) == false) ?? 0;

        private IEnumerable<string> GetOrderedVoterList(HashSet<string> voters)
        {
            var voterList = new List<string> { GetFirstVoter(voters) };
            var otherVoters = voters.Except(voterList);

            var orderedVoters = voterList.Concat(otherVoters.OrderBy(v => v));
            return orderedVoters;
        }

        private string GetFirstVoter(HashSet<string> voters)
        {
            var planVoters = voters.Where(v => VoteCounter.PlanNames.Contains(v));
            var votersCollection = VoteCounter.GetVotersCollection(VoteType.Vote);

            if (planVoters.Any())
            {
                return planVoters.MinObject(v => votersCollection[v]);
            }

            var nonFutureVoters = voters.Except(VoteCounter.FutureReferences.Select(p => p.Author));

            if (nonFutureVoters.Any())
            {
                return nonFutureVoters.MinObject(v => votersCollection[v]);
            }

            if (voters.Any())
            {
                return voters.MinObject(v => votersCollection[v]);
            }

            return null;
        }

        private IOrderedEnumerable<IGrouping<string, KeyValuePair<string, HashSet<string>>>> GroupVotesByTask(Dictionary<string, HashSet<string>> allVotes)
        {
            var grouped = allVotes.GroupBy(v => VoteString.GetVoteTask(v.Key), Utility.Text.AgnosticStringComparer).OrderBy(v => v.Key);

            return grouped;
        }

        #endregion
    }
}
