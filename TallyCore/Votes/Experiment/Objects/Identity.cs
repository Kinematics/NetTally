using System;

namespace NetTally.Votes.Experiment
{
    public class Identity
    {
        public string Name { get; }
        public string PostID { get; }

        public Identity(string name, string postID)
        {
            if (string.IsNullOrEmpty(name))
                throw new ArgumentNullException(nameof(name));
            if (string.IsNullOrEmpty(postID))
                throw new ArgumentNullException(nameof(postID));

            Name = name;
            PostID = postID;
        }
    }
}
