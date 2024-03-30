using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;
using NetTally.Collections;
using NetTally.Global;
using NetTally.Types.Components;
using NetTally.Utility;
using NetTally.Votes;

namespace NetTally.ViewModels
{
    public partial class ManageVotesViewModel : ObservableObject
    {
        private readonly VoteConstructor voteConstructor;
        private readonly ILogger<ManageVotesViewModel> logger;
        private readonly Quest quest;

        public ManageVotesViewModel(
            IQuestsInfo questsInfo,
            VoteConstructor voteConstructor,
            ILogger<ManageVotesViewModel> logger)
        {
            ArgumentNullException.ThrowIfNull(questsInfo.SelectedQuest, nameof(questsInfo.SelectedQuest));

            quest = questsInfo.SelectedQuest;
            this.voteConstructor = voteConstructor;
            this.logger = logger;
        }

        public ObservableCollectionExt<VoteLineBlock> AllVotesCollection { get; } = [];
        public ObservableCollectionExt<Origin> AllVotersCollection { get; } = [];
        public ObservableCollectionExt<string> TaskList => quest.VoteCounter.TaskList;

        public bool HasUndoActions => quest.VoteCounter.HasUndoActions;
        public bool HasTasks => TaskList.Count > 0;


        #region Observable Vote List Properties
        /// <summary>
        /// Get the votes for the From side of the window.
        /// </summary>
        public IEnumerable<VoteLineBlock> VotesFrom => AllVotesCollection
            .Order()
            .Where(FilterFromVote);

        /// <summary>
        /// Get the votes for the To side of the window.
        /// </summary>
        public IEnumerable<VoteLineBlock> VotesTo => AllVotesCollection
            .Order()
            .Where(FilterToVote);

        /// <summary>
        /// Filter function for From votes.
        /// </summary>
        /// <param name="vote">The vote being tested.</param>
        /// <returns>True if the vote should be displayed, or false if it should be hidden.</returns>
        private bool FilterFromVote(VoteLineBlock vote)
        {
            return FilterVotes(VoteFromFilter, vote);
        }

        /// <summary>
        /// Filter function for To votes.
        /// </summary>
        /// <param name="vote">The vote being tested.</param>
        /// <returns>True if the vote should be displayed, or false if it should be hidden.</returns>
        private bool FilterToVote(VoteLineBlock vote)
        {
            return FilterVotes(VoteToFilter, vote);
        }

        /// <summary>
        /// General filter function to show or hide votes in a listbox.
        /// </summary>
        /// <param name="filter">The filter to apply.</param>
        /// <param name="vote">The vote to test.</param>
        /// <returns>True if the vote should be displayed, or false if it should be hidden.</returns>
        private bool FilterVotes(string filter, VoteLineBlock vote)
        {
            if (string.IsNullOrEmpty(filter))
                return true;

            return CultureInfo.InvariantCulture.CompareInfo
                .IndexOf(vote.ToComparableString(), filter, CompareOptions.IgnoreCase) >= 0;
        }

        /// <summary>
        /// Get the voters associated with the currently selected From vote (if any).
        /// </summary>
        public IEnumerable<Origin> VotersFrom =>
            GetVotersForVote(SelectedFromVote).Order();

        /// <summary>
        /// Get the voters associated with the currently selected To vote (if any).
        /// </summary>
        public IEnumerable<Origin> VotersTo =>
            GetVotersForVote(SelectedToVote).Order();

        /// <summary>
        /// Get the voters for a given vote.
        /// </summary>
        /// <param name="vote">The vote to get voters for.</param>
        /// <returns>A list of voter origins.</returns>
        public IEnumerable<Origin> GetVotersForVote(VoteLineBlock? vote) =>
            (vote != null) ? quest.VoteCounter.GetVotersFor(vote) : [];
        #endregion Observable Vote List Properties

