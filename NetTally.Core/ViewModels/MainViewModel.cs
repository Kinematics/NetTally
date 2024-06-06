using System.Collections.ObjectModel;
using System.ComponentModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;
using NetTally.Cache;
using NetTally.Configure;
using NetTally.Data;
using NetTally.Enums;
using NetTally.Extensions;
using NetTally.Tally;

namespace NetTally.ViewModels
{
    public partial class MainViewModel : ObservableObject
    {
        private readonly IQuestsInfoMod questsInfo;
        private readonly Tallyer tally;
        private readonly PageCache pageCache;
        private readonly CheckForNewRelease checkForNewRelease;
        private readonly ILogger<MainViewModel> logger;

        public MainViewModel(
            IQuestsInfoMod questsInfo,
            Tallyer tally,
            PageCache cache,
            CheckForNewRelease checkForNewRelease,
            ILogger<MainViewModel> logger)
        {
            this.logger = logger;
            this.questsInfo = questsInfo;
            this.tally = tally;
            this.pageCache = cache;
            this.checkForNewRelease = checkForNewRelease;
            SelectedQuest = questsInfo.SelectedQuest;

            System.Net.ServicePointManager.DefaultConnectionLimit = 4;
            System.Net.ServicePointManager.Expect100Continue = true;
            System.Net.ServicePointManager.SecurityProtocol = System.Net.SecurityProtocolType.Tls12;

            RunTallyCommand.PropertyChanged += RunTallyCommand_PropertyChanged;
            tally.PropertyChanged += Tally_PropertyChanged;
            checkForNewRelease.PropertyChanged += CheckForNewRelease_PropertyChanged;
        }

        #region Item Source Properties
        public ObservableCollection<Quest> Quests => questsInfo.Quests;

        public List<string> DisplayModes { get; } = EnumExtensions.EnumDescriptionsList<DisplayMode>().ToList();

        public List<string> PartitionModes { get; } = EnumExtensions.EnumDescriptionsList<PartitionMode>().ToList();

        public List<string> RankVoteCountingModes { get; } = EnumExtensions.EnumDescriptionsList<RankVoteCounterMethod>().ToList();
        #endregion Item Source Properties

        #region State Properties
        public bool HasQuests => Quests.Count > 0;
        public bool IsQuestSelected => SelectedQuest != null;
        public bool TallyIsRunning => RunTallyCommand.IsRunning;
        public bool HasOutput => tally.HasTallyResults;
        public string Output => tally.TallyResults;
        #endregion State Properties

        #region Generated Properties
        [ObservableProperty]
        private bool hasNewRelease;

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
            tally.ClearTallyResults();

            if (value is not null)
            {
                value.PropertyChanged += Quest_PropertyChanged;
                UpdateOutput();
            }
        }
        #endregion Generated Properties

        #region Update Functions
        public void CheckForNewRelease()
        {
            checkForNewRelease.Start();
        }

        public void UpdateTally()
        {
            if (SelectedQuest is not null)
            {
                tally.UpdateTally(SelectedQuest);
            }
        }

        public void UpdateOutput()
        {
            if (SelectedQuest is not null)
            {
                tally.UpdateOutput(SelectedQuest);
            }
        }

        public void RepositionQuest()
        {
            var wasQuest = SelectedQuest;
            questsInfo.RepositionQuest(SelectedQuest);
            SelectedQuest = wasQuest;
        }
        #endregion Update Functions

        #region View Model Commands
        private bool CanAddQuest => !TallyIsRunning;

        [RelayCommand(CanExecute = nameof(CanAddQuest))]
        private void AddQuest()
        {
            bool hadZeroQuests = Quests.Count == 0;

            SelectedQuest = questsInfo.CreateQuest();

            if (hadZeroQuests)
                OnPropertyChanged(nameof(HasQuests));

            OnPropertyChanged(nameof(AddQuestCommand));

            logger.LogInformation("Added new quest");
        }

        private bool CanRemoveQuest() => !TallyIsRunning && IsQuestSelected;

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

        private bool CanRunTally() => !TallyIsRunning && IsQuestSelected;

        [RelayCommand(CanExecute = nameof(CanRunTally),
            IncludeCancelCommand = true)]
        private async Task RunTally(CancellationToken cancellationToken)
        {
            try
            {
                if (SelectedQuest is not null)
                {
                    await tally.RunTallyAsync(SelectedQuest, cancellationToken);
                }
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
            catch (Exception e)
            {
                tally.TallyResults += e.Message;
                logger.LogError(e, "Failure while tallying.");
                RunTallyCommand.Cancel();
            }
        }

        private bool CanClearTallyCache() => !TallyIsRunning && IsQuestSelected;

        [RelayCommand(CanExecute = nameof(CanClearTallyCache))]
        private void ClearTallyCache()
        {
            pageCache.Clear();
            SelectedQuest?.VoteCounterF.ResetUserMerges();
        }
        #endregion View Model Commands

        #region Event Handling
        private void CheckForNewRelease_PropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            HasNewRelease = checkForNewRelease.HasNewRelease;
        }

        private void Tally_PropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(Tallyer.TallyResults))
            {
                OnPropertyChanged(nameof(Output));
            }
            else if (e.PropertyName == nameof(Tallyer.HasTallyResults))
            {
                OnPropertyChanged(nameof(HasOutput));
            }
        }

        private void Quest_PropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            // If the display mode is changed, we just have to update the output.
            // Any other change to the quest preferences needs a full re-tally.
            if (sender is Quest)
            {
                switch (e.PropertyName)
                {
                    case nameof(Quest.DisplayMode):
                        UpdateOutput();
                        break;
                    default:
                        UpdateTally();
                        break;
                }
            }
        }

        private void RunTallyCommand_PropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(RunTallyCommand.IsRunning))
            {
                AddQuestCommand.NotifyCanExecuteChanged();
                RemoveQuestCommand.NotifyCanExecuteChanged();
                ClearTallyCacheCommand.NotifyCanExecuteChanged();
                OnPropertyChanged(nameof(TallyIsRunning));

                if (RunTallyCommand.ExecutionTask?.IsCompletedSuccessfully ?? false)
                {
                    OnPropertyChanged(nameof(Output));
                }
            }
        }
        #endregion Event Handling
    }
}
