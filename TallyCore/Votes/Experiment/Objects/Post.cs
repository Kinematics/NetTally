using System;
using System.Collections.Generic;

namespace NetTally.Votes.Experiment
{
    /// <summary>
    /// Class to encapsulate a user's post.
    /// </summary>
    public class Post
    {
        public string Author { get; }
        public string ID { get; }
        public int IDNumber { get; }
        public int Number { get; }
        public string Message { get; }
        public Vote Vote { get; }
        public bool HasVote => Vote?.IsValid ?? false;

        public Post(string author, string postID, int postNumber, string message)
        {
            Author = author;
            ID = postID;
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

        public override int GetHashCode()
        {
            return Author.GetHashCode() ^ ID.GetHashCode() ^ Number.GetHashCode();
        }




        public bool ForceProcess { get; set; }

        internal void SetWorkingVote(Func<Post, List<string>> p)
        {
            throw new NotImplementedException();
        }
    }
}
