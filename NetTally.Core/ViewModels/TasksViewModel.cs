using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;
using NetTally.Configure;
using NetTally.Enums;
using NetTally.Tally.Components.Votes;
using NetTally.Utility.Collections;

namespace NetTally.ViewModels;

/// <summary>
/// A view model for managing and rearranging tasks.
/// </summary>
public partial class TasksViewModel : ObservableObject
{
    private readonly Quest quest;
    private readonly ILogger<TasksViewModel> logger;

    public ObservableCollectionExt<VoteTask> Tasks { get; } = [];

    public TasksViewModel(
        IQuestsInfo questsInfo,
        ILogger<TasksViewModel> logger)
    {
        ArgumentNullException.ThrowIfNull(questsInfo.SelectedQuest);

        this.logger = logger;
        this.quest = questsInfo.SelectedQuest;

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

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(MoveTaskUpCommand))]
    [NotifyCanExecuteChangedFor(nameof(MoveTaskDownCommand))]
    public partial int SelectedTaskIndex { get; set; }

    private bool CanMoveTaskUp()
    {
        return (Tasks.Count > 1 && SelectedTaskIndex > 0);
    }

    [RelayCommand(CanExecute = nameof(CanMoveTaskUp))]
    private void MoveTaskUp()
    {
        if (Tasks.Count > 1 && SelectedTaskIndex > 0)
        {
            int newIndex = SelectedTaskIndex - 1;
            Tasks.Move(SelectedTaskIndex, newIndex);
            SelectedTaskIndex = newIndex;
        }
    }

    private bool CanMoveTaskDown()
    {
        return (Tasks.Count > 1 && SelectedTaskIndex >= 0 && SelectedTaskIndex < Tasks.Count - 1);
    }

    [RelayCommand(CanExecute = nameof(CanMoveTaskDown))]
    private void MoveTaskDown()
    {
        if (Tasks.Count > 1 && SelectedTaskIndex >= 0 && SelectedTaskIndex < Tasks.Count - 1)
        {
            int newIndex = SelectedTaskIndex + 1;
            Tasks.Move(SelectedTaskIndex, newIndex);
            SelectedTaskIndex = newIndex;
        }
    }

    [RelayCommand]
    private void Alphabetize()
    {
        Tasks.Sort(VoteTaskComparer.Instance);
    }

    [RelayCommand]
    private void AlphbetizeDown()
    {
        Tasks.SortDescending(VoteTaskComparer.Instance);
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
        OnPropertyChanged(nameof(SaveCommand));
    }
}
