using System;

namespace NetTally.Votes.Experiment
{
    public class Identity
    {
        public string Name { get; }
        public string PostID { get; }
        public bool IsPlan { get; }
        public int Number { get; }

        private string Variant => Number > 0 ? $" ({Number})" : "";
        public string VariantName => IsPlan ? $"{Name}{Variant}" : Name;
        public string FullName => IsPlan ? $"◈{Name}{Variant}" : Name;

        /// <summary>
        /// Initializes a new instance of the <see cref="Identity"/> class.
        /// </summary>
        /// <param name="name">The user or plan name.</param>
        /// <param name="postID">The post identifier.</param>
        /// <param name="isPlan">if set to <c>true</c> [is plan].</param>
        /// <exception cref="System.ArgumentNullException"/>
        public Identity(string name, string postID, bool isPlan = false, int number = 0)
        {
            if (string.IsNullOrEmpty(name))
                throw new ArgumentNullException(nameof(name));
            if (string.IsNullOrEmpty(postID))
                throw new ArgumentNullException(nameof(postID));

            Name = name;
            PostID = postID;
            IsPlan = isPlan;
            Number = number;
        }

        public override int GetHashCode()
        {
            return Name.GetHashCode() ^ PostID.GetHashCode() ^ IsPlan.GetHashCode();
        }
    }
}
