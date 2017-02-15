using System;
using NetTally.Forums;

namespace NetTally.Votes.Experiment
{
    /// <summary>
    /// Class to hold a name (user or plan) and source location (forum and post ID)
    /// for any given vote.  Also allows for variants on a given name.
    /// </summary>
    public class Identity
    {
        #region Properties        
        /// <summary>
        /// Gets the name of this identity.
        /// </summary>
        public string Name { get; }
        /// <summary>
        /// Gets the post ID that this identity came from.
        /// </summary>
        public string PostID { get; }
        /// <summary>
        /// Gets the numeric value of the post ID that this identity came from.
        /// </summary>
        public Int64 PostIDValue { get; }
        /// <summary>
        /// Gets the type of the identity (user or plan).
        /// </summary>
        public IdentityType IdentityType { get; }
        /// <summary>
        /// Gets a shortcut value indicating whether this instance is a plan.
        /// </summary>
        public bool IsPlan { get; }
        /// <summary>
        /// Gets or sets the number that marks whether this is a variant on the identity name.
        /// </summary>
        public int Number { get; set; }

        /// <summary>
        /// Gets the name of the identity, including the variant, if applicable.
        /// </summary>
        public string VariantName => IsPlan ? $"{Name}{Variant}" : Name;
        /// <summary>
        /// Gets the fully qualified name of the identity, including plan character marker and variant number.
        /// </summary>
        public string FullName => IsPlan ? $"◈{Name}{Variant}" : Name;

        /// <summary>
        /// Gets the text for the variant number, if applicable.
        /// </summary>
        private string Variant => Number > 0 ? $" ({Number})" : "";

        /// <summary>
        /// Gets the forum adapter that was used to identify the original identity, in order
        /// to reliably create a permalink back to the original post.
        /// </summary>
        private IForumAdapter ForumAdapter { get; }
        /// <summary>
        /// Gets the host defined in the forum adapter, if any.
        /// </summary>
        public string Host => ForumAdapter?.Site.Host;
        #endregion

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

            Int64 value = 0;

            if (ForumAdapter != null)
                value = ForumAdapter.GetValueOfPostID(PostID);
            else
                Int64.TryParse(PostID, out value);

            PostIDValue = value;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="Identity"/> class, using
        /// an existing Identity for some parameters.
        /// </summary>
        /// <param name="name">The name.</param>
        /// <param name="originatingIdentity">The originating identity.</param>
        /// <param name="identityType">Type of the identity.</param>
        public Identity(string name, Identity originatingIdentity, IdentityType identityType = IdentityType.User)
            : this(name, originatingIdentity?.PostID, identityType, originatingIdentity?.ForumAdapter, 0)
        {
        }
        #endregion

        #region Overrides        
        /// <summary>
        /// Returns a hash code for this instance, suitable for use in hashing
        /// algorithms and data structures like a hash table.
        /// Two objects should have the same hash code if they are sufficiently
        /// similar to make it worth doing an explicit equality check.
        /// </summary>
        public override int GetHashCode()
        {
            return Name.GetHashCode() ^ (ForumAdapter?.Site.Host.GetHashCode() ?? 0);
        }

        /// <summary>
        /// Determines whether the specified <see cref="System.Object" />, is equal to this instance.
        /// </summary>
        /// <param name="obj">The <see cref="System.Object" /> to compare with this instance.</param>
        /// <returns>
        ///   <c>true</c> if the specified <see cref="System.Object" /> is equal to this instance; otherwise, <c>false</c>.
        /// </returns>
        public override bool Equals(object obj)
        {
            if (obj is Identity other)
            {
                return Name == other.Name && PostID == other.PostID && IdentityType == other.IdentityType
                    && Number == other.Number && Host == other.Host;
            }

            return false;
        }

        /// <summary>
        /// Determines whether this is a match to the identity name and forum host, without matching the exact post number.
        /// </summary>
        /// <param name="other">The Identity being compared against.</param>
        /// <returns>Returns true if both identities have the same name, type, and host.</returns>
        public bool Matches(Identity other)
        {
            if (other == null)
                return false;

            return Name == other.Name && IdentityType == other.IdentityType
                && Number == other.Number && Host == other.Host;
        }

        /// <summary>
        /// Returns a <see cref="System.String" /> that represents this instance.
        /// </summary>
        /// <returns>
        /// A <see cref="System.String" /> that represents this instance.
        /// </returns>
        public override string ToString()
        {
            return FullName;
        }

        /// <summary>
        /// Returns a BBCode-formatted string representing a permalink url pointing to
        /// the origin of this identity.
        /// </summary>
        /// <returns>Returns a BBCode [url] for the permalink to this identity.</returns>
        public string ToLink()
        {
            string permalink = "";

            if (ForumAdapter != null)
            {
                permalink = ForumAdapter.GetPermalinkForId(PostID);
            }

            return $"[url=\"{permalink}\"]{FullName}[/url]";
        }
        #endregion
    }
}
