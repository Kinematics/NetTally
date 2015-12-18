using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using NetTally.Utility;

namespace NetTally.Output
{
    /// <summary>
    /// Class to handle generating the output of a tally run, for display in the text box.
    /// </summary>
    public class TallyOutput : ITextResultsProvider
    {
        #region Local Properties
        IQuest Quest { get; set; }
        DisplayMode DisplayMode { get; set; }

        StringBuilder sb { get; set; }

        static string[] rankWinnerLabels = { "Winner", "First Runner Up", "Second Runner Up", "Third Runner Up", "Honorable Mention" };
        #endregion

        #region Public Interface
        /// <summary>
        /// Public function to generate the full output for the tally.
        /// </summary>
        /// <param name="quest">The quest being tallied.</param>
        /// <param name="displayMode">The mode requested for how to format the output.</param>
        /// <returns>Returns the full string to be displayed.</returns>
        public string BuildOutput(IQuest quest, DisplayMode displayMode)
        {
            Quest = quest;
            DisplayMode = displayMode;

            sb = new StringBuilder();

            BuildGlobal();

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
            using (new Spoiler(sb, "Tally Results", DisplayMode == DisplayMode.SpoilerAll))
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
            var assembly = Assembly.GetExecutingAssembly();
            var product = (AssemblyProductAttribute)assembly.GetCustomAttribute(typeof(AssemblyProductAttribute));
            var version = (AssemblyInformationalVersionAttribute)assembly.GetCustomAttribute(typeof(AssemblyInformationalVersionAttribute));

            sb.Append("[b]Vote Tally");
            if (DebugMode.Active)
                sb.Append(" (DEBUG)");
            sb.Append("[/b] : ");
            sb.AppendLine(VoteCounter.Instance.Title);

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
            if (VoteCounter.Instance.HasRankedVotes)
            {
                // Get ranked results, and order them by task name
                var results = RankVotes.Rank().OrderBy(a => a.Key);

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
            var votes = VoteCounter.Instance.GetVotesCollection(VoteType.Rank);
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
            var votes = VoteCounter.Instance.GetVotesCollection(VoteType.Rank);
            var voters = VoteCounter.Instance.GetVotersCollection(VoteType.Rank);

            var whoVoted = from v in votes
                           where VoteString.GetVoteTask(v.Key) == taskName &&
                                 VoteString.GetVoteContent(v.Key) == choice
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
            var allVotes = VoteCounter.Instance.GetVotesCollection(VoteType.Vote);
            var votesGroupedByTask = GroupVotesByTask(allVotes);

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
                        var nodes = GetVoteNodes(taskGroup);

                        foreach (var vote in nodes)
                        {
                            if (vote.VoterCount > 0)
                            {
                                AddVote(vote);
                                AddVoteCount(vote);
                                AddVoters(vote);
                            }
                        }
                    }
                    else
                    {
                        foreach (var vote in taskGroup.OrderByDescending(v => CountVote(v)))
                        {
                            AddVote(vote);
                            AddVoteCount(vote);
                            AddVoters(vote);
                        }
                    }

                }
            }

