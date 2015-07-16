using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;

namespace NetTally
{
    public class TextResults : ITextResultsProvider
    {
        IQuest Quest { get; set; }
        DisplayMode DisplayMode { get; set; }
        IVoteCounter VoteCounter { get; set; }
        StringBuilder sb { get; set; }

        public string BuildOutput(IQuest quest, IVoteCounter voteCounter, DisplayMode displayMode)
        {
            VoteCounter = voteCounter;
            Quest = quest;
            DisplayMode = displayMode;

            sb = new StringBuilder();


            if (DisplayMode == DisplayMode.SpoilerAll)
                AddSpoilerStart("Tally Results");

            AddHeader();

            ConstructRankedOutput();

            ConstructNormalOutput();

            if (DisplayMode == DisplayMode.SpoilerAll)
                AddSpoilerEnd();

            return sb.ToString();
        }

        #region Top-level formatting logic
        /// <summary>
        /// Construct the output of ranked votes for the quest.
        /// </summary>
        private void ConstructRankedOutput()
        {
            if (VoteCounter.HasRankedVotes)
            {
                // Get ranked results, and order them by task name
                var results = RankVotes.Rank(VoteCounter).OrderBy(a => a.Key);

                // output the ranking result
                foreach (var result in results)
                {
                    if (DisplayMode == DisplayMode.Compact)
                    {
                        if (result.Key.Length > 0)
                        {
                            sb.Append($"{result.Key}:\r\n");
                        }

                        int num = 1;
                        foreach (var entry in result.Value)
                        {
                            sb.Append($"[{num++}] {entry}\r\n");
                        }

                        sb.AppendLine("");
                    }
                    else
                    {
                        AddTaskLabel(result.Key);

                        AddRankedOptions(result.Key);

                        AddRankedWinner(result.Value.First());

                        AddRankedVoters(result);

                        AddRunnersUp(result.Value.Skip(1));

                        sb.AppendLine("");
                    }
                }

                sb.AppendLine("");
            }
        }

        /// <summary>
        /// Construct the output of normal votes for the quest.
        /// </summary>
        private void ConstructNormalOutput()
        {
            var groupedVotesWithSupporters = GroupVotes(VoteCounter.VotesWithSupporters);
            bool firstTask = true;

            foreach (var taskGroup in groupedVotesWithSupporters)
            {
                bool taskHasVotes = taskGroup.Any(task => GetUserVoteCount(task.Value) > 0);

                if (taskHasVotes || (DisplayMode != DisplayMode.Compact && DisplayMode != DisplayMode.Streamlined))
                {
                    if (!firstTask)
                    {
                        AddLineBreak();
                    }

                    firstTask = false;
                    int userVoteCount = 0;

                    AddTaskLabel(taskGroup.Key);

                    // Show each vote result in descending number of votes.
                    foreach (var vote in taskGroup.OrderByDescending(v => v.Value.Count(vc => VoteCounter.PlanNames.Contains(vc) == false)))
                    {
                        switch (DisplayMode)
                        {
                            case DisplayMode.Compact:
                                userVoteCount = GetUserVoteCount(vote.Value);
                                if (userVoteCount > 0)
                                {
                                    AddCompactVoteNumber(userVoteCount);
                                    sb.Append($"{VoteString.GetVoteContentFirstLine(vote.Key)} : ");
                                    AddCompactVoters(vote.Value);
                                    sb.AppendLine("");
                                }
                                break;
                            case DisplayMode.Streamlined:
                                userVoteCount = GetUserVoteCount(vote.Value);
                                if (userVoteCount > 0)
                                {
                                    sb.Append($"{VoteString.GetVotePrefix(vote.Key)}");
                                    AddCompactVoteNumber(userVoteCount);
                                    sb.Append($"{VoteString.GetVoteContentFirstLine(vote.Key)}");
                                    sb.AppendLine("");
                                }
                                break;
                            default:
                                // print the entire vote
                                sb.Append(vote.Key);

                                AddVoteCount(vote.Value);

                                if (DisplayMode == DisplayMode.SpoilerVoters || DisplayMode == DisplayMode.SpoilerAll)
                                {
                                    AddSpoilerStart("Voters");
                                }

                                AddVoters(vote.Value);

                                if (DisplayMode == DisplayMode.SpoilerVoters || DisplayMode == DisplayMode.SpoilerAll)
                                {
                                    AddSpoilerEnd();
                                }

                                sb.AppendLine("");
                                break;
                        }
                    }
                }
            }

            if (DisplayMode != DisplayMode.Compact)
                AddTotalVoterCount();
        }
        #endregion

