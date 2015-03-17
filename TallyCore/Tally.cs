using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;
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

        #endregion

        #region Interface functions
        /// <summary>
        /// Run the actual tally.
        /// </summary>
        /// <param name="questTitle">The name of the quest thread to scan.</param>
        /// <param name="startPost">The starting post number.</param>
        /// <param name="endPost">The ending post number.</param>
        /// <returns></returns>
        public async Task Run(IQuest quest, CancellationToken token)
        {
            try
            {
                TallyResults = string.Empty;
                lastTallyQuest = quest;

                await quest.GetForumAdapter(token);

                // Load pages from the website
                loadedPages = await pageProvider.LoadPages(quest, token).ConfigureAwait(false);

                UpdateTally(quest);
            }
            catch (Exception)
            {
                lastTallyQuest = null;
                loadedPages.Clear();
                loadedPages = null;
                throw;
            }
            finally
            {

            }
        }

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
        private void ConstructResults(IQuest quest)
        {
            if (quest == null)
                return;

            StringBuilder sb = new StringBuilder();

            var assembly = Assembly.GetExecutingAssembly();
            var product = (AssemblyProductAttribute)assembly.GetCustomAttribute(typeof(AssemblyProductAttribute));
            var version = (AssemblyInformationalVersionAttribute)assembly.GetCustomAttribute(typeof(AssemblyInformationalVersionAttribute));

            sb.AppendFormat("[b]Vote Tally[/b] : {0}", voteCounter.Title);
            sb.AppendLine("");
            sb.AppendFormat("[color=transparent]##### {0} {1}[/color]",
                product.Product,
                version.InformationalVersion);
            sb.AppendLine("");

            foreach (var vote in voteCounter.VotesWithSupporters)
            {
                sb.Append(vote.Key);

                sb.Append("[b]No. of Votes: ");
                sb.Append(vote.Value.Count);
                sb.AppendLine("[/b]");

                if (UseSpoilerForVoters)
                {
                    sb.AppendLine("[spoiler=Voters]");
                }

                sb.Append(GenerateSupporterUrl(quest, vote.Value.First()));

                var remainder = vote.Value.Skip(1);

                foreach (var supporter in vote.Value.Skip(1).OrderBy(v => v))
                    sb.Append(GenerateSupporterUrl(quest, supporter));

                if (UseSpoilerForVoters)
                {
                    sb.AppendLine("[/spoiler]");
                }

                sb.AppendLine("");
            }

            TallyResults = sb.ToString();
        }

        private string GenerateSupporterUrl(IQuest quest, string supporter)
        {
            StringBuilder sb = new StringBuilder();

            sb.Append("[url=\"");
            sb.Append(quest.GetForumAdapter().GetPostUrlFromId(quest.ThreadName, voteCounter.VoterMessageId[supporter]));
            sb.Append("\"]");
            sb.Append(supporter);
            sb.AppendLine("[/url]");

            return sb.ToString();
        }

        #endregion

    }
}
