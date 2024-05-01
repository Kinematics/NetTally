using System;
using System.Globalization;

namespace NetTally.Tally.Components
{
    /// <summary>
    /// A record for a post ID value.
    /// </summary>
    public record PostId : IComparable<PostId>, IComparable<long>, IComparable<string>
    {
        public static readonly PostId Zero = new(0);

        public string Text { get; init; }
        public long Value { get; init; }

        public PostId(string postId)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(postId);

            Text = postId;

            if (long.TryParse(postId, NumberStyles.AllowThousands, CultureInfo.InvariantCulture, out long postIdValue))
            {
                if (postIdValue > 0)
                    Value = postIdValue;
            }
            //else if (long.TryParse(postId, NumberStyles.HexNumber, CultureInfo.InvariantCulture, out long postIdHexValue))
            //{
            //    if (postIdHexValue > 0)
            //        Value = postIdHexValue;
            //}
        }

        public PostId(long postId)
        {
            ArgumentOutOfRangeException.ThrowIfNegative(postId);

            Value = postId;
            Text = postId.ToString();
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

        public int CompareTo(PostId? other)
        {
            return Compare(this, other);
        }

        public int CompareTo(long other)
        {
            return Value.CompareTo(other);
        }

        public int CompareTo(string? other)
        {
            return Text.CompareTo(other);
        }

        public static bool operator >(PostId first, PostId second) => first.CompareTo(second) == 1;
        public static bool operator <(PostId first, PostId second) => first.CompareTo(second) == -1;
        public static bool operator >=(PostId first, PostId second) => first.CompareTo(second) >= 0;
        public static bool operator <=(PostId first, PostId second) => first.CompareTo(second) <= 0;
        public static bool operator >(PostId first, long second) => first.CompareTo(second) == 1;
        public static bool operator <(PostId first, long second) => first.CompareTo(second) == -1;
        public static bool operator >=(PostId first, long second) => first.CompareTo(second) >= 0;
        public static bool operator <=(PostId first, long second) => first.CompareTo(second) <= 0;
        public static bool operator ==(PostId first, long second) => first.CompareTo(second) == 0;
        public static bool operator !=(PostId first, long second) => first.CompareTo(second) != 0;
        public static bool operator >(PostId first, string second) => first.CompareTo(second) == 1;
        public static bool operator <(PostId first, string second) => first.CompareTo(second) == -1;
        public static bool operator >=(PostId first, string second) => first.CompareTo(second) >= 0;
        public static bool operator <=(PostId first, string second) => first.CompareTo(second) <= 0;
        public static bool operator ==(PostId first, string second) => first.CompareTo(second) == 0;
        public static bool operator !=(PostId first, string second) => first.CompareTo(second) != 0;
    }
}
