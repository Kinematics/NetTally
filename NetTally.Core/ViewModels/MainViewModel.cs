using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Input;
using NetTally.Collections;
using NetTally.CustomEventArgs;
using NetTally.Extensions;
using NetTally.Options;
using NetTally.Output;
using NetTally.Utility;
using NetTally.Utility.Comparers;
using NetTally.VoteCounting;
using NetTally.Votes;
using NetTally.Web;

namespace NetTally.ViewModels
{
    public class MainViewModel : ViewModelBase, IDisposable
    {
        public MainViewModel(
            IPageProvider pageProvider, IVoteCounter voteCounter, ITextResultsProvider textResults,
            Tally tally, CheckForNewRelease newRelease, IGlobalOptions globalOptions)
        {
            PageProvider = pageProvider;
            TextResultsProvider = textResults;
            VoteCounter = voteCounter;
            checkForNewRelease = newRelease;
            Options = globalOptions;

            SetupVoteCounter();

            AllVotesCollection = new ObservableCollectionExt<string>();
            AllVotersCollection = new ObservableCollectionExt<string>();

            Tally = tally;
            Tally.PropertyChanged += Tally_PropertyChanged;

            AddQuestCommand = new RelayCommand(this, DoAddQuest, CanAddQuest);
            RemoveQuestCommand = new RelayCommand(this, DoRemoveQuest, CanRemoveQuest);

            RunTallyCommand = new AsyncRelayCommand(this, DoRunTallyAsync, CanRunTally);
            CancelTallyCommand = new RelayCommand(this, DoCancelTally, CanCancelTally);
            ClearTallyCacheCommand = new RelayCommand(this, DoClearTallyCache, CanClearTallyCache);

            SetupWatches();
        }

        #region IDisposable
        bool _disposed;

        ~MainViewModel()
        {
            Dispose(false);
        }

        public void Dispose()
        {
            Dispose(true); //I am calling you from Dispose, it's safe
            GC.SuppressFinalize(this); //Hey, GC: don't bother calling finalize later
        }

        protected virtual void Dispose(bool itIsSafeToAlsoFreeManagedObjects)
        {
            if (_disposed)
                return;

            if (itIsSafeToAlsoFreeManagedObjects)
            {
                Tally.Dispose();
                PageProvider.Dispose();
            }

            _disposed = true;
        }
        #endregion

        #region Providers
        public IPageProvider PageProvider { get; private set; }

        public ITextResultsProvider TextResultsProvider { get; private set; }
        #endregion

        #region Section: Check for New Release
        /// Fields for this section
        readonly CheckForNewRelease checkForNewRelease;

        /// <summary>
        /// Pass-through flag indicating whether there is a newer release of the program available.
        /// </summary>
        public bool NewRelease => checkForNewRelease.NewRelease;

