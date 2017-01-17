using System;
using System.Collections.Generic;
using System.Text;

namespace NetTally.Votes.Experiment
{
    public class Post
    {
        public string Author { get; }
        public string ID { get; }
        public int IDNumber { get; }
        public int Number { get; }
        public string Message { get; }
        public Vote Vote { get; }
        public bool HasVote => Vote?.IsValid ?? false;

        public bool ForceProcess { get; set; }

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

            try
            {
                Vote = new Vote(Message);
            }
            catch (ArgumentNullException)
            {
                Vote = null;
            }
        }

        public override int GetHashCode()
        {
            return Author.GetHashCode() ^ ID.GetHashCode() ^ Number.GetHashCode();
        }

        internal void SetWorkingVote(Func<Post, List<string>> p)
        {
            throw new NotImplementedException();
        }
    }
}
