using System.Text.RegularExpressions;

namespace NetTally.Tally.Vote.Component.Creation;

/// <summary>
/// Extension class for <see cref="Marker"/> creation.
/// </summary>
public static partial class MarkerCreation
{
    extension(Marker)
    {
        /// <summary>
        /// Create a new <see cref="Marker"/> based on the provided text.
        /// Invalid values will return a <see cref="NoMarker"/> type.
        /// </summary>
        /// <param name="markerText">The text that defines the <see cref="Marker"/>.</param>
        /// <returns>A new <see cref="Marker"/></returns>
        /// <exception cref="InvalidOperationException"></exception>
        public static Marker Create(string? markerText)
        {
            if (string.IsNullOrWhiteSpace(markerText))
                return Marker.Empty;

            markerText = markerText.Trim();

            Match m = MarkerRegex.Match(markerText);

            if (m.Success)
            {
                // Can't have rank and score valid at the same time
                if (m.Groups["rank"].Success && m.Groups["score"].Success)
                    return Marker.Empty;

                return true switch
                {
                    _ when m.Groups["vote"].Success => new VoteMarker(),
                    _ when m.Groups["approval"].Success => new ApprovalMarker(
                        m.Groups["approval"].Value == "+"),
                    _ when m.Groups["rank"].Success => new RankMarker(
                        Math.Clamp(int.Parse(m.Groups["value"].Value, System.Globalization.CultureInfo.CurrentCulture), 1, 99)),
                    _ when m.Groups["score"].Success => new ScoreMarker(
                        Math.Clamp(int.Parse(m.Groups["value"].Value, System.Globalization.CultureInfo.CurrentCulture), 0, 100)),
                    _ when m.Groups["value"].Success => new RankMarker(
                        Math.Clamp(int.Parse(m.Groups["value"].Value, System.Globalization.CultureInfo.CurrentCulture), 1, 99)),
                    _ => throw new InvalidOperationException($"Marker regex succeeded, but no valid regex type found. Text: {markerText}")
                };
            }

            return Marker.Empty;
        }
    }

    [GeneratedRegex(@"^(?<marker>(?<vote>[xX✓✔✗✘Х☒☑])|(?<rank>#)?(?<value>[0-9]{1,3})(?<score>%)?|(?<approval>[-+]))$")]
    private static partial Regex MarkerRegex { get; }
}
