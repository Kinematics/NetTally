using System;
using System.Globalization;

namespace NetTally.Types.Components
{

    public record PostId : IComparable<PostId>, IComparable<long>, IComparable<string>
    {
        public static readonly PostId Zero = new PostId(0);

        public string Text { get; init; }
        public long Value { get; init; }

        public PostId(string postId)
        {
            if (string.IsNullOrWhiteSpace(postId))
                throw new ArgumentException("Post ID is not valid.", nameof(postId));

            Text = postId;

            if (long.TryParse(postId, NumberStyles.AllowThousands, CultureInfo.InvariantCulture, out long postIdValue) &&
                postIdValue > 0)
            {
                Value = postIdValue;
            }
        }

        public PostId(long postId)
        {
            if (postId < 0)
                throw new ArgumentOutOfRangeException(nameof(postId), $"Post ID number '{postId}' is not valid.");

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
            if (other is null)
                return 1;

            if (Value == 0 && other.Value == 0)
                return Text.CompareTo(other.Text);

            return Value.CompareTo(other.Value);
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
