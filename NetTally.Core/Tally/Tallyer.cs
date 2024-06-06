using System;
using System.Threading;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.Extensions.Logging;
using NetTally.CustomEventArgs;
using NetTally.Input.Forums.Reading;
using NetTally.Output;

namespace NetTally.VoteCounting
{
    /// <summary>
    /// Class that links together the various pieces of the tally system.
    /// Call this to run a tally.
    /// </summary>
    public partial class Tallyer : ObservableObject
    {
        #region Construction
        private readonly IForumReader forumReader;
        private readonly ITextResultsProviderF textResultsProvider;
        private readonly ILogger<Tallyer> logger;

        public Tallyer(IForumReader forumReader,
                     ITextResultsProviderF textResultsProvider,
                     ILogger<Tallyer> logger)
        {
            this.forumReader = forumReader;
            this.textResultsProvider = textResultsProvider;
            this.logger = logger;

            this.forumReader.StatusChanged += ForumReader_StatusChanged;
        }
        #endregion

        #region Current Properties
        /// <summary>
        /// The string containing the current tally progress or results.
        /// Creates a notification event if the contents change.
        /// If it changes to or from an empty string, the HasTallyResults property also changes.
        /// </summary>
        [ObservableProperty]
        private string tallyResults = string.Empty;

        partial void OnTallyResultsChanged(string? oldValue, string newValue)
        {
            if (string.IsNullOrEmpty(oldValue) || string.IsNullOrEmpty(newValue))
            {
                OnPropertyChanged(nameof(HasTallyResults));
            }
        }

        public bool HasTallyResults => !string.IsNullOrEmpty(TallyResults);
        #endregion

        #region Public Methods
        public async Task RunTallyAsync(Quest quest, CancellationToken cancellationToken)
        {
            TallyResults = string.Empty;

            try
            {
                var (Titles, Posts) = await forumReader.ReadQuestAsync(quest, cancellationToken)
                                            .ConfigureAwait(false);

                quest.ConstructVotes(Titles, Posts);

                UpdateOutput(quest);

                logger.LogInformation("Tally for quest {questName} completed.", quest.DisplayName);
            }
            finally
            {
                // Free memory used by loading pages as soon as we're done:
                GC.Collect();
            }
        }

        public void UpdateTally(Quest quest)
        {
            quest.ConstructVotes();
            UpdateOutput(quest);
        }

        public void UpdateOutput(Quest quest)
        {
            TallyResults = textResultsProvider.BuildOutput(quest);
        }

        public void ClearTallyResults()
        {
            TallyResults = string.Empty;
        }
        #endregion

        #region Events
        /// <summary>
        /// Keep watch for any status messasges from the forum reader, and add them
        /// to the TallyResults string so that they can be displayed in the UI.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e">Contains the text to be added to the output.</param>
        private void ForumReader_StatusChanged(object? sender, MessageEventArgs e)
        {
            if (!string.IsNullOrEmpty(e.Message))
            {
                TallyResults += e.Message;
            }
        }
        #endregion Events
    }
}
