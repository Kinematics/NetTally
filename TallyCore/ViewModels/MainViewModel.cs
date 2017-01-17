using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Input;
using NetTally.Utility;
using NetTally.Web;

namespace NetTally.ViewModels
{
    public class MainViewModel : ViewModelBase, IDisposable
    {
        public MainViewModel(QuestCollectionWrapper config, HttpClientHandler handler = null)
        {
            if (config != null)
            {
                QuestList = config.QuestCollection;
                QuestList.Sort();
                SelectQuest(config.CurrentQuest);
            }
            else
            {
                QuestList = new QuestCollection();
                SelectQuest(null);
            }

            SetupNetwork(handler);

            BindCheckForNewRelease();

            BindTally();

            AllVotesCollection = new ObservableCollectionExt<string>();
            AllVotersCollection = new ObservableCollectionExt<string>();

            BindVoteCounter();

            SetupCommands();
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
                tally.Dispose();
                PageProvider.Dispose();
            }

            _disposed = true;
        }
        #endregion

        #region Networking
        public IPageProvider PageProvider { get; private set; }

        private void SetupNetwork(HttpClientHandler handler)
        {
            if (handler == null)
                handler = new HttpClientHandler();
            PageProvider = new WebPageProvider3(handler);
        }
        #endregion

        #region Section: Check for New Release
        /// Fields for this section
        CheckForNewRelease checkForNewRelease = new CheckForNewRelease();

        /// <summary>
        /// Bind event watcher to the class that checks for new releases of the program.
        /// </summary>
        private void BindCheckForNewRelease()
        {
            checkForNewRelease.PropertyChanged += CheckForNewRelease_PropertyChanged;
        }

        public void CheckForNewRelease()
        {
            checkForNewRelease.Update(PageProvider);
        }

