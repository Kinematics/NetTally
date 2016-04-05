using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

using NetTally;
using NetTally.Utility;

namespace NetTally.ViewModels
{
    public class MainViewModel : INotifyPropertyChanged, IDisposable
    {
        public MainViewModel(QuestCollectionWrapper config)
        {
            if (config == null)
                throw new ArgumentNullException(nameof(config));

            QuestList = config.QuestCollection;
            QuestList.Sort();
            SelectedQuest = QuestList[config.CurrentQuest];

            BindCheckForNewRelease();
            BindTally();

            AllVotesCollection = new ObservableCollectionExt<string>();
            AllVotersCollection = new ObservableCollectionExt<string>();

            BindVoteCounter();
        }

        #region IDisposable
        bool _disposed = false;

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
            }

            _disposed = true;
        }
        #endregion

        #region Implement INotifyPropertyChanged interface
        /// <summary>
        /// Event for INotifyPropertyChanged.
        /// </summary>
        public event PropertyChangedEventHandler PropertyChanged;

        /// <summary>
        /// Function to raise events when a property has been changed.
        /// </summary>
        /// <param name="propertyName">The name of the property that was modified.</param>
        protected void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
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
            checkForNewRelease.Update();
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
            }
        }

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

        public bool AddNewQuest()
        {
            IQuest newEntry = QuestList.AddNewQuest();

            if (newEntry == null)
                return false;

            SelectedQuest = newEntry;

            return true;
        }

        public bool RemoveQuest()
        {
            if (SelectedQuest == null)
                return false;

            int index = QuestList.IndexOf(SelectedQuest);
            QuestList.Remove(SelectedQuest);

            if (index >= QuestList.Count)
                index = QuestList.Count - 1;

            if (index < 0)
                SelectedQuest = null;
            else
                SelectedQuest = QuestList[index];

            return true;
        }
        #endregion

        #region Section: Tally & Results Binding
        /// Tally class object.
        Tally tally = new Tally();

        /// <summary>
        /// Bind event watcher to the class that handles running the tallies.
        /// </summary>
        private void BindTally()
        {
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
            }
            else if (e.PropertyName == "TallyResults")
            {
                OnPropertyChanged("Output");
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
        /// Redirection for user defined task values.
        /// </summary>
        public HashSet<string> UserDefinedTasks => tally.UserDefinedTasks;
        #endregion

        #region Section: Tally Commands
        CancellationTokenSource cts;

        /// <summary>
        /// Runs a tally on the currently selected quest.
        /// </summary>
        /// <returns></returns>
        public async Task Tally()
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
                    catch (Exception)
                    {
                        // Re-throw exceptions if we didn't request a cancellation.
                        if (!cts.IsCancellationRequested)
                        {
                            // If an exception happened and we haven't already requested a cancellation, do so.
                            cts.Cancel();
                            throw;
                        }
                    }
                }
            }
            finally
            {
                cts = null;
            }
        }

        /// <summary>
        /// Cancels the currently running tally, if any.
        /// </summary>
        public void Cancel()
        {
            cts?.Cancel();
        }

        /// <summary>
        /// Requests that the tally class clear its cache.
        /// </summary>
        public void ClearCache()
        {
            tally.ClearPageCache();
        }

        /// <summary>
        /// Requests that the tally class update its current results.
        /// </summary>
        public void Update()
        {
            tally.UpdateResults();
        }

        #endregion

        #region Section: Vote Counter
        public ObservableCollectionExt<string> AllVotesCollection { get; }
        public ObservableCollectionExt<string> AllVotersCollection { get; }

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
                    .Distinct().ToList();

                AllVotesCollection.Replace(votes);

                var voteVoters = VoteCounter.Instance.GetVotersCollection(VoteType.Vote);
                var rankVoters = VoteCounter.Instance.GetVotersCollection(VoteType.Rank);

                List<string> voters = voteVoters.Select(v => v.Key)
                    .Concat(rankVoters.Select(v => v.Key))
                    .Distinct().OrderBy(v => v).ToList();

                AllVotersCollection.Replace(voters);

                OnPropertyChanged("VoteCounter");
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
        #endregion
    }
}
