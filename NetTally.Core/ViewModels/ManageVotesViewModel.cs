using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;
using NetTally.Collections;
using NetTally.Comparers;
using NetTally.Global;
using NetTally.Tally.Components;
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

            UpdateVotesCollection();
            UpdateVotersCollection();
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
            .Where(FilterFromVote)
            .OrderBy(v => v, new CustomVoteComparer());

        /// <summary>
        /// Get the votes for the To side of the window.
        /// </summary>
        public IEnumerable<VoteLineBlock> VotesTo => AllVotesCollection
            .Where(FilterToVote)
            .OrderBy(v => v, new CustomVoteComparer());

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
        private VoteLineBlock? selectedFromVote;

        partial void OnSelectedFromVoteChanged(VoteLineBlock? value)
        {
            if (value is null)
            {
                VotersFrom.Clear();
                return;
            }

            VotersFrom.Replace(GetVotersForVote(value).Order());
            JoinCommand.NotifyCanExecuteChanged();
        }

        [ObservableProperty]
        [NotifyCanExecuteChangedFor(nameof(MergeCommand))]
        [NotifyCanExecuteChangedFor(nameof(DeleteCommand))]
        private VoteLineBlock? selectedToVote;

        partial void OnSelectedToVoteChanged(VoteLineBlock? value)
        {
            if (value is null)
            {
                VotersTo.Clear();
                return;
            }

            VotersTo.Replace(GetVotersForVote(value).Order());
            JoinCommand.NotifyCanExecuteChanged();
        }

        [ObservableProperty]
        [NotifyCanExecuteChangedFor(nameof(JoinCommand))]
        private Origin? selectedToVoter;

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
        /// Will allow it to be displayed if the contents of the vote, or any of the voters for the vote,
        /// match the provided filter.
        /// </summary>
        /// <param name="filter">The filter to apply.</param>
        /// <param name="vote">The vote to test.</param>
        /// <returns>True if the vote should be displayed, or false if it should be hidden.</returns>
        private bool FilterVotes(string filter, VoteLineBlock vote)
        {
            if (string.IsNullOrEmpty(filter))
                return true;

            bool matchVote = CultureInfo.InvariantCulture.CompareInfo
                .IndexOf(vote.ToComparableString(), filter, CompareOptions.IgnoreCase) >= 0;

            bool matchAnyVoter = GetVotersForVote(vote)
                        .Any(v => CultureInfo.InvariantCulture.CompareInfo
                            .IndexOf(v.Author.Name, filter, CompareOptions.IgnoreCase) >= 0);

            return matchVote || matchAnyVoter;
        }
        #endregion Observable Filter Properties

        #region Collection Updates
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
        #endregion Collection Updates

        #region Commands
        public void ReplaceTask(VoteLineBlock selectedVote, string newTask)
        {
            quest.VoteCounter.ReplaceTask(selectedVote, newTask);
            UpdateVotesCollection();
        }

        public void PartitionChildren(VoteLineBlock selectedVote)
        {
            quest.VoteCounter.Split(selectedVote, VoteConstructor.PartitionChildren(selectedVote));
            UpdateVotesCollection();
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
            return (VotersFrom.Count > 0 &&
                    SelectedToVoter is not null);
        }

        [RelayCommand(CanExecute = nameof(CanJoin))]
        private void Join()
        {
            if (VotersFrom.Count > 0 &&
                SelectedToVoter is not null)
            {
                quest.VoteCounter.Join([.. VotersFrom], SelectedToVoter);
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
            // context menu experiment.
            if (voteLines is not null)
            {

            }
        }
        #endregion Commands

        //[ObservableProperty]
        //[NotifyCanExecuteChangedFor(nameof(JoinCommand))]
        //[Obsolete]
        //private List<Origin> fromVoters = [];

        //[ObservableProperty]
        //[NotifyCanExecuteChangedFor(nameof(JoinCommand))]
        //[Obsolete]
        //private Origin? toVoter;
    }
}
