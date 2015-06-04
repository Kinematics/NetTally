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
    public class TextResults
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

            ConstructRankedOutput(quest);

            ConstructNormalOutput(quest);

            if (DisplayMode == DisplayMode.SpoilerAll)
                AddSpoilerEnd();

            return sb.ToString();
        }


        /// <summary>
        /// Construct the output of ranked votes for the quest.
        /// </summary>
        /// <param name="sb">The string builder to add the results to.</param>
        /// <param name="quest">The quest being tallied.</param>
        private void ConstructRankedOutput(IQuest quest)
        {
            if (VoteCounter.HasRankedVotes)
            {
                // Get ranked results, and order them by task name
                var results = RankVotes.Rank(VoteCounter).OrderBy(a => a.Key);

                // output the ranking result
                foreach (var result in results)
                {
                    AddTaskLabel(result.Key);

                    AddRankedOptions(result.Key);

                    AddRankedWinner(result.Value.First());

                    AddRankedVoters(quest, result);

                    AddRunnersUp(result.Value.Skip(1));

                    sb.AppendLine("");
                }
            }
        }

        /// <summary>
        /// Construct the output of normal votes for the quest.
        /// </summary>
        /// <param name="sb">The string builder to add the results to.</param>
        /// <param name="quest">The quest being tallied.</param>
        private void ConstructNormalOutput(IQuest quest)
        {
            var groupedVotesWithSupporters = GroupVotes(VoteCounter.VotesWithSupporters);
            bool firstTask = true;

            foreach (var taskGroup in groupedVotesWithSupporters)
            {
                if (!firstTask)
                {
                    AddLineBreak();
                }

                firstTask = false;

                AddTaskLabel(taskGroup.Key);

                foreach (var vote in taskGroup.OrderByDescending(v => v.Value.Count(vc => VoteCounter.PlanNames.Contains(vc) == false)))
                {
                    sb.Append(vote.Key);

                    AddVoteCount(vote.Value);

                    if (DisplayMode == DisplayMode.SpoilerVoters || DisplayMode == DisplayMode.SpoilerAll)
                    {
                        AddSpoilerStart("Voters");
                    }

                    AddVoters(vote.Value, quest);

                    if (DisplayMode == DisplayMode.SpoilerVoters || DisplayMode == DisplayMode.SpoilerAll)
                    {
                        AddSpoilerEnd();
                    }

                    sb.AppendLine("");
                }
            }

            AddTotalVoterCount(sb);
        }



        #region Functions for adding pieces of text to the output results.
        private void AddSpoilerStart(string label)
        {
            sb.AppendFormat("[spoiler={0}]\r\n", label);
        }

        private void AddSpoilerEnd()
        {
            sb.AppendLine("[/spoiler]");
        }

        /// <summary>
        /// Construct the header text for the tally results.
        /// </summary>
        private void AddHeader()
        {
            var assembly = Assembly.GetExecutingAssembly();
            var product = (AssemblyProductAttribute)assembly.GetCustomAttribute(typeof(AssemblyProductAttribute));
            var version = (AssemblyInformationalVersionAttribute)assembly.GetCustomAttribute(typeof(AssemblyInformationalVersionAttribute));

            sb.AppendFormat("[b]Vote Tally[/b] : {0}\r\n", VoteCounter.Title);
            sb.AppendFormat("[color=transparent]##### {0} {1}[/color]\r\n\r\n",
                product.Product,
                version.InformationalVersion);
        }

        /// <summary>
        /// Add a line break to the output.
        /// </summary>
        /// <param name="sb">The string builder to add the results to.</param>
        private void AddLineBreak()
        {
            //sb.AppendLine("[hr][/hr]");
            //sb.AppendLine("---------------------------------------------------------\r\n");
            sb.AppendLine("—————————————————————————————————————————————————————————\r\n");
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
        /// <param name="sb">The string builder to add the results to.</param>
        /// <param name="task">The name of the task.</param>
        private void AddTaskLabel(string task)
        {
            if (task.Length > 0)
            {
                sb.AppendFormat("[b]Task: {0}[/b]\r\n\r\n", task);
            }
        }

        /// <summary>
        /// Add all voters from the provided list of voters to the output string.
        /// Plans are placed before users, and each group (after the first voter)
        /// is alphabetized.
        /// </summary>
        /// <param name="sb">The string builder to add the results to.</param>
        /// <param name="voters">The set of voters being added.</param>
        /// <param name="quest">The quest being tallied.</param>
        private void AddVoters(HashSet<string> voters, IQuest quest)
        {
            string firstVoter = voters.First();

            AddVoter(firstVoter, quest);

            var remainder = voters.Skip(1);

            var remainingPlans = remainder.Where(vc => VoteCounter.PlanNames.Contains(vc) == true);

            foreach (var supporter in remainingPlans.OrderBy(v => v))
            {
                AddVoter(supporter, quest);
            }

            var remainingVoters = remainder.Except(remainingPlans);

            foreach (var supporter in remainingVoters.OrderBy(v => v))
            {
                AddVoter(supporter, quest);
            }
        }

        /// <summary>
        /// Add an individual voter to the output.
        /// </summary>
        /// <param name="sb">The string builder to add the results to.</param>
        /// <param name="voter">The name of the voter being added.</param>
        /// <param name="quest">The quest being tallied.</param>
        private void AddVoter(string voter, IQuest quest)
        {
            sb.Append(GenerateSupporterUrl(quest, voter));
        }

        /// <summary>
        /// Add an individual voter to the output.
        /// </summary>
        /// <param name="sb">The string builder to add the results to.</param>
        /// <param name="voter">The name of the voter being added.</param>
        /// <param name="rank">The rank that the voter rated the current vote.</param>
        /// <param name="quest">The quest being tallied.</param>
        private void AddRankedVoter(string voter, string marker, IQuest quest)
        {
            sb.Append(GenerateSupporterUrl(quest, voter, marker));
        }

        /// <summary>
        /// Add the the total number of voters to the tally results.
        /// </summary>
        /// <param name="sb">The string builder to add the results to.</param>
        private void AddTotalVoterCount(StringBuilder sb)
        {
            int totalVoterCount = VoteCounter.VoterMessageId.Count - VoteCounter.PlanNames.Count;
            if (totalVoterCount > 0)
            {
                sb.AppendLine("");
                sb.AppendFormat("Total No. of Voters: {0}\r\n", totalVoterCount);
            }
        }

        /// <summary>
        /// Generate a line for a supporter (that's possibly a plan), including the
        /// link to the original post that user voted in.
        /// </summary>
        /// <param name="quest">The quest being tallied.</param>
        /// <param name="supporter">The supporter of a given plan.</param>
        /// <returns>Returns a url'ized string for the voter's post.</returns>
        private string GenerateSupporterUrl(IQuest quest, string supporter)
        {
            StringBuilder sb = new StringBuilder();

            string tail = string.Empty;
            if (VoteCounter.PlanNames.Contains(supporter))
            {
                sb.Append("[b]Plan: ");
                tail = "[/b]";
            }

            AddSupporterUrl(supporter, VoteCounter.VoterMessageId, quest);

            sb.AppendLine(tail);

            return sb.ToString();
        }

        /// <summary>
        /// Generate a line for a voter that ranked a vote with a specific value, including the
        /// link to the original post that user voted in.
        /// </summary>
        /// <param name="quest">The quest being tallied.</param>
        /// <param name="supporter">The supporter of a given plan.</param>
        /// <returns>Returns a url'ized string for the voter's post.</returns>
        private string GenerateSupporterUrl(IQuest quest, string supporter, string marker)
        {
            StringBuilder sb = new StringBuilder();

            sb.AppendFormat("[{0}] ", marker);
            AddSupporterUrl(supporter, VoteCounter.RankedVoterMessageId, quest);
            sb.AppendLine("");

            return sb.ToString();
        }

        /// <summary>
        /// Adds a [url] entry to the provided string builder for the supporter,
        /// within a given quest.
        /// </summary>
        /// <param name="sb">The string builder to add the results to.</param>
        /// <param name="supporter">The supporter of a given plan.</param>
        /// <param name="quest">The quest being tallied.</param>
        private void AddSupporterUrl(string supporter, Dictionary<string, string> idLookup, IQuest quest)
        {
            sb.Append("[url=\"");
            sb.Append(quest.GetForumAdapter().GetPostUrlFromId(quest.ThreadName, idLookup[supporter]));
            sb.Append("\"]");
            sb.Append(supporter);
            sb.Append("[/url]");
        }

        /// <summary>
        /// Add the list of options available for the given ranked task.
        /// </summary>
        /// <param name="sb">The string builder to add the results to.</param>
        /// <param name="task"></param>
        private void AddRankedOptions(string task)
        {
            var voteContents = VoteCounter.RankedVotesWithSupporters.
                Where(v => VoteLine.GetVoteTask(v.Key) == task).
                Select(v => VoteLine.GetVoteContent(v.Key));

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
        /// <param name="sb">The string builder to add the results to.</param>
        /// <param name="winningChoice">The winning choice.</param>
        private void AddRankedWinner(string winningChoice)
        {
            sb.AppendFormat("[b]Winner:[/b] {0}\r\n\r\n", winningChoice);
        }

        /// <summary>
        /// Add the list of voters who voted for the winning vote for the current task.
        /// </summary>
        /// <param name="sb">The string builder to add the results to.</param>
        /// <param name="quest">The quest being tallied.</param>
        /// <param name="result">The task and winning vote.</param>
        private void AddRankedVoters(IQuest quest, KeyValuePair<string, List<string>> result)
        {
            if (DisplayMode == DisplayMode.SpoilerVoters || DisplayMode == DisplayMode.SpoilerAll)
            {
                AddSpoilerStart("Voters");
            }

            string winningChoice = result.Value.First();

            var whoVoted = from v in VoteCounter.RankedVotesWithSupporters
                           where VoteLine.GetVoteTask(v.Key) == result.Key &&
                                 VoteLine.GetVoteContent(v.Key) == winningChoice
                           select new { marker = VoteLine.GetVoteMarker(v.Key), voters = v.Value };

            var markerOrder = whoVoted.OrderBy(a => a.marker);

            foreach (var mark in markerOrder)
            {
                var sortedVoters = mark.voters.OrderBy(a => a);
                foreach (var voter in sortedVoters)
                {
                    AddRankedVoter(voter, mark.marker, quest);
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
        /// <param name="sb">The string builder to add the results to.</param>
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

        #region Utility functions for constructing chunks of the output.
        private IOrderedEnumerable<IGrouping<string, KeyValuePair<string, HashSet<string>>>> GroupVotes(Dictionary<string, HashSet<string>> votesWithSupporters)
        {
            var grouped = from v in votesWithSupporters
                          group v by VoteLine.GetVoteTask(v.Key) into g
                          orderby g.Key
                          select g;

            return grouped;
        }

        #endregion
    }
}
