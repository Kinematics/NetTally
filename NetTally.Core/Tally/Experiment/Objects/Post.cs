using System;
using System.Collections.Generic;
using NetTally.Forums;

namespace NetTally.Votes.Experiment
{
    /// <summary>
    /// Class to encapsulate a user's post.
    /// </summary>
    class Post
    {
        #region Properties
        /// <summary>
        /// Gets the identity of the user who made the post, and where the post came from.
        /// </summary>
        public Identity Identity { get; }
        /// <summary>
        /// Gets the thread post number.
        /// </summary>
        public int ThreadPostNumber { get; }
        /// <summary>
        /// Gets the full message of this post.
        /// </summary>
        public string Message { get; }
        /// <summary>
        /// Gets the vote contained within this post, if any.
        /// </summary>
        public Vote Vote { get; }
        /// <summary>
        /// Gets whether this instance has a vote.
        /// </summary>
        public bool HasVote { get; }
        #endregion

        #region Constructor
        /// <summary>
        /// Initializes a new instance of the <see cref="Post"/> class.
        /// </summary>
        /// <param name="author">The author of the post.</param>
        /// <param name="postID">The post identifier.</param>
        /// <param name="threadPostNumber">The post number.</param>
        /// <param name="message">The post message.</param>
        public Post(string author, string postID, int threadPostNumber, string message, IForumAdapter adapter = null)
        {
            Identity = new Identity(author, postID, forumAdapter: adapter);
            ThreadPostNumber = threadPostNumber;
            Message = message ?? string.Empty;

            if (!string.IsNullOrEmpty(Message))
            {
                Vote = new Vote(this, Message);
            }

            HasVote = Vote?.IsValid ?? false;
        }
        #endregion

        #region Class overrides
        /// <summary>
        /// Returns a hash code for this instance.
        /// </summary>
        /// <returns>
        /// A hash code for this instance, suitable for use in hashing algorithms and data structures like a hash table. 
        /// </returns>
        public override int GetHashCode()
        {
            return Identity.GetHashCode();
        }
        #endregion



        public bool ForceProcess { get; set; }
        public bool Processed { get; set; }

        internal void Prepare(List<VotePartition> workingVoteLines)
        {
            if (workingVoteLines == null)
                return;

            Vote.SetWorkingVote(workingVoteLines);

            Processed = false;
            ForceProcess = false;
        }
    }
}
