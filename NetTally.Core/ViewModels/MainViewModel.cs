using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;
using NetTally.Extensions;
using NetTally.Types.Enums;

namespace NetTally.ViewModels
{
    [ObservableObject]
    public partial class MainViewModel
    {
        private readonly Logger<MainViewModel> logger;

        public MainViewModel(Logger<MainViewModel> logger)
        {
            this.logger = logger;
        }

        public List<string> DisplayModes { get; } = EnumExtensions.EnumDescriptionsList<DisplayMode>().ToList();

        public List<string> PartitionModes { get; } = EnumExtensions.EnumDescriptionsList<PartitionMode>().ToList();

        public List<string> RankVoteCountingModes { get; } = EnumExtensions.EnumDescriptionsList<RankVoteCounterMethod>().ToList();


        public ObservableCollection<IQuest> Quests { get; } = new();

        [ObservableProperty]
        [NotifyCanExecuteChangedFor(nameof(RunTallyCommand))]
        private IQuest? selectedQuest;

        [RelayCommand]
        private void AddQuest()
        {
            IQuest q = new Quest();
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

        [RelayCommand(CanExecute = nameof(CanRunTally))]
        private async Task RunTally()
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
