using System;
using NetTally.Forums;

namespace NetTally.Votes.Experiment
{
    public class Identity
    {
        public string Name { get; }
        public string PostID { get; }
        public IdentityType IdentityType { get; }
        public bool IsPlan { get; }
        public int Number { get; set; }

        private string Variant => Number > 0 ? $" ({Number})" : "";
        public string VariantName => IsPlan ? $"{Name}{Variant}" : Name;
        public string FullName => IsPlan ? $"◈{Name}{Variant}" : Name;

        private IForumAdapter ForumAdapter { get; }

        #region Constructors
        /// <summary>
        /// Initializes a new instance of the <see cref="Identity"/> class.
        /// </summary>
        /// <param name="name">The user or plan name.</param>
        /// <param name="postID">The post identifier.</param>
        /// <param name="isPlan">if set to <c>true</c> [is plan].</param>
        /// <exception cref="System.ArgumentNullException"/>
        public Identity(string name, string postID,
            IdentityType identityType = IdentityType.User,
            IForumAdapter forumAdapter = null,
            int number = 0)
        {
            if (string.IsNullOrEmpty(name))
                throw new ArgumentNullException(nameof(name));
            if (string.IsNullOrEmpty(postID))
                throw new ArgumentNullException(nameof(postID));

            Name = name;
            PostID = postID;
            IdentityType = identityType;
            IsPlan = identityType == IdentityType.Plan;
            ForumAdapter = forumAdapter;
            Number = number;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="Identity"/> class, using
        /// an existing Identity for some parameters.
        /// </summary>
        /// <param name="name">The name.</param>
        /// <param name="originatingIdentity">The originating identity.</param>
        /// <param name="identityType">Type of the identity.</param>
        public Identity(string name, Identity originatingIdentity, IdentityType identityType = IdentityType.User)
            : this(name, originatingIdentity.PostID, identityType, originatingIdentity.ForumAdapter, 0)
        {
        }
        #endregion

        public override int GetHashCode()
        {
            return Name.GetHashCode() ^ PostID.GetHashCode() ^ IsPlan.GetHashCode() ^ Number.GetHashCode();
        }

        public override bool Equals(object obj)
        {
            if (obj is Identity other)
            {
                if (Name == other.Name && PostID == other.PostID && IsPlan == other.IsPlan && Number == other.Number)
                    return true;
            }

            return false;
        }

        public override string ToString()
        {
            return FullName;
        }

        public string ToLink()
        {
            string permalink = "";

            if (ForumAdapter != null)
            {
                permalink = ForumAdapter.GetPermalinkForId(PostID);
            }

            return $"[url=\"{permalink}\"]{FullName}[/url]";
        }
    }
}
