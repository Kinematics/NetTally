using System;
using System.Globalization;

namespace NetTally.Experiment3
{
    public class PostId : IComparable, IComparable<PostId>, IComparable<long>, IEquatable<PostId>, IEquatable<long>
    {
        public string Text { get; }
        public long Value { get; }

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

        public static int Compare(PostId? first, PostId? second)
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
            return obj switch
            {
                null => 1,
                PostId other => Compare(this, other),
                long otherValue => Compare(this, otherValue),
                _ => -1
            };
        }

        public override bool Equals(object obj)
        {
            return obj switch
            {
                null => false,
                PostId other => Equals(other),
                long longValue => Equals(longValue),
                _ => false
            };
        }

        public bool Equals(long other) => Compare(this, other) == 0;
        public bool Equals(PostId other) => Compare(this, other) == 0;
        public int CompareTo(long other) => Compare(this, other);
        public int CompareTo(PostId other) => Compare(this, other);

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
        #endregion
    }
}
