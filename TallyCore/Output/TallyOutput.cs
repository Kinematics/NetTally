using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;

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
                int count = GetRankedVoterCount();
                AddVoterCount(count);
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
            if (task.Key.Length > 0)
            {
                sb.Append("[b]Task: ");
                sb.Append(task.Key);
                sb.Append("[/b]\r\n");
                sb.AppendLine("");
            }

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

        }

        #endregion

        #region General assist functions
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

        private string GetVoterUrl(string voter, VoteType voteType)
        {
            Dictionary<string, string> idLookup = VoteCounter.GetVotersCollection(voteType);

            if (idLookup.ContainsKey(voter))
                return Quest.ForumAdapter.GetPermalinkForId(idLookup[voter]);

            return string.Empty;
        }

        private int GetRankedVoterCount()
        {
            var voters = VoteCounter.GetVotersCollection(VoteType.Rank);
            return voters.Count;
        }

        private int GetNormalVoterCount()
        {
            var voters = VoteCounter.GetVotersCollection(VoteType.Vote);
            return voters.Count - VoteCounter.PlanNames.Count;
        }

        private void AddVoterCount(int count)
        {
            if (count > 0)
            {
                sb.Append("Total No. of Voters: ");
                sb.Append(count);
                sb.AppendLine();
                sb.AppendLine();
            }
        }

        #endregion
    }
}
