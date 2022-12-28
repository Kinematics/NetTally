using System;
using System.Collections.Generic;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;
using NetTally.Collections;
using NetTally.Global;
using NetTally.Types.Components;
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

        public ObservableCollectionExt<VoteLineBlock> AllVotesCollection { get; } = new();
        public ObservableCollectionExt<Origin> AllVotersCollection { get; } = new();
        public ObservableCollectionExt<string> TaskList => quest.VoteCounter.TaskList;
        public bool HasUndoActions => quest.VoteCounter.HasUndoActions;

        public bool HasTasks => TaskList.Count > 0;

        /// <summary>
        /// Update the observable collection of votes.
        /// </summary>
        private void UpdateVotesCollection()
        {
            AllVotesCollection.Replace(quest.VoteCounter.GetAllVotes());

            OnPropertyChanged(nameof(AllVotesCollection));
        }

        /// <summary>
        /// Update the observable collection of voters.
        /// </summary>
        private void UpdateVotersCollection()
        {
            AllVotersCollection.Replace(quest.VoteCounter.GetAllVoters());

            OnPropertyChanged(nameof(AllVotersCollection));
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

        public IEnumerable<Origin> GetVoterListForVote(VoteLineBlock vote) => quest.VoteCounter.GetVotersFor(vote);

        [ObservableProperty]
        [NotifyCanExecuteChangedFor(nameof(MergeCommand))]
        [NotifyCanExecuteChangedFor(nameof(DeleteCommand))]
        private VoteLineBlock? fromVote;
        [ObservableProperty]
        [NotifyCanExecuteChangedFor(nameof(MergeCommand))]
        [NotifyCanExecuteChangedFor(nameof(DeleteCommand))]
        private VoteLineBlock? toVote;
        [ObservableProperty]
        [NotifyCanExecuteChangedFor(nameof(JoinCommand))]
        private List<Origin> fromVoters = new();
        [ObservableProperty]
        [NotifyCanExecuteChangedFor(nameof(JoinCommand))]
        private Origin? toVoter;

        private bool CanMerge()
        {
            return (FromVote is not null &&
                    ToVote is not null &&
                    FromVote != ToVote);
        }

        [RelayCommand(CanExecute = nameof(CanMerge))]
        private void Merge()
        {
            if (FromVote is not null &&
                    ToVote is not null &&
                    FromVote != ToVote)
            {
                quest.VoteCounter.Merge(FromVote, ToVote);
                AllVotesCollection.Remove(FromVote);
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
            return (FromVote is not null &&
                    ToVote is not null &&
                    FromVote == ToVote);
        }

        [RelayCommand(CanExecute = nameof(CanDelete))]
        private void Delete()
        {
            if (FromVote is not null &&
                ToVote is not null &&
                FromVote == ToVote)
            {
                quest.VoteCounter.Delete(FromVote);
                AllVotesCollection.Remove(FromVote);
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
    }
}
