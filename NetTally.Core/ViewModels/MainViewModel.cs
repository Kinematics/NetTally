using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;
using NetTally.Cache;
using NetTally.CustomEventArgs;
using NetTally.Extensions;
using NetTally.Global;
using NetTally.Types.Enums;
using NetTally.VoteCounting;

namespace NetTally.ViewModels
{
    public partial class MainViewModel : ObservableObject
    {
        private readonly IQuestsInfoMod questsInfo;
        private readonly Tally tally;
        private readonly ICache<string> pageCache;
        private readonly ILogger<MainViewModel> logger;

        public MainViewModel(
            IQuestsInfoMod questsInfo,
            Tally tally,
            ICache<string> cache,
            ILogger<MainViewModel> logger)
        {
            this.logger = logger;
            this.questsInfo = questsInfo;
            this.tally = tally;
            this.pageCache = cache;
            SelectedQuest = questsInfo.SelectedQuest;

            System.Net.ServicePointManager.DefaultConnectionLimit = 4;
            System.Net.ServicePointManager.Expect100Continue = true;
            System.Net.ServicePointManager.SecurityProtocol = System.Net.SecurityProtocolType.Tls12;

            RunTallyCommand.PropertyChanged += RunTallyCommand_PropertyChanged;
            tally.PropertyChanged += Tally_PropertyChanged;
        }

        #region View Model Properties
        public ObservableCollection<Quest> Quests => questsInfo.Quests;
        public bool HasQuests => Quests.Count > 0;

        [ObservableProperty]
        private bool hasNewRelease;

        public List<string> DisplayModes { get; } = EnumExtensions.EnumDescriptionsList<DisplayMode>().ToList();

        public List<string> PartitionModes { get; } = EnumExtensions.EnumDescriptionsList<PartitionMode>().ToList();

        public List<string> RankVoteCountingModes { get; } = EnumExtensions.EnumDescriptionsList<RankVoteCounterMethod>().ToList();

        public string Output => tally.TallyResults;

