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
using NetTally.Extensions;
using NetTally.Output;
using NetTally.Utility;
using NetTally.VoteCounting;
using NetTally.Votes;
using NetTally.Web;

namespace NetTally.ViewModels
{
    public class MainViewModel : ViewModelBase, IDisposable
    {
        public MainViewModel(QuestCollectionWrapper config, HttpClientHandler handler,
            IPageProvider pageProvider, ITextResultsProvider textResults,
            IErrorLogger errorLogger, Func<string, CompareInfo, CompareOptions, int> hashFunction)
        {
            ErrorLog.LogUsing(errorLogger);
            Agnostic.HashStringsUsing(hashFunction);

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

            SetupNetwork(pageProvider, handler);
            SetupTextResults(textResults);

            AllVotesCollection = new ObservableCollectionExt<string>();
            AllVotersCollection = new ObservableCollectionExt<string>();

            BuildCheckForNewRelease();
            BuildTally();
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

        private void SetupNetwork(IPageProvider pageProvider, HttpClientHandler handler)
        {
            PageProvider = pageProvider ?? PageProviderBuilder.Instance.HttpClientHandler(handler).Build();
        }
        #endregion

        #region Section: Check for New Release
        /// Fields for this section
        CheckForNewRelease checkForNewRelease;

        /// <summary>
        /// Create a new CheckForNewRelease object, and bind an event listener to it.
        /// </summary>
        private void BuildCheckForNewRelease()
        {
            checkForNewRelease = new CheckForNewRelease();
            checkForNewRelease.PropertyChanged += CheckForNewRelease_PropertyChanged;
        }

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
            Task.Run(checkForNewRelease.Update);
        }
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

        #region Quests

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
            OnPropertyChanged(nameof(HasQuests));
        }
        #endregion

        #endregion

        #region Section: Tally & Results Binding
        public ITextResultsProvider TextResultsProvider { get; private set; }

        private void SetupTextResults(ITextResultsProvider textResults)
        {
            TextResultsProvider = textResults ?? new TallyOutput();
        }

        /// Tally class object.
        Tally tally;

        /// <summary>
        /// Bind event watcher to the class that handles running the tallies.
        /// </summary>
        private void BuildTally()
        {
            tally = new Tally(PageProvider);
            tally.PropertyChanged += Tally_PropertyChanged;
        }

        /// <summary>
        /// Handles the PropertyChanged event of the Tally control.
        /// </summary>
        private void Tally_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(tally.TallyIsRunning))
            {
                OnPropertyChanged(nameof(TallyIsRunning));
            }
            else if (e.PropertyName == nameof(tally.TallyResults))
            {
                OnPropertyChanged(nameof(Output));
            }
            else if (e.PropertyName == nameof(tally.HasTallyResults))
            {
                OnPropertyChanged(nameof(HasOutput));
            }
            else if (e.PropertyName == nameof(tally.TallyResultsChanging))
            {
                OnPropertyChanged(nameof(OutputChanging));
            }
        }

        /// <summary>
        /// Flag whether the tally is currently running.
        /// </summary>
        public bool TallyIsRunning => tally.TallyIsRunning;

        /// <summary>
        /// The string containing the current tally progress or results.
        /// Creates a notification event if the contents change.
        /// </summary>
        public string Output => tally.TallyResults;

        /// <summary>
        /// The piecemeal updates to the tally's TallyResults, before
        /// TallyResults is changed.
        /// </summary>
        public string OutputChanging => tally.TallyResultsChanging;

        /// <summary>
        /// Flag whether there's any text in the Output property.
        /// </summary>
        public bool HasOutput => tally.HasTallyResults;

        /// <summary>
        /// Redirection for user defined task values.
        /// </summary>
        public HashSet<string> UserDefinedTasks => VoteCounter.Instance.UserDefinedTasks;
        #endregion

        #region Section: Tally Commands
        CancellationTokenSource cts;

        /// <summary>
        /// Requests that the tally class update its current results.
        /// </summary>
        public async void UpdateOutput()
        {
            await tally.UpdateResults().ConfigureAwait(false);
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
                        await tally.RunAsync(SelectedQuest, cts.Token).ConfigureAwait(false);
                    }
                    catch (OperationCanceledException)
                    {
                        // Operation was cancelled.  Verify whether we requested the cancellation.
                        if (cts.IsCancellationRequested)
                        {
                            // User requested cancellation. Add it to the output display.
                            tally.TallyResults += "Tally Cancelled!\n";
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
            if (cts == null || cts.IsCancellationRequested)
            {
                tally.TallyIsRunning = false;
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
            tally.ClearPageCache();
        }
        #endregion

        #endregion

        #region Section: Vote Counter
        public ObservableCollectionExt<string> AllVotesCollection { get; }
        public ObservableCollectionExt<string> AllVotersCollection { get; }

        private void BindVoteCounter()
        {
            VoteCounter.Instance.PropertyChanged += VoteCounter_PropertyChanged;
        }

        /// <summary>
        /// Update the observable collection of votes.
        /// </summary>
        private void UpdateVotesCollection()
        {
            var votesWithSupporters = VoteCounter.Instance.GetVotesCollection(VoteType.Vote);

            List<string> votes = votesWithSupporters.Keys
                .Concat(VoteCounter.Instance.GetCondensedRankVotes())
                .Distinct(Agnostic.StringComparer).ToList();

            AllVotesCollection.Replace(votes);

            OnPropertyChanged(nameof(AllVotesCollection));
        }

        /// <summary>
        /// Update the observable collection of voters.
        /// </summary>
        private void UpdateVotersCollection()
        {
            var voteVoters = VoteCounter.Instance.GetVotersCollection(VoteType.Vote);
            var rankVoters = VoteCounter.Instance.GetVotersCollection(VoteType.Rank);

            List<string> voters = voteVoters.Select(v => v.Key)
                .Concat(rankVoters.Select(v => v.Key))
                .Distinct().OrderBy(v => v).ToList();

            AllVotersCollection.Replace(voters);

            OnPropertyChanged(nameof(AllVotersCollection));
        }

        private void VoteCounter_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (!VoteCounter.Instance.VoteCounterIsTallying)
            {
                if (e.PropertyName == nameof(VoteCounter.Instance.VoteCounterIsTallying))
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

        public bool VoteExists(string vote, VoteType voteType) => VoteCounter.Instance.HasVote(vote, voteType);

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
        private void SetupCommands()
        {
            NonCommandPropertyChangedValues.Add("NewRelease");

            AddQuestCommand = new RelayCommand(this, DoAddQuest, CanAddQuest);
            RemoveQuestCommand = new RelayCommand(this, DoRemoveQuest, CanRemoveQuest);

            RunTallyCommand = new AsyncRelayCommand(this, DoRunTallyAsync, CanRunTally);
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
