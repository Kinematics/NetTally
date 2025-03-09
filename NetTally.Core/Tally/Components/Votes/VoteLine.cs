using System.Diagnostics.CodeAnalysis;

namespace NetTally.Tally.Components.Votes;
/// <summary>
/// Data type for vote lines.
/// </summary>
/// <param name="Prefix">The prefix on the vote line.</param>
/// <param name="Marker">The voting marker.</param>
/// <param name="Task">The task assigned to the vote line.</param>
/// <param name="Content">The contents of the vote line.</param>
public record VoteLineType(PrefixType Prefix, Marker Marker, VoteTaskType Task, VoteContentType Content)
{
    public int Depth => Prefix.Depth;
    public bool HasTask => Task.Name.Length > 0;

    public override string ToString()
    {
        return $"{{{Prefix.Indent}[{Marker.Display()}][{Task.Name}] {Content.CleanContent}}}";
    }
}

/// <summary>
/// Static class for creating and modifying <see cref="VoteLineType"/> objects.
/// </summary>
public static class VoteLine
{
    public static VoteLineType Empty { get; } =
        new VoteLineType(Prefix.Empty, Markers.Empty, VoteTask.Empty, VoteContent.Empty);

    public static VoteLineType? Create(
        PrefixType? prefix,
        Marker? marker,
        VoteTaskType? task,
        VoteContentType? content)
    {
        if (prefix == null) return null;
        if (marker == null) return null;
        if (task == null) return null;
        if (content == null) return null;

        if (content == VoteContent.Empty) return null;

        return new VoteLineType(prefix, marker, task, content);
    }


    public static VoteLineType Promote(VoteLineType input, int promoteDepth = 1)
    {
        return input with { Prefix = Prefix.Reduce(input.Prefix, promoteDepth) };
    }

    public static VoteLineType FullPromote(VoteLineType input)
    {
        if (input.Depth > 0)
            return input with { Prefix = Prefix.Empty };

        return input;
    }
}

public static class VoteLineDisplay
{
    /// <summary>
    /// Formats the current object as a string.
    /// </summary>
    /// <returns>Returns a string representing the current object.</returns>
    public static string ToString(VoteLineType voteLine)
    {
        string task = voteLine.HasTask ? $"[{voteLine.Task.Name}]" : "";
        return $"{voteLine.Prefix.Indent}[{voteLine.Marker.Display()}]{task} {voteLine.Content.Content}";
    }

    /// <summary>
    /// Creates a string that displays the cleaned content, and without any particular marker.
    /// May display a provided task instead of the innate one.
    /// </summary>
    /// <returns>Returns a string representing the current object.</returns>
    public static string ToComparableString(VoteLineType voteLine, string? task = null)
    {
        task ??= voteLine.Task.Name;
        task = task.Length > 0 ? $"[{task}]" : "";
        return $"{voteLine.Prefix.Indent}[]{task} {voteLine.Content.CleanContent}";
    }

    /// <summary>
    /// Creates a string that displays the full vote line content, using the specified marker
    /// and task instead of the intrinsic ones.
    /// </summary>
    /// <param name="marker">The optional string to use in place of the marker.</param>
    /// <param name="task">The optional string to use in place of the task.</param>
    /// <returns>Returns a string representing the current object.</returns>
    public static string ToOverrideString(VoteLineType voteLine, string? marker = null, string? task = null)
    {
        marker ??= voteLine.Marker.Display();
        task ??= voteLine.Task.Name;
        task = task.Length > 0 ? $"[{task}]" : "";
        return $"{voteLine.Prefix.Indent}[{marker}]{task} {voteLine.Content.Content}";
    }

    /// <summary>
    /// Formats a vote line for output, with optional override marker and task.
    /// </summary>
    /// <param name="marker">The optional string to use in place of the marker.</param>
    /// <param name="task">The optional string to use in place of the task.</param>
    /// Will use the default task if left null.</param>
    /// <returns>Returns a string representing the current vote line.</returns>
    public static string ToOutputString(VoteLineType voteLine, string? marker = null, string? task = null)
    {
        marker ??= voteLine.Marker.Display();
        task ??= voteLine.Task.Name;
        task = task.Length > 0 ? $"[{task}]" : "";
        string content = FormatBBCodeForOutput(voteLine.Content.Content);
        return $"{voteLine.Prefix.Indent}[{marker}]{task} {content}";
    }

    /// <summary>
    /// Function to convert placeholder symbols in a vote line to BBCode brackets.
    /// </summary>
    /// <param name="input">An input string.</param>
    /// <returns>The string with any 『』 brackets converted to [].</returns>
    private static string FormatBBCodeForOutput(string input)
    {
        string output = input.Replace('『', '[');
        output = output.Replace('』', ']');

        return output;
    }
}

/// <summary>
/// Comparer class for <see cref="VoteLineType"/> objects.
/// </summary>
public class VoteLineComparer : IEqualityComparer<VoteLineType>, IComparer<VoteLineType>
{
    public static VoteLineComparer Instance { get; } = new();

    public int Compare(VoteLineType? x, VoteLineType? y)
    {
        if (ReferenceEquals(x, y)) return 0;
        if (x is null) return -1;
        if (y is null) return 1;

        int compare = VoteTaskComparer.Instance.Compare(x.Task, y.Task);

        if (compare != 0) return compare;

        return VoteContentComparer.Instance.Compare(x.Content, y.Content);
    }

    public bool Equals(VoteLineType? x, VoteLineType? y)
    {
        if (x is null || y is null) return false;
        if (ReferenceEquals(x, y)) return true;

        return Compare(x, y) == 0;
    }

    public int GetHashCode([DisallowNull] VoteLineType obj)
    {
        return VoteContentComparer.Instance.GetHashCode(obj.Content);
    }
}
