using System;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;
using NetTally.Collections;
using NetTally.Extensions;
using NetTally.Global;
using NetTally.Types.Enums;

namespace NetTally.ViewModels
{
    public partial class TasksViewModel : ObservableObject
    {
        private readonly Quest quest;
        private readonly ILogger<TasksViewModel> logger;

        public ObservableCollectionExt<string> Tasks { get; } = new();


        public TasksViewModel(
            IQuestsInfo questsInfo,
            ILogger<TasksViewModel> logger)
        {
            this.logger = logger;

            ArgumentNullException.ThrowIfNull(questsInfo.SelectedQuest);
            quest = questsInfo.SelectedQuest;

            LoadTasks();
        }

        private void LoadTasks()
        {
            Tasks.Replace(quest.VoteCounter.TaskList);
            logger.LogInformation("{count} tasks loaded.", Tasks.Count);
        }

        private void SaveTasks()
        {
            quest.VoteCounter.ReplaceTasks(Tasks);
            logger.LogInformation("{count} tasks saved.", Tasks.Count);
        }

        private bool CanMoveTaskUp(int? position)
        {
            return (Tasks.Count > 1 && position.HasValue && position.Value > 0);
        }

        [RelayCommand(CanExecute = nameof(CanMoveTaskUp))]
        private void MoveTaskUp(int? position)
        {
            if (position.HasValue && position.Value > 0 && position < Tasks.Count)
            {
                Tasks.Move(position.Value, position.Value - 1);
            }
        }

        private bool CanMoveTaskDown(int? position)
        {
            return (Tasks.Count > 1 && position.HasValue && position.Value < Tasks.Count - 1);
        }

        [RelayCommand(CanExecute = nameof(CanMoveTaskDown))]
        private void MoveTaskDown(int? position)
        {
            if (position.HasValue && position.Value >= 0 && position < Tasks.Count - 1)
            {
                Tasks.Move(position.Value, position.Value + 1);
            }
        }

        [RelayCommand]
        private void Alphabetize()
        {
            Tasks.Sort();
        }

        [RelayCommand]
        private void AlphbetizeDown()
        {
            Tasks.Sort(descending: true);
        }

        [RelayCommand]
        private void PutInTallyOrder()
        {
            quest.VoteCounter.ResetTasksOrder(TasksOrdering.AsTallied);
            LoadTasks();
        }

        [RelayCommand]
        private void Save()
        {
            SaveTasks();
        }
    }
}