        #region Utility functions
        /// <summary>
        /// Get the number of voters that are users, and exclude plans.
        /// </summary>
        /// <param name="voters">The set of voters.</param>
        /// <returns>A count of the number of users voting.</returns>
        private int GetUserVoteCount(HashSet<string> voters)
        {
            return voters.Count(vc => VoteCounter.PlanNames.Contains(vc) == false);
        }

        /// <summary>
        /// Adds a [url] entry to the provided string builder for the supporter,
        /// within a given quest.
        /// </summary>
        /// <param name="voter">The supporter of a given plan.</param>
        private string GetVoterUrl(string voter, Dictionary<string, string> idLookup)
        {
            StringBuilder lb = new StringBuilder();

            lb.Append("[url=\"");
            lb.Append(Quest.GetForumAdapter().GetPostUrlFromId(Quest.ThreadName, idLookup[voter]));
            lb.Append("\"]");
            lb.Append(voter);
            lb.Append("[/url]");

            return lb.ToString();
        }

        /// <summary>
        /// Group votes together by task.
        /// </summary>
        /// <param name="votesWithSupporters">A collection of all votes.</param>
        /// <returns>Returns votes grouped by task.</returns>
        private IOrderedEnumerable<IGrouping<string, KeyValuePair<string, HashSet<string>>>> GroupVotes(Dictionary<string, HashSet<string>> votesWithSupporters)
        {
            var grouped = from v in votesWithSupporters
                          group v by VoteString.GetVoteTask(v.Key) into g
                          orderby g.Key
                          select g;

            return grouped;
        }

        #endregion



        #region Functions for adding pieces of text to the output results.
        /// <summary>
        /// Add a starting spoiler tag.
        /// </summary>
        /// <param name="label">The label for the spoiler tag.</param>
        private void AddSpoilerStart(string label)
        {
            sb.Append($"[spoiler={label}]\r\n");
        }

        /// <summary>
        /// Add an ending spoiler tag.
        /// </summary>
        private void AddSpoilerEnd()
        {
            sb.AppendLine("[/spoiler]");
        }

        /// <summary>
        /// Add a compact indicator of the number of votes for a proposal, placed in brackets.
        /// </summary>
        /// <param name="votes">The number of votes to report.</param>
        private void AddCompactVoteNumber(int votes)
        {
            // Number of voters where the voter name is not a plan name (and is thus a user).
            sb.Append($"[{votes}] ");
        }

        /// <summary>
        /// Adds a list of comma-separated voters with links to their posts.
        /// Names after the first one are alphabetized.
        /// </summary>
        /// <param name="voters">The list of voters.</param>
        private void AddCompactVoters(HashSet<string> voters)
        {
            string firstVoter = voters.Except(VoteCounter.LastFloatingReferencePerAuthor.Select(p => p.Author))
                .OrderBy(v => VoteCounter.VoterMessageId[v]).FirstOrDefault();

            if (firstVoter == null)
                firstVoter = voters.OrderBy(v => VoteCounter.VoterMessageId[v]).First();

            var remainder = voters.Where(v => v != firstVoter && VoteCounter.PlanNames.Contains(v) == false).OrderBy(v => v);

            sb.Append($"({GetVoterUrl(firstVoter, VoteCounter.VoterMessageId)}");

            foreach (var voter in remainder)
            {
                sb.Append($", {GetVoterUrl(voter, VoteCounter.VoterMessageId)}");
            }

            sb.Append(")");
        }


