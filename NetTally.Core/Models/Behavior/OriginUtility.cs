using NetTally.Configure;
using NetTally.Models.Defaults;
using NetTally.Models.Mapping;
using NetTally.Models.Posts;

namespace NetTally.Models.Behavior;

/// <summary>
/// Extension methods for <see cref="Origin"/> objects.
/// </summary>
public static class OriginUtility
{
    extension(Origin origin)
    {
        /// <summary>
        /// Determine if the <see cref="Origin"/> object is a user type.
        /// </summary>
        public bool IsUser => origin.Map(
            userOrigin => true,
            planOrigin => false);

        /// <summary>
        /// Determine if the <see cref="Origin"/> object is a plan type.
        /// </summary>
        public bool IsPlan => origin.Map(
            userOrigin => false,
            planOrigin => true);

        /// <summary>
        /// Get the appropriate <see cref="Author"/> based on the type of <see cref="Origin"/>.
        /// <see cref="UserOrigin"/> returns the Author. <see cref="PlanOrigin"/> returns the PlanName.
        /// </summary>
        /// <param name="origin"></param>
        /// <returns>The <see cref="Author"/> of the <see cref="Origin"/>.</returns>
        public Author GetName() => origin.Map<Author>(
            userOrigin => userOrigin.Author,
            planOrigin => planOrigin.PlanName);

        /// <summary>
        /// Gets the original <see cref="Origin"/> used as a basis for this one.
        /// Only applies to <see cref="PlanOrigin"/> objects. Otherwise returns <see cref="Origins.None"/>.
        /// </summary>
        /// <param name="origin">The Origin of the <see cref="Origin"/>, if any.</param>
        /// <returns></returns>
        public Origin Source() => origin.Map<Origin>(
            userOrigin => Origin.None,
            planOrigin => planOrigin.Origin);

        /// <summary>
        /// Gets a formatted BBCode string containing the URL for the <see cref="Origin"/>'s author.
        /// </summary>
        /// <param name="origin"></param>
        /// <returns>A formatted BBCode string containing the URL for the <see cref="Origin"/>'s author.</returns>
        public string GetBBCodeLink() => origin.Map(
            userOrigin => $"[url=\"{userOrigin.Permalink}\"]{userOrigin.Author.DisplayName}[/url]",
            planOrigin => $"[url=\"{planOrigin.Permalink}\"]{Strings.PlanNameMarker}{planOrigin.PlanName.DisplayName}[/url]");
    }
}


