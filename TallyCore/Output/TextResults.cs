using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Reflection;
using NetTally.Utility;

namespace NetTally.Output
{
    public class TextResults : ITextResultsProvider
    {
        IQuest Quest { get; set; }
        IVoteCounter VoteCounter { get; set; }
        DisplayMode DisplayMode { get; set; }

        StringBuilder sb { get; set; }

        public string BuildOutput(IQuest quest, IVoteCounter voteCounter, DisplayMode displayMode)
        {
            Quest = quest;
            VoteCounter = voteCounter;
            DisplayMode = displayMode;

            sb = new StringBuilder();


            if (DisplayMode == DisplayMode.SpoilerAll)
                StartSpoiler("Tally Results");

            AddHeader();

            ConstructRankedOutput();

            ConstructNormalOutput();

            if (DisplayMode == DisplayMode.SpoilerAll)
                EndSpoiler();

            return sb.ToString();
        }

        #region Top-level formatting logic
        /// <summary>
        /// Construct the header text for the tally results.
        /// </summary>
        private void AddHeader()
        {
            var assembly = Assembly.GetExecutingAssembly();
            var product = (AssemblyProductAttribute)assembly.GetCustomAttribute(typeof(AssemblyProductAttribute));
            var version = (AssemblyInformationalVersionAttribute)assembly.GetCustomAttribute(typeof(AssemblyInformationalVersionAttribute));

            if (DebugMode.Active)
                sb.Append($"[b]Vote Tally (DEBUG)[/b] : ");
            else
                sb.Append($"[b]Vote Tally[/b] : ");
            sb.AppendLine(VoteCounter.Title);
            sb.AppendLine($"[color=transparent]##### {product.Product} {version.InformationalVersion}[/color]");
            sb.AppendLine("");
        }

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
                    if (DisplayMode == DisplayMode.Compact || DisplayMode == DisplayMode.CompactNoVoters)
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

                        string[] labels = { "Winner", "First Runner Up", "Second Runner Up", "Third Runner Up", "Honorable Mention" };
                        int index = 0;
                        foreach (var winner in result.Value)
                        {
                            sb.Append($"[b]{labels[index++]}:[/b] {winner}\r\n");
                            if (index > 4)
                                index = 4;

                            AddRankedVoters(result.Key, winner);
                        }

