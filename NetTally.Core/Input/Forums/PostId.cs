using System;
using System.Globalization;

namespace NetTally.Forums
{
    public class PostId : IComparable, IComparable<PostId>, IComparable<long>, IComparable<string>, IEquatable<PostId>, IEquatable<long>, IEquatable<string>
    {
        public string Text { get; }
        public long Value { get; }

        public static PostId Zero = new PostId("0");

        public PostId(string input)
        {
            if (input == "")
                throw new ArgumentOutOfRangeException(nameof(input), "No post ID provided.");

            Text = input ?? throw new ArgumentNullException(nameof(input));

            if (long.TryParse(input, NumberStyles.AllowThousands, CultureInfo.InvariantCulture, out long inputNumber) 
                && inputNumber > 0)
            {
                Value = inputNumber;
            }
            else
            {
                Value = 0;
            }
        }

        #region Comparisons and Equality
        public override string ToString() => Text;
        public override int GetHashCode() => Value.GetHashCode();

        public static int Compare(PostId first, long second)
        {
            if (first is null)
                return -1;

            return first.Value.CompareTo(second);
        }

        public static int Compare(PostId first, PostId second)
        {
            if (ReferenceEquals(first, second))
                return 0;
            if (first is null)
                return -1;
            if (second is null)
                return 1;

            if (first.Value == 0 && second.Value == 0)
                return first.Text.CompareTo(second.Text);

            return first.Value.CompareTo(second.Value);
        }

        public int CompareTo(object obj)
        {
            switch (obj)
            {
                case PostId other:
                    return Compare(this, other);
                case long other:
                    return Compare(this, other);
                case string other:
                    return this.CompareTo(other);
                case null:
                    return 1;
                default:
                    return -1;
            }
        }

        public override bool Equals(object obj)
        {
            switch (obj)
            {
                case PostId other:
                    return Equals(other);
                case long other:
                    return Equals(other);
                case string other:
                    return Equals(other);
                case null:
                    return false;
                default:
                    return false;
            }
        }

        public bool Equals(long other) => Compare(this, other) == 0;
        public bool Equals(PostId other) => Compare(this, other) == 0;
        public int CompareTo(long other) => Compare(this, other);
        public int CompareTo(PostId other) => Compare(this, other);
        public int CompareTo(string other) => string.Compare(this.Text, other);
        public bool Equals(string other) => this.Text == other;

        public static bool operator >(PostId first, PostId second) => Compare(first, second) == 1;
        public static bool operator <(PostId first, PostId second) => Compare(first, second) == -1;
        public static bool operator >=(PostId first, PostId second) => Compare(first, second) >= 0;
        public static bool operator <=(PostId first, PostId second) => Compare(first, second) <= 0;
        public static bool operator ==(PostId first, PostId second) => Compare(first, second) == 0;
        public static bool operator !=(PostId first, PostId second) => Compare(first, second) != 0;
        public static bool operator >(PostId first, long second) => Compare(first, second) == 1;
        public static bool operator <(PostId first, long second) => Compare(first, second) == -1;
        public static bool operator >=(PostId first, long second) => Compare(first, second) >= 0;
        public static bool operator <=(PostId first, long second) => Compare(first, second) <= 0;
        public static bool operator ==(PostId first, long second) => Compare(first, second) == 0;
        public static bool operator !=(PostId first, long second) => Compare(first, second) != 0;
        public static bool operator >(PostId first, string second) => first.CompareTo(second) == 1;
        public static bool operator <(PostId first, string second) => first.CompareTo(second) == -1;
        public static bool operator >=(PostId first, string second) => first.CompareTo(second) >= 0;
        public static bool operator <=(PostId first, string second) => first.CompareTo(second) <= 0;
        public static bool operator ==(PostId first, string second) => first.CompareTo(second) == 0;
        public static bool operator !=(PostId first, string second) => first.CompareTo(second) != 0;
        #endregion
    }
}