        public bool HasOutput => tally.HasTallyResults;

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(IsQuestSelected))]
        [NotifyCanExecuteChangedFor(nameof(RunTallyCommand))]
        [NotifyCanExecuteChangedFor(nameof(RemoveQuestCommand))]
        [NotifyCanExecuteChangedFor(nameof(ClearTallyCacheCommand))]
        private Quest? selectedQuest;

        partial void OnSelectedQuestChanging(Quest? value)
        {
            if (SelectedQuest is not null)
            {
                SelectedQuest.PropertyChanged -= Quest_PropertyChanged;
            }
        }

        partial void OnSelectedQuestChanged(Quest? value)
        {
            questsInfo.SelectedQuest = value;

            if (value is not null)
            {
                value.PropertyChanged += Quest_PropertyChanged;
            }
        }

        public bool IsQuestSelected => SelectedQuest != null;
        #endregion View Model Properties

        #region Utility Functions
        public async Task UpdateOutput()
        {
            if (SelectedQuest is not null)
                await tally.UpdateResults(SelectedQuest);
        }

        public async Task UpdateTally()
        {
            if (SelectedQuest is not null)
                await tally.UpdateResults(SelectedQuest);
        }

        public List<Quest> GetLinkedQuests(Quest quest)
        {
            return Quests.Where(q => quest.HasLinkedQuest(q)).ToList();
        }
        #endregion Utility Functions

        #region Event Handling
        private void Tally_PropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(Tally.TallyResults))
            {
                OnPropertyChanged(nameof(Output));
            }
            else if (e.PropertyName == nameof(Tally.HasTallyResults))
            {
                OnPropertyChanged(nameof(HasOutput));
            }
            //else if (e is PropertyDataChangedEventArgs<string> eData)
            //{
            //    if (eData.PropertyName == "TallyResultsStatusChanged")
            //    {
            //        OnPropertyDataChanged(eData.PropertyData, eData.PropertyName);
            //    }
            //}
        }

        private async void Quest_PropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (sender is not Quest quest)
                return;

            switch (e.PropertyName)
            {
                case nameof(quest.DisplayName):
                    break;
                default:
                    await tally.UpdateResults(quest);
                    break;
            }
        }
        #endregion Event Handling

        #region View Model Commands
        private bool CanAddQuest => TallyIsNotRunning;

        [RelayCommand(CanExecute = nameof(CanAddQuest))]
        private void AddQuest()
        {
            bool hadZeroQuests = Quests.Count == 0;

            SelectedQuest = questsInfo.CreateQuest();
            
            if (hadZeroQuests)
                OnPropertyChanged(nameof(HasQuests));
            
            logger.LogInformation("Added new quest");
        }

        private bool CanRemoveQuest() => TallyIsNotRunning && IsQuestSelected;

        [RelayCommand(CanExecute = nameof(CanRemoveQuest))]
        private void RemoveQuest()
        {
            if (SelectedQuest != null)
            {
                logger.LogInformation("Removing quest for thread: {url}", SelectedQuest.ThreadName);

                int position = Quests.IndexOf(SelectedQuest);

                if (questsInfo.RemoveQuest(SelectedQuest))
                {
                    if (Quests.Count == 0)
                    {
                        OnPropertyChanged(nameof(HasQuests));
                        logger.LogInformation("There are no remaining quests.");
                    }
                    else
                    {
                        position = Math.Min(position, Quests.Count - 1);
                        SelectedQuest = Quests[position];
                        logger.LogInformation("Selected quest updated to thread: {url}", SelectedQuest.ThreadName);
                    }
                }
                else
                {
                    logger.LogWarning("Failed to remove quest for thread: {url}", SelectedQuest.ThreadName);
                }
            }
        }


        public bool TallyIsRunning => RunTallyCommand.IsRunning;
        public bool TallyIsNotRunning => !RunTallyCommand.IsRunning;

        private void RunTallyCommand_PropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(RunTallyCommand.IsRunning))
            {
                AddQuestCommand.NotifyCanExecuteChanged();
                RemoveQuestCommand.NotifyCanExecuteChanged();
                CancelTallyCommand.NotifyCanExecuteChanged();
                ClearTallyCacheCommand.NotifyCanExecuteChanged();
                OnPropertyChanged(nameof(TallyIsRunning));
                OnPropertyChanged(nameof(TallyIsNotRunning));

                if (RunTallyCommand.ExecutionTask?.IsCompletedSuccessfully ?? false)
                {
                    OnPropertyChanged(nameof(Output));
                }
            }
        }

        private bool CanRunTally() => TallyIsNotRunning && IsQuestSelected;

        [RelayCommand(CanExecute = nameof(CanRunTally),
            IncludeCancelCommand = true)]
        private async Task RunTally(CancellationToken cancellationToken)
        {
            try
            {
                await tally.RunAsync(SelectedQuest!, cancellationToken).ConfigureAwait(false);
            }
            catch (Exception e) when (e is TaskCanceledException or OperationCanceledException)
            {
                if (RunTallyCommand.IsCancellationRequested)
                {
                    tally.TallyResults += "Tally cancelled!\n";
                }
                else
                {
                    RunTallyCommand.Cancel();
                }
            }
        }

        private bool CanCancelTally() => TallyIsRunning;

        [RelayCommand(CanExecute = nameof(CanCancelTally))]
        private void CancelTally()
        {
            RunTallyCommand.Cancel();
        }

        private bool CanClearTallyCache() => TallyIsNotRunning && IsQuestSelected;

        [RelayCommand(CanExecute = nameof(CanClearTallyCache))]
        private void ClearTallyCache()
        {
            pageCache.Clear();
            SelectedQuest?.VoteCounter.ResetUserMerges();
        }
        #endregion View Model Commands
    }
}
