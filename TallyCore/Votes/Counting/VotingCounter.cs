using NetTally.Utility;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;

namespace NetTally.Votes.Experiment
{
    public class VotingCounter : INotifyPropertyChanged
    {
        public List<PostComponents> PostsList { get; private set; }
        public IQuest CurrentQuest { get; private set; }

        bool voteCounterIsTallying;

        /// <summary>
        /// Flag whether the tally is currently running.
        /// </summary>
        public bool VoteCounterIsTallying
        {
            get { return voteCounterIsTallying; }
            set
            {
                if (voteCounterIsTallying != value)
                {
                    voteCounterIsTallying = value;
                    OnPropertyChanged();
                }
            }
        }

        public async Task CountVotesInPosts(IQuest quest, IEnumerable<PostComponents> posts)
        {
            CurrentQuest = quest ?? throw new ArgumentNullException(nameof(quest));
            if (posts == null)
                throw new ArgumentNullException(nameof(posts));

            PostsList = new List<PostComponents>(posts);

            await CountVotesInCurrentPosts().ConfigureAwait(false);
        }

        public async Task CountVotesInCurrentPosts()
        {
            if (CurrentQuest == null)
                return;

            if (PostsList == null || PostsList.Count == 0)
                return;

            try
            {
                VoteCounterIsTallying = true;

                VotingRecords.Reset();

                if (PostsList == null || PostsList.Count == 0)
                    return;

                await Task.Run(() => PreprocessPosts()).ConfigureAwait(false);
                await Task.Run(() => ProcessPosts()).ConfigureAwait(false);
            }
            finally
            {
                VoteCounterIsTallying = false;
            }
        }

        /// <summary>
        /// The first half of tallying posts involves doing the preprocessing
        /// work on the plans in the post list.
        /// </summary>
        private void PreprocessPosts()
        {
            Dictionary<string, (PlanType planType, string plan, string postId)> planRepo = 
                new Dictionary<string, (PlanType planType, string plan, string postId)>(Agnostic.StringComparer);

            // Scan the post list once.
            foreach (var post in PostsList)
            {
                // Keep a record of the most recent post ID for each user.
                VotingRecords.AddVoter(post.Author, post.ID);

                // Pull out all plans from each post.
                var plans = VotingConstructor.GetPlansFromPost(post, CurrentQuest);

                // Examine each plan.
                foreach (var plan in plans)
                {
                    // The plan must be named to be valid.
                    if (string.IsNullOrEmpty(plan.planName))
                        continue;

                    if (planRepo.TryGetValue(plan.planName, out var existing))
                    {
                        // If the plan type found is 'higher' quality than any previously found plan types, replace the existing one.
                        if (plan.planType > existing.planType)
                        {
                            planRepo[plan.planName] = (plan.planType, plan.plan, post.ID);
                        }
                    }
                    else
                    {
                        // If the plan name doesn't already exist, add it.
                        planRepo.Add(plan.planName, (plan.planType, plan.plan, post.ID));
                    }
                }
            }

            // At the end, we should have a collection of the 'best' versions of each named plan.
            // Store them in the voting records.
            VotingRecords.AddPlans(planRepo);

            // Once all the plans are in place, set the working votes for each post.
            foreach (var post in PostsList)
            {
                post.SetWorkingVote(p => VotingConstructor.GetWorkingVote(p));
            }
        }

        /// <summary>
        /// The second half of tallying the posts involves cycling through for
        /// as long as future references need to be handled.
        /// </summary>
        private void ProcessPosts()
        {
            var unprocessed = PostsList;

            // Loop as long as there are any more to process.
            while (unprocessed.Any())
            {
                // Get the list of the ones that were processed.
                var processed = unprocessed.Where(p => VotingConstructor.ProcessPost(p, CurrentQuest) == true).ToList();

                // As long as some got processed, remove those from the unprocessed list
                // and let the loop run again.
                if (processed.Any())
                {
                    unprocessed = unprocessed.Except(processed).ToList();
                }
                else
                {
                    // If none got processed (and there must be at least some waiting on processing),
                    // Set the ForceProcess flag on them to avoid pending FutureReference waits.
                    foreach (var p in unprocessed)
                    {
                        p.ForceProcess = true;
                    }
                }
            }
        }


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

    }
}
