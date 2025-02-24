using System.Globalization;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;
using NetTally.Collections;
using NetTally.Configure;
using NetTally.Tally.Components.Counting;
using NetTally.Tally.Components.Posts;
using NetTally.Tally.Components.Votes;
using NetTally.Utility;

namespace NetTally.ViewModels
{
    public partial class ManageVotesViewModel : ObservableObject
    {
        private readonly ILogger<ManageVotesViewModel> logger;
        private readonly Quest quest;

        public ManageVotesViewModel(
            IQuestsInfo questsInfo,
            ILogger<ManageVotesViewModel> logger)
        {
            ArgumentNullException.ThrowIfNull(questsInfo.SelectedQuest, nameof(questsInfo.SelectedQuest));

            quest = questsInfo.SelectedQuest;
            this.logger = logger;

            UpdateVotesCollection();
            UpdateVotersCollection();
        }

        public ObservableCollectionExt<VoteBlockType> AllVotesCollection { get; } = [];
        public ObservableCollectionExt<Origin> AllVotersCollection { get; } = [];
        public ObservableCollectionExt<VoteTaskType> TaskList => quest.VoteCounter.TaskList;

        public bool HasUndoActions => quest.VoteCounter.HasUndoActions;
        public bool HasTasks => TaskList.Count > 0;


        #region Observable Vote List Properties
        /// <summary>
        /// Get the votes for the From side of the window.
        /// </summary>
        public IEnumerable<VoteBlockType> VotesFrom => AllVotesCollection
            .Where(FilterFromVote)
            .OrderBy(v => v, new VoteBlockComparer());

        /// <summary>
        /// Get the votes for the To side of the window.
        /// </summary>
        public IEnumerable<VoteBlockType> VotesTo => AllVotesCollection
            .Where(FilterToVote)
            .OrderBy(v => v, new VoteBlockComparer());

        /// <summary>
        /// Get the voters associated with the currently selected From vote (if any).
        /// </summary>
        public ObservableCollectionExt<Origin> VotersFrom { get; } = [];

        /// <summary>
        /// Get the voters associated with the currently selected To vote (if any).
        /// </summary>
        public ObservableCollectionExt<Origin> VotersTo { get; } = [];

        [ObservableProperty]
        [NotifyCanExecuteChangedFor(nameof(MergeCommand))]
        [NotifyCanExecuteChangedFor(nameof(DeleteCommand))]
        public partial VoteBlockType? SelectedFromVote { get; set; }

        partial void OnSelectedFromVoteChanged(VoteBlockType? value)
        {
            if (value is null)
            {
                VotersFrom.Clear();
                return;
            }

            VotersFrom.Replace(GetVotersForVote(value).Order(OriginComparer.Instance));
            JoinCommand.NotifyCanExecuteChanged();
        }

        [ObservableProperty]
        [NotifyCanExecuteChangedFor(nameof(MergeCommand))]
        [NotifyCanExecuteChangedFor(nameof(DeleteCommand))]
        public partial VoteBlockType? SelectedToVote { get; set; }

        partial void OnSelectedToVoteChanged(VoteBlockType? value)
        {
            if (value is null)
            {
                VotersTo.Clear();
                return;
            }

            VotersTo.Replace(GetVotersForVote(value).Order(OriginComparer.Instance));
            JoinCommand.NotifyCanExecuteChanged();
        }

        [ObservableProperty]
        [NotifyCanExecuteChangedFor(nameof(JoinCommand))]
        public partial Origin? SelectedToVoter { get; set; }

        /// <summary>
        /// Get the voters for a given vote.
        /// </summary>
        /// <param name="vote">The vote to get voters for.</param>
        /// <returns>A list of voter origins.</returns>
        public IEnumerable<Origin> GetVotersForVote(VoteBlockType? vote) =>
            (vote != null) ? quest.VoteCounter.GetUserVotersFor(vote) : [];

