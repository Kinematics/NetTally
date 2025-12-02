using NetTally.Configure;

namespace NetTally.Models;

public static class VoteLineDisplay
{
    extension(VoteLine voteLine)
    {
        /// <summary>
        /// Gets the raw vote line formatted as a string.
        /// </summary>
        /// <param name="markerOverride">An optional override of the marker value.</param>
        /// <param name="taskOverride">An optional override of the task value.</param>
        public string Display(string? markerOverride = null, string? taskOverride = null) =>
            $"{voteLine.Prefix.Indent}{markerOverride.Bracket ?? voteLine.Marker.BracketedDisplay}{taskOverride.Bracket ?? voteLine.Task.BracketedDisplay} {voteLine.Content.Content.FormatBBCode}";

        /// <summary>
        /// Get the vote line formatted as a string suitable for comparing.
        /// Uses the clean content, and no marker.
        /// </summary>
        /// <param name="taskOverride">An optional override of the task value.</param>
        public string DisplayComparable(string? taskOverride = null) =>
            $"{voteLine.Prefix.Indent}[]{taskOverride.Bracket ?? voteLine.Task.BracketedDisplay} {voteLine.Content.CleanContent}";

        /// <summary>
        /// Gets the vote line formatted as a string, with BBCode punctuation properly substituted in.
        /// </summary>
        /// <param name="markerOverride">An optional override of the marker value.</param>
        /// <param name="taskOverride">An optional override of the task value.</param>
        public string DisplayOutput(string? markerOverride = null, string? taskOverride = null) =>
            $"{voteLine.Prefix.Indent}{markerOverride.Bracket ?? voteLine.Marker.BracketedDisplay}{taskOverride.Bracket ?? voteLine.Task.BracketedDisplay} {voteLine.Content.Content.FormatBBCode}";
    }

    extension(string? str)
    {
        /// <summary>
        /// Put the provided string in brackets.
        /// </summary>
        string? Bracket => str is not null && str.Length > 0 ? $"[{str}]" : null;

        /// <summary>
        /// Convert custom BBCode punctuation back to normal output form.
        /// </summary>
        string? FormatBBCode => str?.Replace(Strings.OpenBBCode, '[').Replace(Strings.CloseBBCode, ']');
    }
}
