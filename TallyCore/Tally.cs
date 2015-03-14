using System;
using System.ComponentModel;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace NetTally
{
    public class Tally : INotifyPropertyChanged
    {
        IPageProvider pageProvider;
        IVoteCounter voteCounter;

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
        string results = string.Empty;
        /// <summary>
        /// Property for the string containing the current tally progress or results.
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
                IForumAdapter forumAdapter = ForumAdapterFactory.GetAdapter(quest);

                TallyResults = string.Empty;

                // Load pages from the website
                var pages = await pageProvider.LoadPages(forumAdapter, quest, token).ConfigureAwait(false);

                // Tally the votes from the loaded pages.
                voteCounter.TallyVotes(forumAdapter, quest, pages);

                // Compose the final result string from the compiled votes.
                ConstructResults(forumAdapter);

            }
            catch (NotImplementedException)
            {
                throw;
            }
            finally
            {

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

        #region Local class functions
        /// <summary>
        /// Compose the tallied results into a string to put in the TallyResults property,
        /// for display in the UI.
        /// </summary>
        private void ConstructResults(IForumAdapter forumAdapter)
        {
            StringBuilder sb = new StringBuilder();

            var assembly = Assembly.GetExecutingAssembly();
            var product = (AssemblyProductAttribute)assembly.GetCustomAttribute(typeof(AssemblyProductAttribute));
            var version = (AssemblyInformationalVersionAttribute)assembly.GetCustomAttribute(typeof(AssemblyInformationalVersionAttribute));

            sb.AppendLine("[b]Vote Tally[/b]");
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


                foreach (var supporter in vote.Value)
                {
                    sb.Append("[url=\"");
                    sb.Append(forumAdapter.GetPostUrlFromId(voteCounter.VoterMessageId[supporter]));
                    sb.Append("\"]");
                    sb.Append(supporter);
                    sb.AppendLine("[/url]");
                }

                sb.AppendLine("");
            }

            TallyResults = sb.ToString();
        }
        #endregion
    }        
}
