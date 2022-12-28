using System;
using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;
using NetTally.Extensions;
using NetTally.Global;

namespace NetTally.ViewModels
{
    public partial class TasksViewModel : ObservableObject
    {
        private readonly Quest quest;
        private readonly ILogger<TasksViewModel> logger;

        public ObservableCollection<string> Tasks { get; } = new();


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
            Tasks.Clear();
            foreach (var t in quest.VoteCounter.TaskList)
            {
                Tasks.Add(t);
            }

            logger.LogInformation("{count} tasks loaded.", Tasks.Count);
        }

        private void SaveTasks()
        {
            quest.VoteCounter.ReplaceTasks(Tasks);
            logger.LogInformation("{count} tasks saved.", Tasks.Count);
        }

        [RelayCommand]
        private void MoveTaskUp(int? position)
        {
            if (position.HasValue)
            {
                if (position.Value >= 0 && position < Tasks.Count)
                {
                    Tasks.Move(position.Value, position.Value - 1);
                }
            }
        }

        [RelayCommand]
        private void MoveTaskDown(int? position)
        {
            if (position.HasValue)
            {
                if (position.Value >= 0 && position < Tasks.Count)
                {
                    Tasks.Move(position.Value, position.Value + 1);
                }
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
            quest.VoteCounter.ResetTasksOrder(Types.Enums.TasksOrdering.AsTallied);
            LoadTasks();
        }

        [RelayCommand]
        private void Save()
        {
            SaveTasks();
        }
    }
}