        #endregion Observable Vote List Properties

        #region Observable Filter Properties
        /// <summary>
        /// The filter to be applied to From votes.
        /// </summary>
        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(VotesFrom))]
        [NotifyPropertyChangedFor(nameof(VoteFromFilterEmpty))]
        public partial string VoteFromFilter { get; set; } = string.Empty;

        public bool VoteFromFilterEmpty => VoteFromFilter == string.Empty;

        /// <summary>
        /// The filter to be applied to To votes.
        /// </summary>
        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(VotesTo))]
        [NotifyPropertyChangedFor(nameof(VoteToFilterEmpty))]
        public partial string VoteToFilter { get; set; } = string.Empty;

        public bool VoteToFilterEmpty => VoteToFilter == string.Empty;

        /// <summary>
        /// Ensure that no unsafe values are entered into the filter.
        /// When the VoteFromFilter value changes, do a follow-up check to
        /// remove unsafe characters.
        /// </summary>
        /// <param name="value">The new VoteFromFilter value.</param>
        partial void OnVoteFromFilterChanged(string value)
        {
            VoteFromFilter = VoteFromFilter.RemoveUnsafeCharacters();
        }

        /// <summary>
        /// Ensure that no unsafe values are entered into the filter.
        /// When the VoteToFilter value changes, do a follow-up check to
        /// remove unsafe characters.
        /// </summary>
        /// <param name="value">The new VoteToFilter value.</param>
        partial void OnVoteToFilterChanged(string value)
        {
            VoteToFilter = VoteToFilter.RemoveUnsafeCharacters();
        }

        /// <summary>
        /// Filter function for From votes.
        /// </summary>
        /// <param name="vote">The vote being tested.</param>
        /// <returns>True if the vote should be displayed, or false if it should be hidden.</returns>
        private bool FilterFromVote(VoteBlockType vote)
        {
            return FilterVotes(VoteFromFilter, vote);
        }

        /// <summary>
        /// Filter function for To votes.
        /// </summary>
        /// <param name="vote">The vote being tested.</param>
        /// <returns>True if the vote should be displayed, or false if it should be hidden.</returns>
        private bool FilterToVote(VoteBlockType vote)
        {
            return FilterVotes(VoteToFilter, vote);
        }

        /// <summary>
        /// General filter function to show or hide votes in a listbox.
        /// Will allow it to be displayed if the contents of the vote, or any of the voters for the vote,
        /// match the provided filter.
        /// </summary>
        /// <param name="filter">The filter to apply.</param>
        /// <param name="vote">The vote to test.</param>
        /// <returns>True if the vote should be displayed, or false if it should be hidden.</returns>
        private bool FilterVotes(string filter, VoteBlockType vote)
        {
            if (string.IsNullOrEmpty(filter))
                return true;

            bool matchVote = CultureInfo.InvariantCulture.CompareInfo
                .IndexOf(VoteBlockDisplay.ToComparableString(vote), filter, CompareOptions.IgnoreCase) >= 0;

            bool matchAnyVoter = GetVotersForVote(vote)
                        .Any(v => CultureInfo.InvariantCulture.CompareInfo
                            .IndexOf(v.Author.Name, filter, CompareOptions.IgnoreCase) >= 0);

            return matchVote || matchAnyVoter;
        }
        #endregion Observable Filter Properties

        #region Collection Updates
        private void NotifyVotesChanged()
        {
            OnPropertyChanged(nameof(AllVotesCollection));
            OnPropertyChanged(nameof(VotesFrom));
            OnPropertyChanged(nameof(VotesTo));
        }

        private void NotifyVotersChanged()
        {
            OnPropertyChanged(nameof(AllVotersCollection));
            OnPropertyChanged(nameof(VotersFrom));
            OnPropertyChanged(nameof(VotersTo));
        }

        /// <summary>
        /// Update the observable collection of votes.
        /// </summary>
        private void UpdateVotesCollection()
        {
            AllVotesCollection.Replace(quest.VoteCounter.GetAllVotes());
            NotifyVotesChanged();
        }

        /// <summary>
        /// Update the observable collection of voters.
        /// </summary>
        private void UpdateVotersCollection()
        {
            AllVotersCollection.Replace(quest.VoteCounter.GetAllVoters());
            NotifyVotersChanged();
        }

        private void NotifyUndoChanged()
        {
            OnPropertyChanged(nameof(HasUndoActions));
            UndoCommand.NotifyCanExecuteChanged();
        }
        #endregion Collection Updates

        #region Commands
        public void ReplaceTask(VoteBlockType selectedVote, string newTask)
        {
            var task = VoteTask.Create(newTask);
            quest.VoteCounter.ReplaceTask(selectedVote, task);
            UpdateVotesCollection();
        }

        public void PartitionChildren(VoteBlockType selectedVote)
        {
            quest.VoteCounter.Split(selectedVote, VoteConstructor.PartitionChildren(selectedVote));
            UpdateVotesCollection();
        }

        public void AddUserDefinedTask(string newTask)
        {
            var task = VoteTask.Create(newTask);
            quest.VoteCounter.AddUserDefinedTask(task);
        }


        private bool CanMerge()
        {
            return (SelectedFromVote is not null &&
                    SelectedToVote is not null &&
                    SelectedFromVote != SelectedToVote);
        }

        [RelayCommand(CanExecute = nameof(CanMerge))]
        private void Merge()
        {
            if (SelectedFromVote is not null &&
                    SelectedToVote is not null &&
                    SelectedFromVote != SelectedToVote)
            {
                if (quest.VoteCounter.Merge(SelectedFromVote, SelectedToVote))
                {
                    AllVotesCollection.Remove(SelectedFromVote);
                    NotifyVotesChanged();
                    OnSelectedToVoteChanged(SelectedToVote);
                    NotifyVotersChanged();
                    NotifyUndoChanged();
                }
            }
        }

        private bool CanJoin()
        {
            return (VotersFrom.Count > 0 &&
                    SelectedToVoter is not null);
        }

        [RelayCommand(CanExecute = nameof(CanJoin))]
        private void Join()
        {
            if (VotersFrom.Count > 0 &&
                SelectedToVoter is not null)
            {
                if (quest.VoteCounter.Join([.. VotersFrom], SelectedToVoter))
                {
                    UpdateVotesCollection();
                    OnSelectedToVoteChanged(SelectedToVote);
                    UpdateVotersCollection();
                    NotifyUndoChanged();
                }
            }
        }

        private bool CanDelete()
        {
            return (SelectedFromVote is not null &&
                    SelectedToVote is not null &&
                    SelectedFromVote == SelectedToVote);
        }

        [RelayCommand(CanExecute = nameof(CanDelete))]
        private void Delete()
        {
            if (SelectedFromVote is not null &&
                SelectedToVote is not null &&
                SelectedFromVote == SelectedToVote)
            {
                if (quest.VoteCounter.Delete(SelectedFromVote))
                {
                    AllVotesCollection.Remove(SelectedFromVote);
                    NotifyVotesChanged();
                    NotifyUndoChanged();
                }
            }
        }

        private bool CanUndo()
        {
            return HasUndoActions;
        }

        [RelayCommand(CanExecute = nameof(CanUndo))]
        private void Undo()
        {
            if (quest.VoteCounter.Undo())
            {
                UpdateVotesCollection();
                OnSelectedFromVoteChanged(SelectedFromVote);
                OnSelectedToVoteChanged(SelectedToVote);
                UpdateVotersCollection();
                NotifyUndoChanged();
            }
        }

        [RelayCommand]
        private static void RunTest(VoteBlockType? voteLines)
        {
            // context menu experiment.
            if (voteLines is not null)
            {

            }
        }
        #endregion Commands
    }
}