        /// <summary>
        /// Construct the header text for the tally results.
        /// </summary>
        private void AddHeader()
        {
            var assembly = Assembly.GetExecutingAssembly();
            var product = (AssemblyProductAttribute)assembly.GetCustomAttribute(typeof(AssemblyProductAttribute));
            var version = (AssemblyInformationalVersionAttribute)assembly.GetCustomAttribute(typeof(AssemblyInformationalVersionAttribute));

            sb.Append($"[b]Vote Tally[/b] : {VoteCounter.Title}\r\n");
            sb.Append($"[color=transparent]##### {product.Product} {version.InformationalVersion}[/color]\r\n\r\n");
        }

        /// <summary>
        /// Add a line break to the output.
        /// </summary>
        private void AddLineBreak()
        {
            if (DisplayMode == DisplayMode.Compact || DisplayMode == DisplayMode.Streamlined)
                sb.AppendLine("");

            //sb.AppendLine("[hr][/hr]");
            //sb.AppendLine("-------------------------------------------------------\r\n");
            sb.AppendLine("———————————————————————————————————————————————————————\r\n");
        }

        /// <summary>
        /// Add the total number of user votes (not plan votes) to the output.
        /// </summary>
        /// <param name="voters">The set of voters voting for this item.</param>
        private void AddVoteCount(HashSet<string> voters)
        {
            // Number of voters where the voter name is not a plan name (and is thus a user).
            sb.Append("[b]No. of Votes: ");
            sb.Append(voters.Count(vc => VoteCounter.PlanNames.Contains(vc) == false));
            sb.AppendLine("[/b]");
        }

        /// <summary>
        /// Add a task label line to the string builder.
        /// </summary>
        /// <param name="task">The name of the task.</param>
        private void AddTaskLabel(string task)
        {
            if (task.Length > 0)
            {
                sb.Append($"[b]Task: {task}[/b]\r\n\r\n");
            }
        }

        /// <summary>
        /// Add all voters from the provided list of voters to the output string.
        /// Plans are placed before users, and each group (after the first voter)
        /// is alphabetized.
        /// </summary>
        /// <param name="voters">The set of voters being added.</param>
        private void AddVoters(HashSet<string> voters)
        {
            string firstVoter = voters.Except(VoteCounter.LastFloatingReferencePerAuthor.Select(p => p.Author))
                .OrderBy(v => VoteCounter.VoterMessageId[v]).FirstOrDefault();

            if (firstVoter == null)
                firstVoter = voters.OrderBy(v => VoteCounter.VoterMessageId[v]).First();

            AddVoter(firstVoter);

            var remainder = voters.Where(v => v != firstVoter);

            var remainingPlans = remainder.Where(vc => VoteCounter.PlanNames.Contains(vc) == true);

            foreach (var supporter in remainingPlans.OrderBy(v => v))
            {
                AddVoter(supporter);
            }

            var remainingVoters = remainder.Except(remainingPlans);

            foreach (var supporter in remainingVoters.OrderBy(v => v))
            {
                AddVoter(supporter);
            }
        }

        /// <summary>
        /// Add an individual voter to the output.
        /// </summary>
        /// <param name="voter">The name of the voter being added.</param>
        private void AddVoter(string voter)
        {
            AddSupporterEntry(voter);
        }

        /// <summary>
        /// Add an individual voter to the output.
        /// </summary>
        /// <param name="voter">The name of the voter being added.</param>
        /// <param name="marker">The rank that the voter rated the current vote.</param>
        private void AddRankedVoter(string voter, string marker)
        {
            AddRankedSupporterEntry(voter, marker);
        }

        /// <summary>
        /// Add the the total number of voters to the tally results.
        /// </summary>
        private void AddTotalVoterCount()
        {
            int totalVoterCount = VoteCounter.VoterMessageId.Count - VoteCounter.PlanNames.Count;
            if (totalVoterCount > 0)
            {
                sb.Append($"\r\nTotal No. of Voters: {totalVoterCount}\r\n");
            }
        }

