using System;
using System.Collections.Generic;
using NetTally.Forums;

namespace NetTally.Votes.Experiment
{
    /// <summary>
    /// Class to encapsulate a user's post.
    /// </summary>
    public class Post
    {
        public Identity Identity { get; }
        public Vote Vote { get; }
        public bool HasVote { get; }

        public string Message { get; }

        public int PostIDNumber { get; }
        public int ThreadPostNumber { get; }

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

            if (int.TryParse(postID, out int postIDNum))
            {
                PostIDNumber = postIDNum;
            }

            if (!string.IsNullOrEmpty(Message))
            {
                Vote = new Vote(this, Message);
            }

            HasVote = Vote?.IsValid ?? false;
        }


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




        public bool ForceProcess { get; set; }
        public bool Processed { get; set; }

        internal void Prepare(Func<Post, List<string>> prepare)
        {
            if (prepare == null)
                throw new ArgumentNullException(nameof(prepare));

            //WorkingVote = prepare(this);

            Processed = false;
            ForceProcess = false;
        }
    }
}