                        sb.AppendLine("");
                    }
                }

                AddTotalRankedVoterCount();

                sb.AppendLine("");
            }
        }

        /// <summary>
        /// Construct the output of normal votes for the quest.
        /// </summary>
        private void ConstructNormalOutput()
        {
            var votesWithSupporters = VoteCounter.GetVotesCollection(VoteType.Vote);
            var votesGroupedByTask = GroupVotesByTask(votesWithSupporters);
            bool firstTask = true;
            int userVoteCount = 0;

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
                    var votes = taskGroup.OrderByDescending(v => v.Value.Count(vc => VoteCounter.PlanNames.Contains(vc) == false));

                    foreach (var vote in votes)
                    {
                        switch (DisplayMode)
                        {
                            case DisplayMode.Compact:
                                userVoteCount = GetUserVoteCount(vote.Value);
                                AddCompactVote(vote, taskGroup.Key, userVoteCount);
                                StartSpoiler("Voters");
                                AddVoters(vote.Value);
                                EndSpoiler();
                                break;
                            case DisplayMode.CompactNoVoters:
                                userVoteCount = GetUserVoteCount(vote.Value);
                                AddCompactVote(vote, taskGroup.Key, userVoteCount);
                                break;
                            default:
                                // Print the entire vote and vote count
                                sb.Append(vote.Key);
                                AddVoteCount(vote.Value);

                                // Spoiler the voters if requested
                                if (DisplayMode == DisplayMode.SpoilerVoters || DisplayMode == DisplayMode.SpoilerAll)
                                {
                                    StartSpoiler("Voters");
                                    AddVoters(vote.Value);
                                    EndSpoiler();
                                }
                                else
                                {
                                    AddVoters(vote.Value);
                                }

                                sb.AppendLine("");
                                break;
                        }
                    }

                }
            }

            AddTotalVoterCount();
        }
        #endregion

        #region Utility functions (no side effects)
        /// <summary>
        /// Split the text of a vote string into a list of individual lines.
        /// </summary>
        /// <param name="text">Text of a vote.</param>
        /// <returns>Returns a list of lines from the vote.</returns>
        private List<string> GetVoteLines(string text)
        {
            var split = text.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
            return new List<string>(split);
        }

        /// <summary>
        /// Group votes together by task.
        /// </summary>
        /// <param name="votesWithSupporters">A collection of all votes.</param>
        /// <returns>Returns votes grouped by task.</returns>
        private IOrderedEnumerable<IGrouping<string, KeyValuePair<string, HashSet<string>>>> GroupVotesByTask(Dictionary<string, HashSet<string>> votesWithSupporters)
        {
            var grouped = from v in votesWithSupporters
                          group v by VoteString.GetVoteTask(v.Key) into g
                          orderby g.Key
                          select g;

            return grouped;
        }

        /// <summary>
        /// Get the number of voters that are users, and exclude plans.
        /// </summary>
        /// <param name="voters">The set of voters.</param>
        /// <returns>A count of the number of users voting.</returns>
        private int GetUserVoteCount(HashSet<string> voters) => voters.Count(vc => VoteCounter.PlanNames.Contains(vc) == false);

        /// <summary>
        /// Get the URL that links to a voter's post, varying by vote type
        /// </summary>
        /// <param name="voter">The name of the voter to look up.</param>
        /// <param name="voteType">The type of voter being queried.</param>
        /// <returns>Returns the constructed URL that links to the post made by the voter.</returns>
        private string GetVoterUrl(string voter, VoteType voteType)
        {
            Dictionary<string, string> idLookup = VoteCounter.GetVotersCollection(voteType);
            string url = Quest.ForumAdapter.GetPermalinkForId(idLookup[voter]);

            return url;
        }

        /// <summary>
        /// Provides the URL for a voter's post in BBCode format.
        /// </summary>
        /// <param name="voter">The supporter of a given plan.</param>
        /// <param name="voteType">The type of voter being queried.</param>
        /// <returns>Returns the constructed BBCode URL that links to the post made by the voter.</returns>
        private string GetVoterUrlBBCode(string voter, VoteType voteType)
        {
            string url = GetVoterUrl(voter, voteType);
            return $"[url=\"{url}\"]{voter}[/url]";
        }

        /// <summary>
        /// Get the string providing the URL'ized form of the link to a voter's
        /// plan, with the optional bolding and labeling of the line if it's a
        /// reference to a base plan.
        /// </summary>
        /// <param name="voter">The voter we're constructing the line for.</param>
        /// <returns>Returns a string that can be appended to the output.</returns>
        private string GetNormalVoterLine(string voter)
        {
            if (VoteCounter.PlanNames.Contains(voter))
                return $"[b]Plan: {GetVoterUrlBBCode(voter, VoteType.Vote)}[/b]";
            else
                return GetVoterUrlBBCode(voter, VoteType.Vote);
        }

        /// <summary>
        /// Given a list of voters, select the first one that counts as a 'real' vote
        /// (ie: not reference votes).
        /// </summary>
        /// <param name="voters">The set of voters to check.</param>
        /// <returns>Returns an IEnumerable containing the first voter.</returns>
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

        /// <summary>
        /// Given a list of voters, order the voters alphabetically, except for the
        /// first voter, who is always placed first.
        /// </summary>
        /// <param name="voters">The set of voters to check.</param>
        /// <returns>Returns an ordered enumeration of the voters.</returns>
        private IEnumerable<string> GetOrderedVoterList(HashSet<string> voters)
        {
            var voterList = new List<string> { GetFirstVoter(voters) };
            var otherVoters = voters.Except(voterList);

            var orderedVoters = voterList.Concat(otherVoters.OrderBy(v => v));
            return orderedVoters;
        }

        /// <summary>
        /// Given a list of voters, create a list of the corresponding URLs that
        /// point to each voter's post, ordered alphabetically aside from the
        /// first voter.
        /// </summary>
        /// <param name="voters">The set of voters to check.</param>
        /// <returns>Returns an ordered enumeration of links to the voters' posts.</returns>
        private IEnumerable<string> GetOrderedVoterUrlList(HashSet<string> voters)
        {
            var urls = from v in GetOrderedVoterList(voters)
                       select GetVoterUrlBBCode(v, VoteType.Vote);

            return urls;
        }
        #endregion

        #region Functions for adding pieces of text to the output results.
        /// <summary>
        /// Add a starting spoiler tag.
        /// </summary>
        /// <param name="label">The label for the spoiler tag.</param>
        private void StartSpoiler(string label)
        {
            sb.AppendLine($"[spoiler={label}]");
        }

        /// <summary>
        /// Add an ending spoiler tag.
        /// </summary>
        private void EndSpoiler()
        {
            sb.AppendLine("[/spoiler]");
        }

        /// <summary>
        /// Add a vote in compact format to the output stringbuilder.
        /// </summary>
        /// <param name="vote">The vote to add.</param>
        /// <param name="task">The task the vote is associated with.</param>
        /// <param name="userVoteCount">The voter count for the vote</param>
        private void AddCompactVote(KeyValuePair<string, HashSet<string>> vote, string task, int userVoteCount)
        {
            List<string> voteLines = GetVoteLines(vote.Key);

            if (voteLines.Count == 0)
                return;

            // Single-line votes are always shown.
            if (voteLines.Count == 1)
            {
                sb.AppendLine(VoteString.ModifyVoteLine(voteLines.First(), marker: userVoteCount.ToString()));
                return;
            }

            // Two-line votes can be shown if the second line is a sub-vote.
            if (voteLines.Count == 2 && VoteString.GetVotePrefix(voteLines.Last()) != string.Empty)
            {
                sb.AppendLine(VoteString.ModifyVoteLine(voteLines.First(), marker: userVoteCount.ToString()));
                sb.AppendLine(VoteString.ModifyVoteLine(voteLines.Last(), marker: userVoteCount.ToString()));
                return;
            }


            // Longer votes get condensed down to a link to the original post (and named after the first voter)
            string firstVoter = GetFirstVoter(vote.Value);

            sb.Append($"[{userVoteCount}]");
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
            var orderedVotersArray = GetOrderedVoterUrlList(voters).ToArray();
            sb.AppendLine($"({string.Join(", ", orderedVotersArray)})");
        }

        /// <summary>
        /// Add a line break to the output.
        /// </summary>
        private void AddLineBreak()
        {
            if (DisplayMode == DisplayMode.Compact || DisplayMode == DisplayMode.CompactNoVoters)
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
            var orderedVoters = GetOrderedVoterList(voters);

            foreach (var voter in orderedVoters)
            {
                AddVoter(voter);
            }
        }

        /// <summary>
        /// Generate a line for a voter (that's possibly a plan), including the
        /// link to the original post that user voted in.
        /// </summary>
        /// <param name="voter">The name of the voter being added.</param>
        private void AddVoter(string voter)
        {
            sb.AppendLine(GetNormalVoterLine(voter));
        }

        /// <summary>
        /// Add an individual voter to the output.
        /// </summary>
        /// <param name="voter">The name of the voter being added.</param>
        /// <param name="marker">The rank that the voter rated the current vote.</param>
        private void AddRankedVoter(string voter, string marker)
        {
            sb.Append($"[{marker}] ");
            sb.AppendLine(GetVoterUrlBBCode(voter, VoteType.Rank));
        }

        /// <summary>
        /// Add the the total number of voters to the tally results.
        /// </summary>
        private void AddTotalVoterCount()
        {
            var voters = VoteCounter.GetVotersCollection(VoteType.Vote);
            int totalVoterCount = voters.Count - VoteCounter.PlanNames.Count;
            if (totalVoterCount > 0)
            {
                sb.Append($"\r\nTotal No. of Voters: {totalVoterCount}\r\n");
            }
        }

        /// <summary>
        /// Add the the total number of ranked voters to the tally results.
        /// </summary>
        private void AddTotalRankedVoterCount()
        {
            var voters = VoteCounter.GetVotersCollection(VoteType.Rank);
            int totalVoterCount = voters.Count;
            if (totalVoterCount > 0)
            {
                sb.Append($"Total No. of Voters: {totalVoterCount}\r\n\r\n");
            }
        }

        /// <summary>
        /// Add the list of options available for the given ranked task.
        /// </summary>
        /// <param name="task"></param>
        private void AddRankedOptions(string task)
        {
            var votes = VoteCounter.GetVotesCollection(VoteType.Rank);
            var voteContents = votes.
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
        private void AddRankedVoters(string task, string choice)
        {
            var votes = VoteCounter.GetVotesCollection(VoteType.Rank);
            var voters = VoteCounter.GetVotersCollection(VoteType.Rank);

            var whoVoted = from v in votes
                           where VoteString.GetVoteTask(v.Key) == task &&
                                 VoteString.GetVoteContent(v.Key) == choice
                           select new { marker = VoteString.GetVoteMarker(v.Key), voters = v.Value };

            var whoDidNotVote = from v in voters
                                where whoVoted.Any(a => a.voters.Contains(v.Key)) == false
                                select v.Key;

            if (DisplayMode == DisplayMode.SpoilerVoters || DisplayMode == DisplayMode.SpoilerAll)
            {
                StartSpoiler("Voters");
            }

            foreach (var mark in whoVoted.OrderBy(a => a.marker))
            {
                var sortedVoters = mark.voters.OrderBy(a => a);
                foreach (var voter in sortedVoters)
                {
                    AddRankedVoter(voter, mark.marker);
                }
            }

            foreach (var nonVoter in whoDidNotVote.OrderBy(a => a))
            {
                AddRankedVoter(nonVoter, "-");
            }

            if (DisplayMode == DisplayMode.SpoilerVoters || DisplayMode == DisplayMode.SpoilerAll)
            {
                EndSpoiler();
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
