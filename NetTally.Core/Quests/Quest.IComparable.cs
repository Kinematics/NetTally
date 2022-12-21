﻿using System;

namespace NetTally
{
    /// <summary>
    /// Implement <see cref="IComparable"/> for <see cref="NetTally.Quest"/> class.
    /// </summary>
    public partial class Quest : IComparable<Quest>, IComparable, IEquatable<Quest>
    {
        /// <summary>
        /// Compares the current instance with another object of the same type and returns an integer that
        /// indicates whether the current instance precedes, follows, or occurs in the same position in the
        /// sort order as the other object.
        /// Sort order is determined by DisplayName, with a case-insensitive check.
        /// </summary>
        /// <param name="obj">An object to compare with this instance.</param>
        /// <returns>
        /// Returns -1 if this is before obj, 0 if it's the same, and 1 if it's after obj.
        /// </returns>
        public int CompareTo(object? obj) => Compare(this, obj as Quest);

        public int CompareTo(Quest? other) => Compare(this, other);

        /// <summary>
        /// Hash code for the quest object.
        /// </summary>
        /// <returns></returns>
        public override int GetHashCode() => QuestId.GetHashCode();

        /// <summary>
        /// IComparer function.
        /// </summary>
        /// <param name="left">The first object being compared.</param>
        /// <param name="right">The second object being compared.</param>
        /// <returns>Returns a negative value if left is 'before' right, 0 if they're equal, and
        /// a positive value if left is 'after' right.</returns>
        public static int Compare(Quest? left, Quest? right)
        {
            if (ReferenceEquals(left, right))
                return 0;
            if (left is null)
                return -1;
            if (right is null)
                return 1;

            return string.Compare(left.DisplayName.ToLowerInvariant(), right.DisplayName.ToLowerInvariant(), StringComparison.Ordinal);
        }

        public bool Equals(Quest? other)
        {
            if (other is null)
                return false;

            if (ReferenceEquals(this, other))
                return true;

            return QuestId == other.QuestId;
        }

        public override bool Equals(object? obj)
        {
            return Equals(obj as Quest);
        }

        public static bool operator ==(Quest? left, Quest? right)
        {
            if (left is null)
            {
                return right is null;
            }
            return left.Equals(right);
        }

        public static bool operator !=(Quest? left, Quest? right) => !(left == right);

        public static bool operator <(Quest left, Quest right) => (Compare(left, right) < 0);

        public static bool operator >(Quest left, Quest right) => (Compare(left, right) > 0);

    }
}
