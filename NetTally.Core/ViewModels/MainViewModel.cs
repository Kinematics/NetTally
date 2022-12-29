using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;
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
        private readonly ILogger<MainViewModel> logger;

        public MainViewModel(
            IQuestsInfoMod questsInfo,
            Tally tally,
            ILogger<MainViewModel> logger)
        {
            this.logger = logger;
            this.questsInfo = questsInfo;
            this.tally = tally;
            SelectedQuest = questsInfo.SelectedQuest;

            System.Net.ServicePointManager.DefaultConnectionLimit = 4;
            System.Net.ServicePointManager.Expect100Continue = true;
            System.Net.ServicePointManager.SecurityProtocol = System.Net.SecurityProtocolType.Tls12;
        }

        public ObservableCollection<Quest> Quests => questsInfo.Quests;

        public bool HasQuests => Quests.Count > 0;


        public List<string> DisplayModes { get; } = EnumExtensions.EnumDescriptionsList<DisplayMode>().ToList();

        public List<string> PartitionModes { get; } = EnumExtensions.EnumDescriptionsList<PartitionMode>().ToList();

        public List<string> RankVoteCountingModes { get; } = EnumExtensions.EnumDescriptionsList<RankVoteCounterMethod>().ToList();

        [ObservableProperty]
        private bool hasNewRelease;

        [ObservableProperty]
        private string output = string.Empty;

        public bool HasOutput => Output != string.Empty;

        public async Task UpdateOutput()
        {
            if (SelectedQuest is not null)
                await tally.UpdateResults(SelectedQuest);
        }

        public List<Quest> GetLinkedQuests(Quest quest)
        {
            return Quests.Where(quest.HasLinkedQuest).ToList();
        }


        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(IsQuestSelected))]
        [NotifyCanExecuteChangedFor(nameof(RunTallyCommand))]
        [NotifyCanExecuteChangedFor(nameof(RemoveQuestCommand))]
        private Quest? selectedQuest;

        partial void OnSelectedQuestChanged(Quest? value)
        {
            questsInfo.SelectedQuest = value;
        }

        public bool IsQuestSelected => SelectedQuest != null;

        [RelayCommand]
        private void AddQuest()
        {
            Quest q = new();
            if (!Quests.Contains(q))
            {
                SelectedQuest = q;
                Quests.Add(q);
                OnPropertyChanged(nameof(HasQuests));
                logger.LogInformation("Added new quest");
            }
        }

        private bool CanRemoveQuest() => IsQuestSelected;

        [RelayCommand(CanExecute = nameof(CanRemoveQuest))]
        private void RemoveQuest()
        {
            if (SelectedQuest != null)
            {
                logger.LogInformation("Removing quest for thread: {url}", SelectedQuest.ThreadName);
                int selectedQuestPosition = Quests.IndexOf(SelectedQuest);
                Quests.Remove(SelectedQuest);
                OnPropertyChanged(nameof(HasQuests));

                if (Quests.Count > 0)
                {
                    if (selectedQuestPosition < Quests.Count)
                    {
                        SelectedQuest = Quests[selectedQuestPosition];
                    }
                    else
                    {
                        SelectedQuest = Quests[^1];
                    }

                    logger.LogInformation("Selected quest updated to thread: {url}", SelectedQuest.ThreadName);
                }
                else
                {
                    SelectedQuest = null;
                    logger.LogInformation("There are no remaining quests.");
                }
            }
        }


        public bool TallyIsRunning => RunTallyCommand.IsRunning;
        public bool TallyIsNotRunning => !RunTallyCommand.IsRunning;

        private bool CanRunTally() => TallyIsNotRunning && IsQuestSelected;
        private bool CanCancelTally() => TallyIsRunning;
        private bool CanClearTallyCache() => TallyIsNotRunning && IsQuestSelected;


        private CancellationTokenSource? tallyCTS;

        [RelayCommand(CanExecute = nameof(CanRunTally),
            IncludeCancelCommand = true)]
        private async Task RunTallyAsync(CancellationToken cancellationToken)
        {
            try
            {
                using (tallyCTS = new CancellationTokenSource())

                    await Task.Delay(100, tallyCTS.Token).ConfigureAwait(false);

            }
            finally
            {
                tallyCTS = null;
            }
        }

        [RelayCommand(CanExecute = nameof(CanCancelTally))]
        private void CancelTally()
        {
            if (tallyCTS?.IsCancellationRequested == false)
            {
                tallyCTS.Cancel();
            }
        }

        [RelayCommand(CanExecute = nameof(CanClearTallyCache))]
        private void ClearTallyCache()
        {

        }
    }
}
