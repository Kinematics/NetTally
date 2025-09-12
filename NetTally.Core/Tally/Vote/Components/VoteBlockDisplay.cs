namespace NetTally.Tally.Vote.Components;

/// <summary>
/// Display class for <see cref="VoteBlock"/> objects.
/// </summary>
public static class VoteBlockDisplay
{
    public static string ToString(VoteBlock block)
    {
        return block.Lines
            .Select((a, b) => b == 0
                ? VoteLineDisplay.ToOverrideString(a, block.Marker.Display(), block.Task.Name)
                : VoteLineDisplay.ToString(a))
            .Aggregate((a, b) => $"{a}\n{b}");
    }

    public static string ToOutputString(VoteBlock block, string? marker = null, string? subMarker = null)
    {
        return block.Lines
            .Select((a, b) => b == 0
                ? VoteLineDisplay.ToOutputString(a, marker, block.Task.Name)
                : VoteLineDisplay.ToOutputString(a, subMarker))
            .Aggregate((a, b) => $"{a}\n{b}");
    }

    public static string ToManageVotesString(VoteBlock block)
    {
        return ToOutputString(block, marker: "", subMarker: "");
    }

    public static string ToComparableString(VoteBlock block)
    {
        return block.Lines
            .Select((a, b) => b == 0
                ? VoteLineDisplay.ToComparableString(a, block.Task.Name)
                : VoteLineDisplay.ToComparableString(a))
            .Aggregate((a, b) => $"{a}\r\n{b}");
    }
}
