namespace NetTally.Tally.Vote.Components;
/// <summary>
/// Data type for vote lines.
/// </summary>
/// <param name="Prefix">The prefix on the vote line.</param>
/// <param name="Marker">The voting marker.</param>
/// <param name="Task">The task assigned to the vote line.</param>
/// <param name="Content">The contents of the vote line.</param>
public record VoteLine(Prefix Prefix, Marker Marker, VoteTask Task, VoteContent Content)
{
    public int Depth => Prefix.Depth;
    public bool HasTask => Task.Name.Length > 0;

    /// <summary>
    /// ToString implementation to make debugging easier, by formatting the
    /// data in an easy-to-read manner.
    /// </summary>
    public override string ToString()
    {
        return $"{{{Prefix.Indent}[{Marker.Display()}][{Task.Name}] {Content.CleanContent}}}";
    }
}

/// <summary>
/// Extension class for creating <see cref="VoteLine"/> objects.
/// </summary>
public static class VoteLineCreation
{
    extension(VoteLine)
    {
        /// <summary>
        /// The default empty <see cref="VoteLine"/>.
        /// </summary>
        public static VoteLine Empty => _empty;

        /// <summary>
        /// Create a new <see cref="VoteLine"/> using the provided components.
        /// Marker and Content must be non-empty.
        /// </summary>
        /// <param name="prefix">The prefix component of the vote line.</param>
        /// <param name="marker">The marker of the vote line.</param>
        /// <param name="task">The task of the vote line.</param>
        /// <param name="content">The content of the vote line.</param>
        /// <returns>A new <see cref="VoteLine"/></returns>
        public static VoteLine Create(
            Prefix prefix,
            Marker marker,
            VoteTask task,
            VoteContent content)
        {
            ArgumentNullException.ThrowIfNull(prefix);
            ArgumentNullException.ThrowIfNull(marker);
            ArgumentNullException.ThrowIfNull(task);
            ArgumentNullException.ThrowIfNull(content);

            if (content == VoteContent.Empty)
                return VoteLine.Empty;

            return new(prefix, marker, task, content);
        }
    }

    private static readonly VoteLine _empty = 
        new (Prefix.Empty, Marker.Empty, VoteTask.Empty, VoteContent.Empty);
}

/// <summary>
/// Extension class for promoting (reducing the depth of the prefix) <see cref="VoteLine"/>s
/// </summary>
public static class VoteLinePromotion
{
    extension(VoteLine voteLine)
    {
        /// <summary>
        /// Promote a <see cref="VoteLine"/> by a specified depth level.
        /// </summary>
        /// <param name="promoteDepth">The number of steps to promote the line by. Default is 1.</param>
        /// <returns>A new version of the <see cref="VoteLine"/> after being promoted,
        /// or the same instance of no change was made.</returns>
        public VoteLine Promote(int promoteDepth = 1)
        {
            if (promoteDepth == 0)
                return voteLine;

            return voteLine with { Prefix = voteLine.Prefix.Promote(promoteDepth) };
        }

        /// <summary>
        /// Maximally promote a vote line by reducing the prefix depth to 0.
        /// </summary>
        /// <returns>The fully promoted <see cref="VoteLine"/></returns>
        public VoteLine FullPromote()
        {
            return voteLine.Promote(voteLine.Depth);
        }
    }
}


public static class VoteLineDisplay
{
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
