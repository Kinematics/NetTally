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

        public ObservableCollection<Quest> Quests => questsInfo.Quests;
        public bool HasQuests => Quests.Count > 0;

        public List<string> DisplayModes { get; } = EnumExtensions.EnumDescriptionsList<DisplayMode>().ToList();

        public List<string> PartitionModes { get; } = EnumExtensions.EnumDescriptionsList<PartitionMode>().ToList();

        public List<string> RankVoteCountingModes { get; } = EnumExtensions.EnumDescriptionsList<RankVoteCounterMethod>().ToList();

        [ObservableProperty]
        private bool hasNewRelease;



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

        public string Output => tally.TallyResults;

        public bool HasOutput => tally.HasTallyResults;

        public async Task UpdateOutput()
        {
            if (SelectedQuest is not null)
                await tally.UpdateResults(SelectedQuest);
        }


        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(IsQuestSelected))]
        [NotifyCanExecuteChangedFor(nameof(RunTallyCommand))]
        [NotifyCanExecuteChangedFor(nameof(RemoveQuestCommand))]
        [NotifyCanExecuteChangedFor(nameof(ClearTallyCacheCommand))]
        private Quest? selectedQuest;

        partial void OnSelectedQuestChanged(Quest? value)
        {
            questsInfo.SelectedQuest = value;
        }

        public bool IsQuestSelected => SelectedQuest != null;

        public List<Quest> GetLinkedQuests(Quest quest)
        {
            return Quests.Where(q => quest.HasLinkedQuest(q)).ToList();
        }


        private bool CanAddQuest => TallyIsNotRunning;

        [RelayCommand(CanExecute = nameof(CanAddQuest))]
        private void AddQuest()
        {
            bool hadZeroQuests = Quests.Count == 0;
            Quest q = questsInfo.CreateQuest();
            SelectedQuest = q;
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

                if (questsInfo.RemoveQuest(SelectedQuest))
                {
                    SelectedQuest = questsInfo.SelectedQuest;

                    if (Quests.Count == 0)
                    {
                        OnPropertyChanged(nameof(HasQuests));
                    }

                    if (SelectedQuest != null)
                    {
                        logger.LogInformation("Selected quest updated to thread: {url}", SelectedQuest.ThreadName);
                    }
                    else
                    {
                        logger.LogInformation("There are no remaining quests.");
                    }
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
                //await Task.Delay(5000, cancellationToken);
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
    }
}
