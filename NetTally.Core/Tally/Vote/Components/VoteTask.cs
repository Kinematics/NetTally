using System.Diagnostics.CodeAnalysis;
using NetTally.Utility;
using NetTally.Utility.Comparers;

namespace NetTally.Tally.Vote.Components;

/// <summary>
/// Data type to store a vote task.
/// </summary>
/// <param name="Name">The name of the task.</param>
public sealed record VoteTask(string Name);

/// <summary>
/// Extension class for creating <see cref="VoteTask"/> objects.
/// </summary>
public static class VoteTaskCreation
{
    extension(VoteTask)
    {
        /// <summary>
        /// The default, empty, <see cref="VoteTask"/>
        /// </summary>
        public static VoteTask Empty => _empty;

        /// <summary>
        /// Create a new <see cref="VoteTask"/> based on the provided input.
        /// </summary>
        /// <param name="task">The text for the task.</param>
        /// <returns>A new <see cref="VoteTask"/></returns>
        public static VoteTask Create(string task)
        {
            if (string.IsNullOrWhiteSpace(task))
                return VoteTask.Empty;

            task = task.RemoveUnsafeCharacters().Trim();

            return new VoteTask(task);
        }
    }

    private static readonly VoteTask _empty = new("");
}

/// <summary>
/// Comparer class for <see cref="VoteTask"/> objects.
/// </summary>
public class VoteTaskComparer : IEqualityComparer<VoteTask>, IComparer<VoteTask>
{
    public static VoteTaskComparer Instance { get; } = new();

    public int Compare(VoteTask? x, VoteTask? y)
    {
        if (ReferenceEquals(x, y)) return 0;
        if (x is null) return -1;
        if (y is null) return 1;

        return Agnostic.CaseInsensitiveComparer.Compare(x.Name, y.Name);
    }

    public bool Equals(VoteTask? x, VoteTask? y)
    {
        if (x is null || y is null) return false;
        if (ReferenceEquals(x, y)) return true;

        return Compare(x, y) == 0;
    }

    public int GetHashCode([DisallowNull] VoteTask obj)
    {
        return Agnostic.CaseInsensitiveComparer.GetHashCode(obj.Name);
    }
}
