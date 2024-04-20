using System;
using NetTally.Utility;
using NetTally.Utility.Comparers;
using NetTally.Types.Enums;
using NetTally.Data;

namespace NetTally.Tally.Components
{
    public class Origin : IComparable, IComparable<Origin>, IEquatable<Origin>
    {
        #region Construction and Properties
        public Author Author { get; }
        public IdentityType AuthorType { get; }
        public PostId ID { get; }
        public int ThreadPostNumber { get; }
        public DateTime Timestamp { get; }
        public Uri Thread { get; }
        public string Permalink { get; }
        public Origin Source { get; }

        public static readonly Origin Empty = new("-", "0", 0,
            DateTime.MinValue, new Uri(StringData.ExampleHostString), StringData.ExampleHostString);

        private static readonly Uri exampleUri = new(StringData.ExampleHostString);
        private readonly int hash;
        private readonly bool limitedToName;

        public Origin(string author, string postID, int postNumber, Uri thread, string permalink)
            : this(author, IdentityType.User, new PostId(postID),
                  postNumber, DateTime.MinValue, thread, permalink, Empty)
        { }

        public Origin(string author, string postID, int postNumber, DateTime timestamp, Uri thread, string permalink)
            : this(author, IdentityType.User, new PostId(postID),
                  postNumber, timestamp, thread, permalink, Empty)
        { }

        public Origin(string author, IdentityType identityType)
            : this(author, identityType, PostId.Zero, 0, DateTime.MinValue,
                  exampleUri, string.Empty, Empty)
        {
            limitedToName = true;
        }

        private Origin(string author, IdentityType identityType, PostId postId, int postNumber, DateTime timestamp,
            Uri thread, string permalink, Origin source)
        {
            Author = new Author(author);
            AuthorType = identityType;
            ID = postId;
            ThreadPostNumber = postNumber;
            Timestamp = timestamp;
            Thread = thread;
            Permalink = permalink;
            Source = source;
            hash = ComputeHash();
        }

        public Origin GetPlanOrigin(string planName)
        {
            return new Origin(planName, IdentityType.Plan, ID, ThreadPostNumber, Timestamp, Thread, Permalink, this);
        }
        #endregion

        #region Comparisons and Equality
        private int ComputeHash() => Agnostic.CaseInsensitiveComparer.GetHashCode(Author);
        public override int GetHashCode() => hash;

        public override string ToString()
        {
            if (AuthorType == IdentityType.Plan)
                return $"{Strings.PlanNameMarker}{Author.Name}";
            else
                return Author.Name;
        }

        public static int Compare(Origin? first, Origin? second)
        {
            if (ReferenceEquals(first, second))
                return 0;
            if (first is null)
                return -1;
            if (second is null)
                return 1;

            int result = first.AuthorType.CompareTo(second.AuthorType);

            if (result == 0 && !first.limitedToName && !second.limitedToName)
            {
                result = first.Thread.AbsoluteUri.CompareTo(second.Thread.AbsoluteUri);
            }

            if (result == 0)
            {
                result = Agnostic.CaseInsensitiveComparer.Compare(first.Author.Name, second.Author.Name);
            }

            return result;
        }

        public override bool Equals(object? obj) => Compare(this, obj as Origin) == 0;
        public bool Equals(Origin? other) => Compare(this, other) == 0;
        public int CompareTo(object? obj) => Compare(this, obj as Origin);
        public int CompareTo(Origin? other) => Compare(this, other);
        public static bool operator >(Origin? first, Origin? second) => Compare(first, second) == 1;
        public static bool operator <(Origin? first, Origin? second) => Compare(first, second) == -1;
        public static bool operator >=(Origin? first, Origin? second) => Compare(first, second) >= 0;
        public static bool operator <=(Origin? first, Origin? second) => Compare(first, second) <= 0;
        public static bool operator ==(Origin? first, Origin? second) => Compare(first, second) == 0;
        public static bool operator !=(Origin? first, Origin? second) => Compare(first, second) != 0;

        #endregion
    }
}
