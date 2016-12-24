using System;
using System.Collections.Generic;
using System.Linq;

namespace NetTally.Votes.Experiment
{
    public class VotingCounter
    {
        public List<PostComponents> PostsList { get; private set; }
        public IQuest CurrentQuest { get; private set; }

        public void CountVotes(IQuest quest, IEnumerable<PostComponents> posts)
        {
            if (posts == null)
                throw new ArgumentNullException(nameof(posts));

            CurrentQuest = quest ?? throw new ArgumentNullException(nameof(quest));
            PostsList = new List<PostComponents>(posts);

            CountVotes();
        }

        public void CountVotes()
        {
            if (CurrentQuest == null)
                return;

            if (PostsList == null || PostsList.Count == 0)
                return;

            VotingRecords.Reset();

            foreach (var post in PostsList)
            {
                PreprocessPosts(post, CurrentQuest);
            }


            // Store all voters.  
            // If a poster votes multiple times, the last post will have its ID stored.
            foreach (var post in PostsList)
            {
                VotingRecords.AddVoter(post.Author, post.ID);
            }

            // Preprocessing Phase 1 (Only plans with contents are counted as plans.)
            foreach (var post in PostsList)
            {
                VoteConstructor.PreprocessPlansWithContent(post, CurrentQuest);
            }

            // Preprocessing Phase 2 (Full-post plans may be named (ie: where the plan name has no contents).)
            // Total vote must have multiple lines.
            foreach (var post in PostsList)
            {
                VoteConstructor.PreprocessPlanLabelsWithContent(post, CurrentQuest);
            }

            // Preprocessing Phase 3 (Full-post plans may be named (ie: where the plan name has no contents).)
            // Total vote may be only one line.
            foreach (var post in PostsList)
            {
                VoteConstructor.PreprocessPlanLabelsWithoutContent(post, CurrentQuest);
            }

            // Once all the plans are in place, set the working votes for each post.
            foreach (var post in PostsList)
            {
                post.SetWorkingVote(p => VoteConstructor.GetWorkingVote(p));
            }

            var unprocessed = PostsList;

            // Loop as long as there are any more to process.
            while (unprocessed.Any())
            {
                // Get the list of the ones that were processed.
                var processed = unprocessed.Where(p => VoteConstructor.ProcessPost(p, CurrentQuest) == true).ToList();

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

        private void PreprocessPosts(PostComponents post, IQuest currentQuest)
        {
            // Record the author and ID of each post.
            // If the author makes multiple posts, the later IDs will overwrite the earlier ones.
            VotingRecords.AddVoter(post.Author, post.ID);

            var plans = GetPlans(post);
        }

        private (List<string> contentPlans, List<string> labelPlans, List<string> labelNames) GetPlans(PostComponents post)
        {
            throw new NotImplementedException();
        }
    }
}
