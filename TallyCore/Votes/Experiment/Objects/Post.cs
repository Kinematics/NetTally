using System;
using System.Collections.Generic;

namespace NetTally.Votes.Experiment
{
    /// <summary>
    /// Class to encapsulate a user's post.
    /// </summary>
    public class Post
    {
        public Identity Identity { get; }
        public int IDNumber { get; }
        public int Number { get; }
        public string Message { get; }
        public Vote Vote { get; }
        public bool HasVote => Vote?.IsValid ?? false;

        /// <summary>
        /// Initializes a new instance of the <see cref="Post"/> class.
        /// </summary>
        /// <param name="author">The author of the post.</param>
        /// <param name="postID">The post identifier.</param>
        /// <param name="postNumber">The post number.</param>
        /// <param name="message">The post message.</param>
        public Post(string author, string postID, int postNumber, string message)
        {
            Identity = new Identity(author, postID);
            Number = postNumber;
            Message = message ?? string.Empty;

            if (int.TryParse(postID, out int postIDNum))
            {
                IDNumber = postIDNum;
            }

            if (!string.IsNullOrEmpty(Message))
            {
                Vote = new Vote(this, Message);
            }
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