        /// <summary>
        /// Handles the PropertyChanged event of the CheckForNewRelease control.
        /// </summary>
        private void CheckForNewRelease_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == "NewRelease")
            {
                OnPropertyChanged("NewReleaseAvailable");
            }
        }

        /// <summary>
        /// Flag indicating whether there is a newer release of the program available.
        /// </summary>
        public bool NewReleaseAvailable => checkForNewRelease.NewRelease;
        #endregion

        #region Section: Options        
        /// <summary>
        /// Gets the user-readable list of display modes, for use in the view.
        /// </summary>
        public List<string> DisplayModes { get; } = Enumerations.EnumDescriptionsList<DisplayMode>().ToList();

        /// <summary>
        /// Gets the user-readable list of partition modes, for use in the view.
        /// </summary>
        public List<string> PartitionModes { get; } = Enumerations.EnumDescriptionsList<PartitionMode>().ToList();

        /// <summary>
        /// Gets the user-readable list of rank counting modes, for use in the view.
        /// </summary>
        public List<string> RankVoteCountingModes { get; } = Enumerations.EnumDescriptionsList<RankVoteCounterMethod>().ToList();

        /// <summary>
        /// Gets the user-readable list of valid posts per page, for use in the view.
        /// </summary>
        public List<int> ValidPostsPerPage { get; } = new List<int> { 0, 5, 10, 15, 20, 25, 30, 35, 40, 45, 50 };

        /// <summary>
        /// Public link to the advanced options instance, for data binding.
        /// </summary>
        public AdvancedOptions Options => AdvancedOptions.Instance;
        #endregion

        #region Section: Quest collection
        /// <summary>
        /// List of quests for binding.
        /// </summary>
        public QuestCollection QuestList { get; }

        /// <summary>
        /// The currently selected quest.
        /// </summary>
        IQuest selectedQuest;
        public IQuest SelectedQuest
        {
            get { return selectedQuest; }
            set
            {
                UnbindQuest(selectedQuest);
                selectedQuest = value;
                BindQuest(selectedQuest);
                OnPropertyChanged();
                OnPropertyChanged("IsQuestSelected");
                OnPropertyChanged("IsSafeToEnable");
            }
        }

        public void SelectQuest(string threadName)
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

        public bool IsQuestSelected => SelectedQuest != null;

        public bool HasQuests => QuestList.Count > 0;

        public bool IsSafeToEnable => IsQuestSelected && !TallyIsRunning;

        #region Manage Events from the Quest
        private void BindQuest(IQuest quest)
        {
            if (quest != null)
            {
                quest.PropertyChanged += SelectedQuest_PropertyChanged;
            }
        }

        private void UnbindQuest(IQuest quest)
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
            IQuest newEntry = QuestList.AddNewQuest();

            if (newEntry != null)
            {
                SelectedQuest = newEntry;
                OnPropertyChanged("AddQuest");
                OnPropertyChanged("HasQuests");
            }
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
            IQuest questToRemove = GetThisQuest(parameter);

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
            OnPropertyChanged("HasQuests");
        }
        #endregion

        #endregion

        #region Section: Tally & Results Binding
        /// Tally class object.
        Tally tally;

        /// <summary>
        /// Bind event watcher to the class that handles running the tallies.
        /// </summary>
        private void BindTally()
        {
            tally = new Tally(PageProvider);
            tally.PropertyChanged += Tally_PropertyChanged;
        }

        /// <summary>
        /// Handles the PropertyChanged event of the Tally control.
        /// </summary>
        private void Tally_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == "TallyIsRunning")
            {
                OnPropertyChanged("TallyIsRunning");
                OnPropertyChanged("IsSafeToEnable");
            }
            else if (e.PropertyName == "TallyResults")
            {
                OnPropertyChanged("Output");
                OnPropertyChanged("HasOutput");
            }
        }

        /// <summary>
        /// Flag whether the tally is currently running.
        /// </summary>
        public bool TallyIsRunning => tally.TallyIsRunning;

        /// <summary>
        /// Shortcut version while there are issues with data converters in the xaml.
        /// </summary>
        public bool TallyIsNotRunning => !TallyIsRunning;

        /// <summary>
        /// The string containing the current tally progress or results.
        /// Creates a notification event if the contents change.
        /// </summary>
        public string Output => tally.TallyResults;

        /// <summary>
        /// Flag whether there's any text in the Output property.
        /// </summary>
        public bool HasOutput => !string.IsNullOrEmpty(Output);

        /// <summary>
        /// Redirection for user defined task values.
        /// </summary>
        public HashSet<string> UserDefinedTasks => tally.UserDefinedTasks;

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
        CancellationTokenSource cts;

        /// <summary>
        /// Requests that the tally class update its current results.
        /// </summary>
        public void UpdateOutput()
        {
            tally.UpdateResults();
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
        private async Task DoRunTally(object parameter)
        {
            try
            {
                using (cts = new CancellationTokenSource())
                {
                    try
                    {
                        await tally.Run(SelectedQuest, cts.Token);
                    }
                    catch (OperationCanceledException)
                    {
                        // Got a cancel request somewhere.  No special handling needed.
                    }
                }
            }
            catch (Exception e)
            {
                if (!OnExceptionRaised(e).Handled)
                    throw;
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
            cts?.Cancel();
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
            tally.ClearPageCache();
        }
        #endregion

        #endregion

        #region Section: Vote Counter
        public ObservableCollectionExt<string> AllVotesCollection { get; }
        public ObservableCollectionExt<string> AllVotersCollection { get; }
        public List<string> TaskList { get; set; }

        private void BindVoteCounter()
        {
            VoteCounter.Instance.PropertyChanged += VoteCounter_PropertyChanged;
        }

        private void VoteCounter_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == "Votes")
            {
                var votesWithSupporters = VoteCounter.Instance.GetVotesCollection(VoteType.Vote);

                List<string> votes = votesWithSupporters.Keys
                    .Concat(VoteCounter.Instance.GetCondensedRankVotes())
                    .Distinct(StringUtility.AgnosticStringComparer).ToList();

                AllVotesCollection.Replace(votes);

                TaskList = KnownTasks.ToList();

                var voteVoters = VoteCounter.Instance.GetVotersCollection(VoteType.Vote);
                var rankVoters = VoteCounter.Instance.GetVotersCollection(VoteType.Rank);

                List<string> voters = voteVoters.Select(v => v.Key)
                    .Concat(rankVoters.Select(v => v.Key))
                    .Distinct().OrderBy(v => v).ToList();

                AllVotersCollection.Replace(voters);

                OnPropertyChanged("VotesFromTally");
            }
        }

        public IEnumerable<string> KnownTasks
        {
            get
            {
                var voteTasks = VoteCounter.Instance.GetVotesCollection(VoteType.Vote).Keys
                    .Select(v => VoteString.GetVoteTask(v));
                var rankTasks = VoteCounter.Instance.GetVotesCollection(VoteType.Rank).Keys
                    .Select(v => VoteString.GetVoteTask(v));
                var userTasks = UserDefinedTasks.ToList();

                var allTasks = voteTasks.Concat(rankTasks).Concat(userTasks)
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .Where(v => !string.IsNullOrEmpty(v));

                return allTasks;
            }
        }

        public bool HasRankedVotes => VoteCounter.Instance.HasRankedVotes;

        public bool HasUndoActions => VoteCounter.Instance.HasUndoActions;

        public bool MergeVotes(string fromVote, string toVote, VoteType voteType) => VoteCounter.Instance.Merge(fromVote, toVote, voteType);

        public bool JoinVoters(List<string> voters, string voterToJoin, VoteType voteType) => VoteCounter.Instance.Join(voters, voterToJoin, voteType);

        public bool DeleteVote(string vote, VoteType voteType) => VoteCounter.Instance.Delete(vote, voteType);

        public bool UndoVoteModification() => VoteCounter.Instance.Undo();

        public HashSet<string> GetVoterListForVote(string vote, VoteType voteType)
        {
            var votes = VoteCounter.Instance.GetVotesCollection(voteType);
            if (votes.ContainsKey(vote))
                return votes[vote];

            if (voteType == VoteType.Rank)
            {
                var condensedVoters = votes.Where(k => StringUtility.AgnosticStringComparer.Equals(VoteString.CondenseVote(k.Key), vote)).Select(k => k.Value);

                HashSet<string> condensedHash = new HashSet<string>();

                foreach (var cond in condensedVoters)
                {
                    condensedHash.UnionWith(cond);
                }

                return condensedHash;
            }

            return null;
        }

        public bool VoteExists(string vote, VoteType voteType) => VoteCounter.Instance.HasVote(vote, voteType);
        #endregion

        #region Section: Command Setup        
        /// <summary>
        /// Setups the commands to attach to the view model.
        /// </summary>
        private void SetupCommands()
        {
            AddQuestCommand = new RelayCommand(this, DoAddQuest, CanAddQuest);
            RemoveQuestCommand = new RelayCommand(this, DoRemoveQuest, CanRemoveQuest);

            RunTallyCommand = new AsyncRelayCommand(this, DoRunTally, CanRunTally);
            CancelTallyCommand = new RelayCommand(this, DoCancelTally, CanCancelTally);
            ClearTallyCacheCommand = new RelayCommand(this, DoClearTallyCache, CanClearTallyCache);
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
        private IQuest GetThisQuest(object parameter)
        {
            IQuest thisQuest;

            if (parameter == null)
            {
                thisQuest = SelectedQuest;
            }
            else
            {
                thisQuest = parameter as IQuest;
                if (thisQuest == null)
                {
                    string questName = parameter as string;
                    if (!string.IsNullOrEmpty(questName))
                    {
                        thisQuest = QuestList.FirstOrDefault(a => questName == a.DisplayName);
                    }
                }
            }

            return thisQuest;
        }
        #endregion
    }
}
