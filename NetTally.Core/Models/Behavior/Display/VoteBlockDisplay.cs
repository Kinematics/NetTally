namespace NetTally.Models;

/// <summary>
/// Display class for <see cref="VoteBlock"/> objects.
/// </summary>
public static class VoteBlockDisplay
{
    public static string ToString(VoteBlock block)
    {
        return block.Lines
            .Select((a, b) => b == 0
                ? a.Display(block.Marker.Display, block.Task.Name)
                : a.Display())
            .Aggregate((a, b) => $"{a}\n{b}");
    }

    public static string ToOutputString(VoteBlock block, string? marker = null, string? subMarker = null)
    {
        return block.Lines
            .Select((a, b) => b == 0
                ? a.DisplayOutput(marker, block.Task.Name)
                : a.DisplayOutput(subMarker))
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
                ? a.DisplayComparable(block.Task.Name)
                : a.DisplayComparable())
            .Aggregate((a, b) => $"{a}\r\n{b}");
    }
}
