using System.Diagnostics.CodeAnalysis;
using NetTally.Tally.Vote.Component;
using NetTally.Utility.Comparers;

namespace NetTally.Tally.Vote.Comparers;

/// <summary>
/// Comparer class for <see cref="VoteContent"/> objects.
/// </summary>
public class VoteContentComparer : IEqualityComparer<VoteContent>, IComparer<VoteContent>
{
    private VoteContentComparer() { }

    public VoteContentComparer(Quest quest)
    {
        this.quest = quest;
    }

    private readonly Quest? quest;

    public static VoteContentComparer Instance { get; } = new();

    public int Compare(VoteContent? x, VoteContent? y)
    {
        if (ReferenceEquals(x, y)) return 0;
        if (x is null) return -1;
        if (y is null) return 1;

        var contentComparer = quest?.CurrentComparer
            ?? Agnostic.CurrentStringComparer;

        return contentComparer.Compare(x.CleanContent, y.CleanContent);
    }

    public bool Equals(VoteContent? x, VoteContent? y)
    {
        if (x is null || y is null) return false;
        if (ReferenceEquals(x, y)) return true;

        return Compare(x, y) == 0;
    }

    public int GetHashCode([DisallowNull] VoteContent obj)
    {
        return Agnostic.InsensitiveComparer.GetHashCode(obj.CleanContent);
    }
}