        /// <summary>
        /// Handles the PropertyChanged event of the CheckForNewRelease control.
        /// </summary>
        private void CheckForNewRelease_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == "NewRelease")
            {
                OnPropertyChanged(nameof(NewRelease));
            }
        }

        /// <summary>
        /// Public function to initiate a check for a new release.
        /// </summary>
        public void CheckForNewRelease()
        {
            checkForNewRelease.PropertyChanged += CheckForNewRelease_PropertyChanged;
            Task.Run(checkForNewRelease.Update);
        }
        #endregion

        #region Section: Options        
        /// <summary>
        /// Gets the user-readable list of display modes, for use in the view.
        /// </summary>
        public List<string> DisplayModes { get; } = EnumExtensions.EnumDescriptionsList<DisplayMode>().ToList();

        /// <summary>
        /// Gets the user-readable list of partition modes, for use in the view.
        /// </summary>
        public List<string> PartitionModes { get; } = EnumExtensions.EnumDescriptionsList<PartitionMode>().ToList();

        /// <summary>
        /// Gets the user-readable list of rank counting modes, for use in the view.
        /// </summary>
        public List<string> RankVoteCountingModes { get; } = EnumExtensions.EnumDescriptionsList<RankVoteCounterMethod>().ToList();

        /// <summary>
        /// Gets the user-readable list of valid posts per page, for use in the view.
        /// </summary>
        public List<int> ValidPostsPerPage { get; } = new List<int> { 0, 5, 10, 15, 20, 25, 30, 35, 40, 45, 50 };

        /// <summary>
        /// Public link to the advanced options instance, for data binding.
        /// </summary>
        public IGlobalOptions Options { get; }
        #endregion

        #region Quests

        #region Section: Quest collection
        /// <summary>
        /// Initializes the quests when defining the view model.
        /// Must be set separately from the class construction to avoid circular
        /// references when raising events.
        /// Takes the provided collection of quests and initializes the ViewModel
        /// with them.  Sets the current quest, if provided.
        /// </summary>
        /// <param name="quests">The quests.</param>
        /// <param name="currentQuest">The currently selected quest.</param>
        public void InitializeQuests(QuestCollection? quests, string? currentQuest)
        {
            if (quests != null)
            {
                QuestList = quests;
                QuestList.Sort();
                SelectQuest(currentQuest);
            }
            else
            {
                QuestList = new QuestCollection();
                SelectQuest(null);
            }
        }

        /// <summary>
        /// List of quests for binding.
        /// </summary>
        public QuestCollection QuestList { get; private set; } = new QuestCollection();

        /// <summary>
        /// The currently selected quest.
        /// </summary>
        IQuest? selectedQuest;
        public IQuest? SelectedQuest
        {
            get { return selectedQuest; }
            set
            {
                if (value == selectedQuest)
                    return;

                UnbindQuest(selectedQuest);
                selectedQuest = value;
                BindQuest(selectedQuest);
                OnPropertyChanged();
                OnPropertyChanged(nameof(IsQuestSelected));
            }
        }

        /// <summary>
        /// Allows directly setting a quest by its thread name.
        /// </summary>
        /// <param name="threadName">The thread name of the quest being selected.</param>
        public void SelectQuest(string? threadName)
        {
            if (string.IsNullOrEmpty(threadName))
            {
                SelectedQuest = null;
            }
            else
            {
                SelectedQuest = QuestList[threadName];
            }
        }

        /// <summary>
        /// Gets whether there's a valid selected quest.
        /// </summary>
        public bool IsQuestSelected => SelectedQuest != null;

        /// <summary>
        /// Gets whether there are any quests available for selection.
        /// </summary>
        public bool HasQuests => QuestList.Count > 0;
        #endregion

        #region Manage Events from the Quest
        private void BindQuest(IQuest? quest)
        {
            if (quest != null)
            {
                quest.PropertyChanged += SelectedQuest_PropertyChanged;

                // Some quest properties need to raise events on quest binding.
                OnPropertyChanged($"SelectedQuest.{nameof(IQuest.CaseIsSignificant)}");
                OnPropertyChanged($"SelectedQuest.{nameof(IQuest.WhitespaceAndPunctuationIsSignificant)}");
            }
        }

        private void UnbindQuest(IQuest? quest)
        {
            if (quest != null)
            {
                quest.PropertyChanged -= SelectedQuest_PropertyChanged;
            }
        }

        private void SelectedQuest_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == "DisplayName")
            {
                QuestList.Sort();
                OnPropertyChanged("RenameQuest");
            }

            OnPropertyChanged($"SelectedQuest.{e.PropertyName}");
        }
        #endregion

        #region Quiet quest list modifications        
        /// <summary>
        /// Adds the quest quietly.
        /// </summary>
        /// <param name="questToAdd">The quest to add.</param>
        public void AddQuestQuiet(IQuest questToAdd)
        {
            if (questToAdd != null)
            {
                QuestList.Add(questToAdd);
                QuestList.Sort();
                OnPropertyChanged(nameof(HasQuests));
            }
        }

        /// <summary>
        /// Renames the quest quietly.
        /// </summary>
        /// <param name="questURL">The quest URL.</param>
        /// <param name="newQuestName">New name of the quest.</param>
        public void RenameQuestQuiet(string questURL, string newQuestName)
        {
            if (string.IsNullOrEmpty(questURL))
                return;
            if (string.IsNullOrEmpty(newQuestName))
                return;

            var questToRename = QuestList.FirstOrDefault(a => questURL == a.ThreadName);
            if (questToRename != null)
            {
                questToRename.DisplayName = newQuestName;
                QuestList.Sort();
            }
        }

        /// <summary>
        /// Removes the quest quietly.
        /// </summary>
        /// <param name="questToRemove">The quest to remove.</param>
        public void RemoveQuestQuiet(IQuest questToRemove)
        {
            if (questToRemove != null)
            {
                int index = QuestList.IndexOf(questToRemove);

                if (index < 0)
                    return;

                bool isCurrentQuest = questToRemove == SelectedQuest;

                QuestList.RemoveAt(index);

                if (isCurrentQuest)
                {
                    if (QuestList.Count <= index)
                        SelectedQuest = QuestList.LastOrDefault();
                    else
                        SelectedQuest = QuestList[index];

                    OnPropertyChanged(nameof(HasQuests));
                }
            }
        }
        #endregion

        #region Add Quest
        /// <summary>
        /// Command property for adding a new quest to the quest list.
        /// </summary>
        public ICommand AddQuestCommand { get; private set; }

        /// <summary>
        /// Determines whether it's valid to add a new quest right now.
        /// </summary>
        /// <returns>Returns true if it's valid to execute the command.</returns>
        private bool CanAddQuest(object parameter) => !TallyIsRunning;

        /// <summary>
        /// Adds a new quest to the quest list, selects it, and notifies any
        /// listeners that it happened.
        /// </summary>
        /// <param name="parameter"></param>
        private void DoAddQuest(object parameter)
        {
            if (parameter is IQuest quest)
            {
                QuestList.Add(quest);
                SelectedQuest = quest;
            }
            else
            {
                IQuest newEntry = QuestList.AddNewQuest();

                if (newEntry != null)
                {
                    SelectedQuest = newEntry;
                }
            }

            OnPropertyChanged("AddQuest");
            OnPropertyChanged(nameof(HasQuests));
        }
        #endregion

        #region Remove Quest
        /// <summary>
        /// Command property for removing a quest from the quest list.
        /// </summary>
        public ICommand RemoveQuestCommand { get; private set; }

        /// <summary>
        /// Determines whether it's valid to remove the current or requested quest.
        /// </summary>
        /// <param name="parameter">The parameter to signal which quest is being removed.</param>
        /// <returns>Returns true if it's valid to execute the command.</returns>
        private bool CanRemoveQuest(object parameter) => !TallyIsRunning && GetThisQuest(parameter) != null;

        /// <summary>
        /// Removes either the currently selected quest, or the quest specified
        /// in the provided parameter (if it can be found).
        /// </summary>
        /// <param name="parameter">Either an IQuest object or a string specifying
        /// the quest's DisplayName.  If null, will instead use the current
        /// SelectedQuest.</param>
        private void DoRemoveQuest(object parameter)
        {
            int index = -1;
            IQuest? questToRemove = GetThisQuest(parameter);

            if (questToRemove == null)
                return;

            index = QuestList.IndexOf(questToRemove);

            if (index < 0)
                return;

            QuestList.RemoveAt(index);

            if (QuestList.Count <= index)
                SelectedQuest = QuestList.LastOrDefault();
            else
                SelectedQuest = QuestList[index];

            OnPropertyChanged("RemoveQuest");
            OnPropertyChanged(nameof(HasQuests));
        }
        #endregion

        #endregion

        #region Section: Tally & Results Binding
        /// Tally class object.
        Tally Tally { get; }

        /// <summary>
        /// Handles the PropertyChanged event of the Tally control.
        /// </summary>
        private void Tally_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(Tally.TallyIsRunning))
            {
                OnPropertyChanged(nameof(TallyIsRunning));
            }
            else if (e.PropertyName == nameof(Tally.TallyResults))
            {
                OnPropertyChanged(nameof(Output));
            }
            else if (e.PropertyName == nameof(Tally.HasTallyResults))
            {
                OnPropertyChanged(nameof(HasOutput));
            }
            else if (e is PropertyDataChangedEventArgs<string> eData)
            {
                if (eData.PropertyName == "TallyResultsStatusChanged")
                {
                    OnPropertyDataChanged(eData.PropertyData, eData.PropertyName);
                }
            }
        }

        /// <summary>
        /// Flag whether the tally is currently running.
        /// </summary>
        public bool TallyIsRunning => Tally.TallyIsRunning;

        /// <summary>
        /// The string containing the current tally progress or results.
        /// Creates a notification event if the contents change.
        /// </summary>
        public string Output => Tally.TallyResults;

        /// <summary>
        /// Flag whether there's any text in the Output property.
        /// </summary>
        public bool HasOutput => Tally.HasTallyResults;

        /// <summary>
        /// Redirection for user defined task values.
        /// </summary>
        public HashSet<string> UserDefinedTasks => VoteCounter.UserDefinedTasks;

        public void AddUserDefinedTask(string task)
        {
            if (UserDefinedTasks.Add(task))
            {
                TaskList.Add(task);
                OnPropertyChanged("Tasks");
            }
        }
        #endregion

        #region Section: Tally Commands
        CancellationTokenSource? cts;

        /// <summary>
        /// Requests that the tally class update its current results.
        /// </summary>
        public async void UpdateOutput()
        {
            await Tally.UpdateResults().ConfigureAwait(false);
        }

        #region Run the Tally
        /// <summary>
        /// Command property for adding a new quest to the quest list.
        /// </summary>
        public ICommand RunTallyCommand { get; private set; }

        /// <summary>
        /// Determines whether it's valid to add a new quest right now.
        /// </summary>
        /// <returns>Returns true if it's valid to execute the command.</returns>
        private bool CanRunTally(object parameter) => !TallyIsRunning && GetThisQuest(parameter) != null;

        /// <summary>
        /// Adds a new quest to the quest list, selects it, and notifies any
        /// listeners that it happened.
        /// </summary>
        /// <param name="parameter"></param>
        private async Task DoRunTallyAsync(object parameter)
        {
            try
            {
                using (cts = new CancellationTokenSource())
                {
                    try
                    {
                        if (SelectedQuest != null)
                            await Tally.RunAsync(SelectedQuest, cts.Token).ConfigureAwait(false);
                    }
                    catch (OperationCanceledException)
                    {
                        // Operation was cancelled.  Verify whether we requested the cancellation.
                        if (cts.IsCancellationRequested)
                        {
                            // User requested cancellation. Add it to the output display.
                            Tally.TallyResults += "Tally Cancelled!\n";
                        }
                        else
                        {
                            // User did not request the cancellation. Make sure to propogate
                            // the cancel to all tokens tied to us.
                            cts.Cancel();
                        }
                    }
                    catch (Exception e)
                    {
                        cts.Cancel();

                        if (!OnExceptionRaised(e).Handled)
                            throw;
                    }
                }
            }
            finally
            {
                cts = null;
            }
        }
        #endregion

        #region Cancel the Tally
        /// <summary>
        /// Command property for adding a new quest to the quest list.
        /// </summary>
        public ICommand CancelTallyCommand { get; private set; }

        /// <summary>
        /// Determines whether it's valid to add a new quest right now.
        /// </summary>
        /// <returns>Returns true if it's valid to execute the command.</returns>
        private bool CanCancelTally(object parameter) => TallyIsRunning;

        /// <summary>
        /// Adds a new quest to the quest list, selects it, and notifies any
        /// listeners that it happened.
        /// </summary>
        /// <param name="parameter"></param>
        private void DoCancelTally(object parameter)
        {
            Tally.Cancel();

            if (cts == null || cts.IsCancellationRequested)
            {
                Tally.TallyIsRunning = false;
            }
            else
            {
                cts.Cancel();
            }
        }
        #endregion

        #region Clear the Tally Cache
        /// <summary>
        /// Command property for adding a new quest to the quest list.
        /// </summary>
        public ICommand ClearTallyCacheCommand { get; private set; }

        /// <summary>
        /// Determines whether it's valid to add a new quest right now.
        /// </summary>
        /// <returns>Returns true if it's valid to execute the command.</returns>
        private bool CanClearTallyCache(object parameter) => !TallyIsRunning;

        /// <summary>
        /// Adds a new quest to the quest list, selects it, and notifies any
        /// listeners that it happened.
        /// </summary>
        /// <param name="parameter"></param>
        private void DoClearTallyCache(object parameter)
        {
            Tally.ClearPageCache();
        }
        #endregion

        #endregion

        #region Section: Vote Counter
        public ObservableCollectionExt<string> AllVotesCollection { get; }
        public ObservableCollectionExt<string> AllVotersCollection { get; }
        public List<string> TaskList => VoteCounter.OrderedTaskList;

        /// <summary>
        /// Increases the task position in the task list.
        /// </summary>
        /// <param name="currentPosition">The task position to modify.</param>
        public void IncreaseTaskPosition(int currentPosition)
        {
            TaskList.Swap(currentPosition, currentPosition + 1);
            OnPropertyChanged("Tasks");
        }

        /// <summary>
        /// Decreases the task position in the task list.
        /// </summary>
        /// <param name="currentPosition">The task position to modify.</param>
        public void DecreaseTaskPosition(int currentPosition)
        {
            TaskList.Swap(currentPosition, currentPosition - 1);
            OnPropertyChanged("Tasks");
        }

        /// <summary>
        /// Resets the tasks order.
        /// </summary>
        /// <param name="order">The type of ordering to use.</param>
        public void ResetTasksOrder(TasksOrdering order)
        {
            if (order == TasksOrdering.Alphabetical)
            {
                TaskList.Sort();
                OnPropertyChanged("Tasks");
            }
            else if (order == TasksOrdering.AsTallied)
            {
                TaskList.Clear();
                TaskList.AddRange(KnownTasks);
                OnPropertyChanged("Tasks");
            }
        }

        public IVoteCounter VoteCounter { get; private set; }

        /// <summary>
        /// Attach to the VoteCounter's property changed event.
        /// </summary>
        private void SetupVoteCounter()
        {
            VoteCounter.PropertyChanged += VoteCounter_PropertyChanged;
        }

        /// <summary>
        /// Update the observable collection of votes.
        /// </summary>
        private void UpdateVotesCollection()
        /// <param name="e">The <see cref="PropertyChangedEventArgs"/> instance containing the event data.</param>
        {
            var votesWithSupporters = VoteCounter.GetVotesCollection(VoteType.Vote);

            List<string> votes = votesWithSupporters.Keys
                .Concat(VoteCounter.GetCondensedRankVotes())
                .Distinct(Agnostic.StringComparer).ToList();

            AllVotesCollection.Replace(votes);

            OnPropertyChanged(nameof(AllVotesCollection));
        }

        /// <summary>
        /// Update the observable collection of voters.
        /// </summary>
        private void UpdateVotersCollection()
        {
            var voteVoters = VoteCounter.GetVotersCollection(VoteType.Vote);
            var rankVoters = VoteCounter.GetVotersCollection(VoteType.Rank);

            List<string> voters = voteVoters.Select(v => v.Key)
                .Concat(rankVoters.Select(v => v.Key))
                .Distinct().OrderBy(v => v).ToList();

            AllVotersCollection.Replace(voters);

            OnPropertyChanged(nameof(AllVotersCollection));
        }

        private void VoteCounter_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (!VoteCounter.VoteCounterIsTallying)
            {
                if (e.PropertyName == nameof(VoteCounter.VoteCounterIsTallying))
                {
                    // Called when the vote counter has finished its tallying.
                    // Update both observable collections.
                    UpdateVotesCollection();
                    UpdateVotersCollection();
                }
                else if (e.PropertyName == "VoteCounter")
                {
                    UpdateVotesCollection();
                    UpdateVotersCollection();
                }
                else if (e.PropertyName == "Votes")
                {
                    UpdateVotesCollection();
                }
                else if (e.PropertyName == "Voters")
                {
                    UpdateVotersCollection();
                }
            }
        }

        /// <summary>
        /// Gets the known tallied and user-defined tasks.
        /// </summary>
        public IEnumerable<string> KnownTasks => VoteCounter.KnownTasks;

        public bool VoteExists(string vote, VoteType voteType) => VoteCounter.HasVote(vote, voteType);

        public bool HasRankedVotes => VoteCounter.HasRankedVotes;

        public bool HasUndoActions => VoteCounter.HasUndoActions;

        public bool MergeVotes(string fromVote, string toVote, VoteType voteType) => VoteCounter.Merge(fromVote, toVote, voteType);

        public bool JoinVoters(List<string> voters, string voterToJoin, VoteType voteType) => VoteCounter.Join(voters, voterToJoin, voteType);

        public bool DeleteVote(string vote, VoteType voteType) => VoteCounter.Delete(vote, voteType);

        public bool PartitionChildren(string vote, VoteType voteType) => VoteCounter.PartitionChildren(vote, voteType, Tally.VoteConstructor);

        public bool UndoVoteModification() => VoteCounter.Undo();

        public HashSet<string>? GetVoterListForVote(string vote, VoteType voteType)
        {
            var votes = VoteCounter.GetVotesCollection(voteType);
            if (votes.ContainsKey(vote))
                return votes[vote];

            if (voteType == VoteType.Rank)
            {
                var condensedVoters = votes.Where(k => Agnostic.StringComparer.Equals(VoteString.CondenseVote(k.Key), vote)).Select(k => k.Value);

                HashSet<string> condensedHash = new HashSet<string>();

                foreach (var cond in condensedVoters)
                {
                    condensedHash.UnionWith(cond);
                }

                return condensedHash;
            }

            return null;
        }

        #endregion

        #region Section: Command Setup        
        /// <summary>
        /// Setups the commands to attach to the view model.
        /// </summary>
        private void SetupWatches()
        {
            NonCommandPropertyChangedValues.Add("NewRelease");
        }
        #endregion

        #region Section: Command Utility
        /// <summary>
        /// Get the specified quest.
        /// If the parameter is null, returns the currently SelectedQuest.
        /// If the parameter is a quest, returns that.
        /// If the parameter is a string, returns a quest with that DisplayName.
        /// </summary>
        /// <param name="parameter">Indicator of what quest is being requested.</param>
        /// <returns>Returns an IQuest based on the above stipulations, or null.</returns>
        private IQuest? GetThisQuest(object parameter)
        {
            if (parameter is IQuest quest)
            {
                return quest;
            }
            else if (parameter is string questName)
            {
                return QuestList.FirstOrDefault(a => questName == a.DisplayName);
            }
            else if (parameter is null)
            {
                return SelectedQuest;
            }

            return null;
        }
        #endregion
    }
}