            AddTotalVoterCount(NormalVoterCount);
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
            if (DisplayMode == DisplayMode.Compact || DisplayMode == DisplayMode.CompactNoVoters)
            {
                sb.AppendLine(VoteString.ModifyVoteLine(vote.Line, marker: vote.VoterCount.ToString(), ByPartition: true));

                var children = vote.Children.OrderByDescending(v => v.VoterCount);
                foreach (var child in children)
                {
                    sb.AppendLine(VoteString.ModifyVoteLine(child.Line, marker: child.VoterCount.ToString(), ByPartition: true));
                }
            }
            else
            {
                sb.Append(vote.Text);
            }
        }

        /// <summary>
        /// Add the voter count to the output, for the provided vote.
        /// </summary>
        /// <param name="vote">The vote to add.</param>
        private void AddVoteCount(VoteNode vote)
        {
            if (DisplayMode != DisplayMode.Compact && DisplayMode != DisplayMode.CompactNoVoters)
            {
                AddVoterCount(vote.VoterCount);
            }
        }

        /// <summary>
        /// Add the list of voters supporting the provided vote.
        /// </summary>
        /// <param name="vote">The vote to add.</param>
        private void AddVoters(VoteNode vote)
        {
            if (DisplayMode == DisplayMode.CompactNoVoters)
                return;

            using (new Spoiler(sb, "Voters", DisplayMode != DisplayMode.Normal))
            {
                var orderedVoters = GetOrderedVoterList(vote.Voters);

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
                sb.Append(vote.Key);
            }
        }

        /// <summary>
        /// Add the provided vote to the output in compact format.
        /// </summary>
        /// <param name="vote">The vote to add.</param>
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

        /// <summary>
        /// Add the vote count for the provided vote to the output.
        /// Does not add to the output in compact mode.
        /// </summary>
        /// <param name="vote">The vote to add.</param>
        private void AddVoteCount(KeyValuePair<string, HashSet<string>> vote)
        {
            if (DisplayMode != DisplayMode.Compact && DisplayMode != DisplayMode.CompactNoVoters)
            {
                AddVoterCount(CountVote(vote));
            }
        }

        /// <summary>
        /// Add the voters in the provided vote to the output.
        /// Does not add voters when in CompactNoVoters mode.
        /// </summary>
        /// <param name="vote">The vote to add.</param>
        private void AddVoters(KeyValuePair<string, HashSet<string>> vote)
        {
            if (DisplayMode == DisplayMode.CompactNoVoters)
                return;

            using (new Spoiler(sb, "Voters", DisplayMode != DisplayMode.Normal))
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

            sb.AppendLine(Quest.ForumAdapter.LineBreak);
            sb.AppendLine();
        }
        #endregion

        #region General assist functions
        /// <summary>
        /// Get the URL for the post that is linked to the specified voter.
        /// </summary>
        /// <param name="voter">The voter to look up.</param>
        /// <param name="voteType">The type of vote being checked.</param>
        /// <returns>Returns the permalink URL for the voter.  Returns an empty string if not found.</returns>
        public string GetVoterUrl(string voter, VoteType voteType)
        {
            Dictionary<string, string> idLookup = VoteCounter.Instance.GetVotersCollection(voteType);

            string voteID;
            if (idLookup.TryGetValue(voter, out voteID))
                return Quest.ForumAdapter.GetPermalinkForId(voteID);

            return string.Empty;
        }

        /// <summary>
        /// Property to get the total number of ranked voters in the tally.
        /// </summary>
        public int RankedVoterCount => VoteCounter.Instance.GetVotersCollection(VoteType.Rank).Count;

        /// <summary>
        /// Property to get the total number of normal voters in the tally.
        /// </summary>
        public int NormalVoterCount => VoteCounter.Instance.GetVotersCollection(VoteType.Vote).Count - VoteCounter.Instance.PlanNames.Count;

        /// <summary>
        /// Calculate the number of non-plan voters in the provided vote object.
        /// </summary>
        /// <param name="vote">The vote containing a list of voters.</param>
        /// <returns>Returns how many of the voters in this vote were users (rather than plans).</returns>
        private int CountVote(KeyValuePair<string, HashSet<string>> vote) => vote.Value?.Count(vc => VoteCounter.Instance.PlanNames.Contains(vc) == false) ?? 0;

        /// <summary>
        /// Get a list of voters, ordered alphabetically, except the first voter,
        /// who is the 'earliest' of the provided voters (ie: the first person to
        /// vote for this vote or plan).
        /// </summary>
        /// <param name="voters">A set of voters.</param>
        /// <returns>Returns an organized, sorted list.</returns>
        private IEnumerable<string> GetOrderedVoterList(HashSet<string> voters)
        {
            var voterList = new List<string> { GetFirstVoter(voters) };
            var otherVoters = voters.Except(voterList);

            var orderedVoters = voterList.Concat(otherVoters.OrderBy(v => v));
            return orderedVoters;
        }

        /// <summary>
        /// Determine which of the provided voters was the 'first'.  That is,
        /// the earliest voter with an actual vote, rather than a reference to
        /// a future vote.
        /// </summary>
        /// <param name="voters">A set of voters to check.</param>
        /// <returns>Returns which one of them is considered the first real poster.</returns>
        public string GetFirstVoter(HashSet<string> voters)
        {
            var planVoters = voters.Where(v => VoteCounter.Instance.PlanNames.Contains(v));
            var votersCollection = VoteCounter.Instance.GetVotersCollection(VoteType.Vote);

            if (planVoters.Any())
            {
                return planVoters.MinObject(v => votersCollection[v]);
            }

            var nonFutureVoters = voters.Except(VoteCounter.Instance.FutureReferences.Select(p => p.Author));

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

        /// <summary>
        /// Group votes by task.
        /// </summary>
        /// <param name="allVotes">A list of all votes.</param>
        /// <returns>Returns all the votes, grouped by task (case-insensitive).</returns>
        private IOrderedEnumerable<IGrouping<string, KeyValuePair<string, HashSet<string>>>> GroupVotesByTask(Dictionary<string, HashSet<string>> allVotes)
        {
            var grouped = allVotes.GroupBy(v => VoteString.GetVoteTask(v.Key), StringComparer.OrdinalIgnoreCase).OrderBy(v => v.Key);

            return grouped;
        }

        /// <summary>
        /// Given a group of votes (grouped by task), create and return
        /// a list of VoteNodes that collapse together votes that are 
        /// sub-votes of each other.
        /// </summary>
        /// <param name="taskGroup">A set of votes with the same task value.</param>
        /// <returns>Returns a list of VoteNodes that collapse similar votes.</returns>
        private IEnumerable<VoteNode> GetVoteNodes(IGrouping<string, KeyValuePair<string, HashSet<string>>> taskGroup)
        {
            var groupByFirstLine = taskGroup.GroupBy(v => Text.FirstLine(v.Key), Text.AgnosticStringComparer);

            List<VoteNode> nodeList = new List<VoteNode>();
            VoteNode parent;

            foreach (var voteGroup in groupByFirstLine)
            {
                parent = null;

                foreach (var vote in voteGroup)
                {
                    var lines = Text.GetStringLines(vote.Key);

                    if (parent == null)
                    {
                        parent = new VoteNode(this, lines[0], vote.Value);
                    }

                    if (lines.Count == 1)
                    {
                        parent.AddVoters(vote.Value);
                    }
                    else if (lines.Count == 2 && VoteString.GetVotePrefix(lines[1]) != string.Empty)
                    {
                        VoteNode child = new VoteNode(this, lines[1], vote.Value);
                        parent.AddChild(child);
                    }
                    else
                    {
                        VoteNode child = new VoteNode(this, vote.Key, vote.Value);
                        parent.AddChild(child);
                    }
                }

                if (parent != null)
                {
                    nodeList.Add(parent);
                }
            }

            return nodeList.OrderByDescending(v => v.VoterCount);
        }
        #endregion
    }

}
