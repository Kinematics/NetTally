using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using HtmlAgilityPack;

namespace NetTally
{
    public class Tally : INotifyPropertyChanged
    {
        IPageProvider pageProvider;
        IVoteCounter voteCounter;

        string results = string.Empty;
        bool useSpoilerForVoters = false;
        bool tallyIsRunning = false;

        IQuest lastTallyQuest = null;
        List<HtmlDocument> loadedPages = null;

        public Tally()
        {
            pageProvider = new WebPageProvider();
            voteCounter = new VoteCounter();

            pageProvider.StatusChanged += PageProvider_StatusChanged;
        }

        #region Event handling
        /// <summary>
        /// Keep watch for any status messasges from the page provider, and add them
        /// to the TallyResults string so that they can be displayed in the UI.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void PageProvider_StatusChanged(object sender, MessageEventArgs e)
        {
            TallyResults = TallyResults + e.Message;
        }

        /// <summary>
        /// Event for INotifyPropertyChanged.
        /// </summary>
        public event PropertyChangedEventHandler PropertyChanged;

        /// <summary>
        /// Function to raise events when a property has been changed.
        /// </summary>
        /// <param name="propertyName">The name of the property that was modified.</param>
        protected void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        #endregion

        #region Behavior properties
        /// <summary>
        /// The string containing the current tally progress or results.
        /// Creates a notification event if the contents change.
        /// </summary>
        public string TallyResults
        {
            get { return results; }
            set
            {
                results = value;
                OnPropertyChanged();
            }
        }

        /// <summary>
        /// Flag for whether to use spoiler blocks for voter lists in
        /// the output display.
        /// Recalculates the display if changed.
        /// </summary>
        public bool UseSpoilerForVoters
        {
            get { return useSpoilerForVoters; }
            set
            {
                useSpoilerForVoters = value;
                ConstructResults(lastTallyQuest);
            }
        }

        /// <summary>
        /// Flag whether the tally is currently running.
        /// </summary>
        public bool TallyIsRunning
        {
            get { return tallyIsRunning; }
            set
            {
                tallyIsRunning = value;
                OnPropertyChanged();
            }
        }

        #endregion

        #region Access properties
        public IVoteCounter VoteCounter => voteCounter;
        #endregion

        #region Interface functions
        /// <summary>
        /// Run the tally for the specified quest.
        /// </summary>
        /// <param name="questTitle">The name of the quest thread to scan.</param>
        /// <param name="startPost">The starting post number.</param>
        /// <param name="endPost">The ending post number.</param>
        /// <returns></returns>
        public async Task Run(IQuest quest, CancellationToken token)
        {
            try
            {
                TallyIsRunning = true;

                TallyResults = string.Empty;
                lastTallyQuest = quest;

                var fa = await quest.GetForumAdapterAsync(token);

                if (fa == null)
                    throw new InvalidOperationException(string.Format("Unable to load a forum adapter for the quest thread:\n{0}", quest.ThreadName));

                // Load pages from the website
                loadedPages = await pageProvider.LoadPages(quest, token).ConfigureAwait(false);

                UpdateTally(quest);
            }
            catch (Exception)
            {
                lastTallyQuest = null;
                loadedPages?.Clear();
                loadedPages = null;
                throw;
            }
            finally
            {
                TallyIsRunning = false;
            }
        }

        /// <summary>
        /// Process the results of the tally through the vote counter, and update the output.
        /// </summary>
        /// <param name="changedQuest"></param>
        public void UpdateTally(IQuest changedQuest)
        {
            if (lastTallyQuest != null && changedQuest == lastTallyQuest)
            {
                if (loadedPages != null && loadedPages.Count > 0)
                {
                    // Tally the votes from the loaded pages.
                    voteCounter.TallyVotes(lastTallyQuest, loadedPages);

                    // Compose the final result string from the compiled votes.
                    ConstructResults(lastTallyQuest);
                }
            }
        }

        /// <summary>
        /// Allow manual clearing of the page cache.
        /// </summary>
        public void ClearPageCache()
        {
            pageProvider.ClearPageCache();
        }

        #endregion

        #region Functions for constructing the tally results output.
        /// <summary>
        /// Compose the tallied results into a string to put in the TallyResults property,
        /// for display in the UI.
        /// </summary>
        public void ConstructResults(IQuest quest)
        {
            if (quest == null)
                return;

            StringBuilder sb = new StringBuilder();

            AddHeader(sb);

            ConstructRankedOutput(sb, quest);

            ConstructNormalOutput(sb, quest);

            TallyResults = sb.ToString();
        }

        /// <summary>
        /// Construct the output of ranked votes for the quest.
        /// </summary>
        /// <param name="sb">The string builder to add the results to.</param>
        /// <param name="quest">The quest being tallied.</param>
        private void ConstructRankedOutput(StringBuilder sb, IQuest quest)
        {
            if (voteCounter.HasRankedVotes)
            {
                RankVotes.Rank(voteCounter);

                // output the ranking result
            }
        }