        /// <summary>
        /// Generate a line for a supporter (that's possibly a plan), including the
        /// link to the original post that user voted in.
        /// </summary>
        /// <param name="supporter">The supporter of a given plan.</param>
        /// <returns>Returns a url'ized string for the voter's post.</returns>
        private void AddSupporterEntry(string supporter)
        {
            string tail = string.Empty;
            if (VoteCounter.PlanNames.Contains(supporter))
            {
                sb.Append("[b]Plan: ");
                tail = "[/b]";
            }

            sb.Append(GetVoterUrl(supporter, VoteCounter.VoterMessageId));

            sb.AppendLine(tail);
        }

        /// <summary>
        /// Generate a line for a voter that ranked a vote with a specific value, including the
        /// link to the original post that user voted in.
        /// </summary>
        /// <param name="supporter">The supporter of a given plan.</param>
        /// <returns>Returns a url'ized string for the voter's post.</returns>
        private void AddRankedSupporterEntry(string supporter, string marker)
        {
            sb.Append($"[{marker}] ");
            sb.AppendLine(GetVoterUrl(supporter, VoteCounter.RankedVoterMessageId));
        }

        /// <summary>
        /// Add the list of options available for the given ranked task.
        /// </summary>
        /// <param name="task"></param>
        private void AddRankedOptions(string task)
        {
            var voteContents = VoteCounter.RankedVotesWithSupporters.
                Where(v => VoteString.GetVoteTask(v.Key) == task).
                Select(v => VoteString.GetVoteContent(v.Key));

            HashSet<string> uniqueOptions = new HashSet<string>(voteContents, StringComparer.OrdinalIgnoreCase);

            sb.AppendLine("[b]Options:[/b]");

            foreach (var option in uniqueOptions.OrderBy(a => a))
            {
                sb.AppendLine(option);
            }

            sb.AppendLine("");
        }

        /// <summary>
        /// Add the winner of the runoff for the given task's options.
        /// </summary>
        /// <param name="winningChoice">The winning choice.</param>
        private void AddRankedWinner(string winningChoice)
        {
            sb.Append($"[b]Winner:[/b] {winningChoice}\r\n\r\n");
        }

        /// <summary>
        /// Add the list of voters who voted for the winning vote for the current task.
        /// </summary>
        /// <param name="result">The task and winning vote.</param>
        private void AddRankedVoters(KeyValuePair<string, List<string>> result)
        {
            if (DisplayMode == DisplayMode.SpoilerVoters || DisplayMode == DisplayMode.SpoilerAll)
            {
                AddSpoilerStart("Voters");
            }

            string winningChoice = result.Value.First();

            var whoVoted = from v in VoteCounter.RankedVotesWithSupporters
                           where VoteString.GetVoteTask(v.Key) == result.Key &&
                                 VoteString.GetVoteContent(v.Key) == winningChoice
                           select new { marker = VoteString.GetVoteMarker(v.Key), voters = v.Value };

            var markerOrder = whoVoted.OrderBy(a => a.marker);

            foreach (var mark in markerOrder)
            {
                var sortedVoters = mark.voters.OrderBy(a => a);
                foreach (var voter in sortedVoters)
                {
                    AddRankedVoter(voter, mark.marker);
                }
            }

            if (DisplayMode == DisplayMode.SpoilerVoters || DisplayMode == DisplayMode.SpoilerAll)
            {
                AddSpoilerEnd();
            }

            sb.AppendLine("");
        }


        /// <summary>
        /// Add the top two runners-up in the tally.
        /// </summary>
        /// <param name="runnersUp">The list of runners-up, in order.</param>
        private void AddRunnersUp(IEnumerable<string> runnersUp)
        {
            if (runnersUp.Count() > 0)
            {
                sb.AppendLine("Runners Up:");

                foreach (var ranker in runnersUp)
                {
                    sb.AppendLine(ranker);
                }

                sb.AppendLine("");
            }
        }

        #endregion
    }
}
