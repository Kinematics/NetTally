using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NetTally.Extensions;
using NetTally.Global;
using NetTally.Types.Enums;

namespace NetTally.ViewModels
{
    public partial class MainViewModel : ObservableObject
    {
        private readonly ILogger<MainViewModel> logger;
        private readonly IOptions<GlobalSettings> globalSettings;
        private readonly IOptions<UserQuests> userQuests;

        public MainViewModel(ILogger<MainViewModel> logger,
            IOptions<GlobalSettings> globalSettings,
            IOptions<UserQuests> userQuests)
        {
            this.logger = logger;
            this.globalSettings = globalSettings;
            this.userQuests = userQuests;

            Quests = new ObservableCollection<Quest>(userQuests.Value.Quests);
        }

        public GlobalSettings GlobalSettings => globalSettings.Value;

        public ObservableCollection<Quest> Quests { get; } = new();

        public List<string> DisplayModes { get; } = EnumExtensions.EnumDescriptionsList<DisplayMode>().ToList();

        public List<string> PartitionModes { get; } = EnumExtensions.EnumDescriptionsList<PartitionMode>().ToList();

        public List<string> RankVoteCountingModes { get; } = EnumExtensions.EnumDescriptionsList<RankVoteCounterMethod>().ToList();

        
        [ObservableProperty]
        [NotifyCanExecuteChangedFor(nameof(RunTallyCommand))]
        private Quest? selectedQuest;

        [RelayCommand]
        private void AddQuest()
        {
            Quest q = new();
            if (!Quests.Contains(q))
            {
                Quests.Add(q);
                SelectedQuest = q;
                logger.LogInformation("Added new quest");
            }
        }

        [RelayCommand]
        private void RemoveQuest()
        {
            if (SelectedQuest != null)
            {
                int selectedQuestPosition = Quests.IndexOf(SelectedQuest);
                Quests.Remove(SelectedQuest);
                logger.LogInformation("Removed quest for thread: {url}", SelectedQuest.ThreadName);

                if (Quests.Count > 0)
                {
                    if (selectedQuestPosition < Quests.Count)
                    {
                        SelectedQuest = Quests[selectedQuestPosition];
                    }
                    else
                    {
                        SelectedQuest = Quests[^0];
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


        private bool CanRunTally() => !RunTallyCommand.IsRunning && SelectedQuest != null;
        private bool TallyIsRunning() => RunTallyCommand.IsRunning;
        private bool TallyIsNotRunning() => !RunTallyCommand.IsRunning;

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

        [RelayCommand(CanExecute = nameof(TallyIsRunning))]
        private void CancelTally()
        {
            if (tallyCTS?.IsCancellationRequested == false)
            {
                tallyCTS.Cancel();
            }
        }

        [RelayCommand(CanExecute = nameof(TallyIsNotRunning))]
        private void ClearTallyCache()
        {

        }
    }
}