        /// <summary>
        /// Construct the output of normal votes for the quest.
        /// </summary>
        /// <param name="sb">The string builder to add the results to.</param>
        /// <param name="quest">The quest being tallied.</param>
        private void ConstructNormalOutput(StringBuilder sb, IQuest quest)
        {
            var groupedVotesWithSupporters = GroupVotes(voteCounter.VotesWithSupporters);
            bool firstTask = true;

            foreach (var taskGroup in groupedVotesWithSupporters)
            {
                if (!firstTask)
                {
                    AddLineBreak(sb);
                }

                firstTask = false;

                AddTaskLabel(sb, taskGroup.Key);

                foreach (var vote in taskGroup.OrderByDescending(v => v.Value.Count(vc => voteCounter.PlanNames.Contains(vc) == false)))
                {
                    sb.Append(vote.Key);

                    AddVoteCount(sb, vote.Value);

                    if (UseSpoilerForVoters)
                    {
                        sb.AppendLine("[spoiler=Voters]");
                    }

                    AddVoters(sb, vote.Value, quest);

                    if (UseSpoilerForVoters)
                    {
                        sb.AppendLine("[/spoiler]");
                    }

                    sb.AppendLine("");
                }
            }

            AddTotalVoterCount(sb);
        }
        #endregion

        #region Functions for adding pieces of text to the output results.
        /// <summary>
        /// Construct the header text for the tally results.
        /// </summary>
        /// <param name="sb">The string builder to add the results to.</param>
        private void AddHeader(StringBuilder sb)
        {
            var assembly = Assembly.GetExecutingAssembly();
            var product = (AssemblyProductAttribute)assembly.GetCustomAttribute(typeof(AssemblyProductAttribute));
            var version = (AssemblyInformationalVersionAttribute)assembly.GetCustomAttribute(typeof(AssemblyInformationalVersionAttribute));

            sb.AppendFormat("[b]Vote Tally[/b] : {0}\r\n", voteCounter.Title);
            sb.AppendFormat("[color=transparent]##### {0} {1}[/color]\r\n\r\n",
                product.Product,
                version.InformationalVersion);
        }

        /// <summary>
        /// Add a line break to the output.
        /// </summary>
        /// <param name="sb">The string builder to add the results to.</param>
        private void AddLineBreak(StringBuilder sb)
        {
            //sb.AppendLine("[hr][/hr]");
            //sb.AppendLine("-----------------------------------------------------------\r\n");
            sb.AppendLine("———————————————————————————————————————————————————————————\r\n");
        }

        /// <summary>
        /// Add the total number of user votes to the output.
        /// </summary>
        /// <param name="sb">The string builder to add the results to.</param>
        /// <param name="voters">The set of voters voting for this item.</param>
        private void AddVoteCount(StringBuilder sb, HashSet<string> voters)
        {
            sb.Append("[b]No. of Votes: ");
            sb.Append(voters.Count(vc => voteCounter.PlanNames.Contains(vc) == false));
            sb.AppendLine("[/b]");
        }

        /// <summary>
        /// Add a task label line to the string builder.
        /// </summary>
        /// <param name="sb">The string builder to add the results to.</param>
        /// <param name="task">The name of the task.</param>
        private void AddTaskLabel(StringBuilder sb, string task)
        {
            if (task.Length > 0)
            {
                sb.AppendFormat("[b]Task: {0}[/b]\r\n", task);
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
        private void AddVoters(StringBuilder sb, HashSet<string> voters, IQuest quest)
        {
            string firstVoter = voters.First();

            AddVoter(sb, firstVoter, quest);

            var remainder = voters.Skip(1);

            var remainingPlans = remainder.Where(vc => voteCounter.PlanNames.Contains(vc) == true);

            foreach (var supporter in remainingPlans.OrderBy(v => v))
            {
                AddVoter(sb, supporter, quest);
            }

            var remainingVoters = remainder.Except(remainingPlans);

            foreach (var supporter in remainingVoters.OrderBy(v => v))
            {
                AddVoter(sb, supporter, quest);
            }
        }

        /// <summary>
        /// Add an individual voter to the output.
        /// </summary>
        /// <param name="sb">The string builder to add the results to.</param>
        /// <param name="voter">The name of the voter being added.</param>
        /// <param name="quest">The quest being tallied.</param>
        private void AddVoter(StringBuilder sb, string voter, IQuest quest)
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
        private void AddRankedVoter(StringBuilder sb, string voter, int rank, IQuest quest)
        {
            sb.Append(GenerateSupporterUrl(quest, voter, rank));
        }

        /// <summary>
        /// Add the the total number of voters to the tally results.
        /// </summary>
        /// <param name="sb">The string builder to add the results to.</param>
        private void AddTotalVoterCount(StringBuilder sb)
        {
            sb.AppendLine("");
            int totalVoterCount = voteCounter.VoterMessageId.Count - voteCounter.PlanNames.Count;
            sb.AppendFormat("Total No. of Voters: {0}\r\n", totalVoterCount);
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
            if (voteCounter.PlanNames.Contains(supporter))
            {
                sb.Append("[b]Plan: ");
                tail = "[/b]";
            }

            AddSupporterUrl(sb, supporter, quest);

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
        private string GenerateSupporterUrl(IQuest quest, string supporter, int rank)
        {
            StringBuilder sb = new StringBuilder();

            sb.AppendFormat("[{0}] ", rank);
            AddSupporterUrl(sb, supporter, quest);
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
        private void AddSupporterUrl(StringBuilder sb, string supporter, IQuest quest)
        {
            sb.Append("[url=\"");
            sb.Append(quest.GetForumAdapter().GetPostUrlFromId(quest.ThreadName, voteCounter.VoterMessageId[supporter]));
            sb.Append("\"]");
            sb.Append(supporter);
            sb.Append("[/url]");
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
