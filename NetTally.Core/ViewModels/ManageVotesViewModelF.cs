using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;
using NetTally.Collections;
using NetTally.Configure;
using NetTally.Tally.ComponentsF.Counting;
using NetTally.Tally.ComponentsF.Posts;
using NetTally.Tally.ComponentsF.Votes;
using NetTally.Utility;

namespace NetTally.ViewModels
{
    public partial class ManageVotesViewModelF : ObservableObject
    {
        private readonly ILogger<ManageVotesViewModelF> logger;
        private readonly Quest quest;

        public ManageVotesViewModelF(
            IQuestsInfo questsInfo,
            ILogger<ManageVotesViewModelF> logger)
        {
            ArgumentNullException.ThrowIfNull(questsInfo.SelectedQuest, nameof(questsInfo.SelectedQuest));

            quest = questsInfo.SelectedQuest;
            this.logger = logger;

            UpdateVotesCollection();
            UpdateVotersCollection();
        }

        public ObservableCollectionExt<VoteBlockType> AllVotesCollection { get; } = [];
        public ObservableCollectionExt<OriginType> AllVotersCollection { get; } = [];
        public ObservableCollectionExt<VoteTaskType> TaskList => quest.VoteCounterF.TaskList;

        public bool HasUndoActions => quest.VoteCounterF.HasUndoActions;
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
        public ObservableCollectionExt<OriginType> VotersFrom { get; } = [];

        /// <summary>
        /// Get the voters associated with the currently selected To vote (if any).
        /// </summary>
        public ObservableCollectionExt<OriginType> VotersTo { get; } = [];

        [ObservableProperty]
        [NotifyCanExecuteChangedFor(nameof(MergeCommand))]
        [NotifyCanExecuteChangedFor(nameof(DeleteCommand))]
        private VoteBlockType? selectedFromVote;

        partial void OnSelectedFromVoteChanged(VoteBlockType? value)
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
        private VoteBlockType? selectedToVote;

        partial void OnSelectedToVoteChanged(VoteBlockType? value)
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
        private OriginType? selectedToVoter;

        /// <summary>
        /// Get the voters for a given vote.
        /// </summary>
        /// <param name="vote">The vote to get voters for.</param>
        /// <returns>A list of voter origins.</returns>
        public IEnumerable<OriginType> GetVotersForVote(VoteBlockType? vote) =>
            (vote != null) ? quest.VoteCounterF.GetVotersFor(vote) : [];

        #endregion Observable Vote List Properties

        #region Observable Filter Properties
        /// <summary>
        /// The filter to be applied to From votes.
        /// </summary>
        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(VotesFrom))]
        [NotifyPropertyChangedFor(nameof(VoteFromFilterEmpty))]
        private string voteFromFilter = string.Empty;

        public bool VoteFromFilterEmpty => VoteFromFilter == string.Empty;

        /// <summary>
        /// The filter to be applied to To votes.
        /// </summary>
        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(VotesTo))]
        [NotifyPropertyChangedFor(nameof(VoteToFilterEmpty))]
        private string voteToFilter = string.Empty;

        public bool VoteToFilterEmpty => VoteToFilter == string.Empty;

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
            AllVotesCollection.Replace(quest.VoteCounterF.GetAllVotes());
            NotifyVotesChanged();
        }

        /// <summary>
        /// Update the observable collection of voters.
        /// </summary>
        private void UpdateVotersCollection()
        {
            AllVotersCollection.Replace(quest.VoteCounterF.GetAllVoters());
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
            quest.VoteCounterF.ReplaceTask(selectedVote, task);
            UpdateVotesCollection();
        }

        public void PartitionChildren(VoteBlockType selectedVote)
        {
            quest.VoteCounterF.Split(selectedVote, VoteConstructor.PartitionChildren(selectedVote));
            UpdateVotesCollection();
        }

        public void AddUserDefinedTask(string newTask)
        {
            var task = VoteTask.Create(newTask);
            quest.VoteCounterF.AddUserDefinedTask(task);
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
                if (quest.VoteCounterF.Merge(SelectedFromVote, SelectedToVote))
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
                if (quest.VoteCounterF.Join([.. VotersFrom], SelectedToVoter))
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
                if (quest.VoteCounterF.Delete(SelectedFromVote))
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
            if (quest.VoteCounterF.Undo())
            {
                UpdateVotesCollection();
                OnSelectedFromVoteChanged(SelectedFromVote);
                OnSelectedToVoteChanged(SelectedToVote);
                UpdateVotersCollection();
                NotifyUndoChanged();
            }
        }

        [RelayCommand]
        private void RunTest(VoteBlockType? voteLines)
        {
            // context menu experiment.
            if (voteLines is not null)
            {

            }
        }
        #endregion Commands
    }
}