        #region Observable Filter Properties
        /// <summary>
        /// The filter to be applied to From votes.
        /// </summary>
        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(VotesFrom))]
        private string voteFromFilter = string.Empty;

        /// <summary>
        /// The filter to be applied to To votes.
        /// </summary>
        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(VotesTo))]
        private string voteToFilter = string.Empty;

        /// <summary>
        /// Ensure that no unsafe values are entered into the filter.
        /// When the VoteFromFilter value changes, do a follow-up check to
        /// remove unsafe characters.
        /// </summary>
        /// <param name="value">The new VoteFromFilter value.</param>
        partial void OnVoteFromFilterChanged(string value)
        {
            // Modify the backing field directly, so that it does not trigger
            // a new round of modifying the property.
#pragma warning disable MVVMTK0034 // Direct field reference to [ObservableProperty] backing field
            voteFromFilter = voteFromFilter.RemoveUnsafeCharacters();
#pragma warning restore MVVMTK0034 // Direct field reference to [ObservableProperty] backing field
        }

        /// <summary>
        /// Ensure that no unsafe values are entered into the filter.
        /// When the VoteToFilter value changes, do a follow-up check to
        /// remove unsafe characters.
        /// </summary>
        /// <param name="value">The new VoteToFilter value.</param>
        partial void OnVoteToFilterChanged(string value)
        {
            // Modify the backing field directly, so that it does not trigger
            // a new round of modifying the property.
#pragma warning disable MVVMTK0034 // Direct field reference to [ObservableProperty] backing field
            voteToFilter = voteToFilter.RemoveUnsafeCharacters();
#pragma warning restore MVVMTK0034 // Direct field reference to [ObservableProperty] backing field
        }
        #endregion Observable Filter Properties






        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(VotesFrom))]
        [NotifyCanExecuteChangedFor(nameof(MergeCommand))]
        [NotifyCanExecuteChangedFor(nameof(DeleteCommand))]
        private VoteLineBlock? selectedFromVote;

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(VotesTo))]
        [NotifyCanExecuteChangedFor(nameof(MergeCommand))]
        [NotifyCanExecuteChangedFor(nameof(DeleteCommand))]
        private VoteLineBlock? selectedToVote;

        [ObservableProperty]
        [NotifyCanExecuteChangedFor(nameof(JoinCommand))]
        private List<Origin> fromVoters = [];

        [ObservableProperty]
        [NotifyCanExecuteChangedFor(nameof(JoinCommand))]
        private List<Origin> toVoters = [];

        [ObservableProperty]
        [NotifyCanExecuteChangedFor(nameof(JoinCommand))]
        private Origin? toVoter;


        /// <summary>
        /// Update the observable collection of votes.
        /// </summary>
        private void UpdateVotesCollection()
        {
            AllVotesCollection.Replace(quest.VoteCounter.GetAllVotes());

            OnPropertyChanged(nameof(AllVotesCollection));
            OnPropertyChanged(nameof(VotesFrom));
            OnPropertyChanged(nameof(VotesTo));
        }

        /// <summary>
        /// Update the observable collection of voters.
        /// </summary>
        private void UpdateVotersCollection()
        {
            AllVotersCollection.Replace(quest.VoteCounter.GetAllVoters());

            OnPropertyChanged(nameof(AllVotersCollection));
            OnPropertyChanged(nameof(VotersFrom));
            OnPropertyChanged(nameof(VotersTo));
        }

        public void ReplaceTask(VoteLineBlock selectedVote, string newTask)
        {
            quest.VoteCounter.ReplaceTask(selectedVote, newTask);
        }

        public void PartitionChildren(VoteLineBlock selectedVote)
        {
            quest.VoteCounter.Split(selectedVote, voteConstructor.PartitionChildren(selectedVote));
        }

        public void AddUserDefinedTask(string newTask)
        {
            quest.VoteCounter.AddUserDefinedTask(newTask);
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
                quest.VoteCounter.Merge(SelectedFromVote, SelectedToVote);
                AllVotesCollection.Remove(SelectedFromVote);
            }
        }

        private bool CanJoin()
        {
            return (FromVoters is not null &&
                    FromVoters.Count > 0 &&
                    ToVoter is not null);
        }

        [RelayCommand(CanExecute = nameof(CanJoin))]
        private void Join()
        {
            if (FromVoters is not null &&
                FromVoters.Count > 0 &&
                ToVoter is not null)
            {
                quest.VoteCounter.Join(FromVoters, ToVoter);
                UpdateVotesCollection();
                UpdateVotersCollection();
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
                quest.VoteCounter.Delete(SelectedFromVote);
                AllVotesCollection.Remove(SelectedFromVote);
            }
        }

        private bool CanUndo()
        {
            return quest.VoteCounter.HasUndoActions;
        }

        [RelayCommand(CanExecute = nameof(CanUndo))]
        private void Undo()
        {
            quest.VoteCounter.Undo();
            UpdateVotesCollection();
            UpdateVotersCollection();
        }

        [RelayCommand]
        private void RunTest(VoteLineBlock? voteLines)
        {
            if (voteLines is not null)
            {

            }
        }
    }
}
