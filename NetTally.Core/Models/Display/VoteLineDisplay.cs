using NetTally.Models.Votes;

namespace NetTally.Models.Display;

public static class VoteLineDisplay
{
    extension(VoteLine voteLine)
    {
        public bool HasTask => voteLine.Task.Name.Length > 0;
    }

    /// <summary>
    /// Formats the current object as a string.
    /// </summary>
    /// <returns>Returns a string representing the current object.</returns>
    public static string ToString(VoteLine voteLine)
    {
        string task = voteLine.HasTask ? $"[{voteLine.Task.Name}]" : "";
        return $"{voteLine.Prefix.Indent}[{voteLine.Marker.Display()}]{task} {voteLine.Content.Content}";
    }

    /// <summary>
    /// Creates a string that displays the cleaned content, and without any particular marker.
    /// May display a provided task instead of the innate one.
    /// </summary>
    /// <returns>Returns a string representing the current object.</returns>
    public static string ToComparableString(VoteLine voteLine, string? task = null)
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
    public static string ToOverrideString(VoteLine voteLine, string? marker = null, string? task = null)
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
    public static string ToOutputString(VoteLine voteLine, string? marker = null, string? task = null)
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
