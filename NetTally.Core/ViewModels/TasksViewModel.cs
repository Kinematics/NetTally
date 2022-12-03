using System;
using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;
using NetTally.Extensions;
using NetTally.VoteCounting;

namespace NetTally.ViewModels
{
    [ObservableObject]
    public partial class TasksViewModel
    {
        private readonly ILogger<TasksViewModel> logger;
        private readonly IVoteCounter voteCounter;

        public ObservableCollection<string> Tasks { get; } = new();


        public TasksViewModel(ILogger<TasksViewModel> logger,
            IVoteCounter voteCounter)
        {
            this.logger = logger;
            this.voteCounter = voteCounter;

            LoadTasks();
        }

        private void LoadTasks()
        {
            Tasks.Clear();
            foreach (var t in voteCounter.TaskList)
            {
                Tasks.Add(t);
            }

            logger.LogInformation("{count} tasks loaded.", Tasks.Count);
        }

        private void SaveTasks()
        {
            voteCounter.TaskList.Clear();
            foreach (var t in Tasks)
            {
                voteCounter.TaskList.Add(t);
            }

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
            voteCounter.ResetTasksOrder(Types.Enums.TasksOrdering.AsTallied);
            LoadTasks();
        }

        [RelayCommand]
        private void Save()
        {
            SaveTasks();
        }
    }
}
