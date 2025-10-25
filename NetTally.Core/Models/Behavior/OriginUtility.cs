using NetTally.Configure;
using NetTally.Models.Mapping;
using NetTally.Utility.Strings;

namespace NetTally.Models;

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
            noOrigin => false,
            userOrigin => true,
            planOrigin => false);

        /// <summary>
        /// Determine if the <see cref="Origin"/> object is a plan type.
        /// </summary>
        public bool IsPlan => origin.Map(
            noOrigin => false,
            userOrigin => false,
            planOrigin => true);

        /// <summary>
        /// Get the appropriate <see cref="Author"/> based on the type of <see cref="Origin"/>.
        /// <see cref="UserOrigin"/> returns the Author. <see cref="PlanOrigin"/> returns the PlanName.
        /// </summary>
        /// <param name="origin"></param>
        /// <returns>The <see cref="Author"/> of the <see cref="Origin"/>.</returns>
        public Author GetName() => origin.Map(
            noOrigin => Author.None,
            userOrigin => userOrigin.UserName,
            planOrigin => planOrigin.PlanName);

        /// <summary>
        /// Gets the author of the <see cref="Origin"/>. If the origin is a plan,
        /// this is the original user Author.
        /// </summary>
        public Author Author => origin.Map(
            noOrigin => Author.None,
            userOrigin => userOrigin.UserName,
            planOrigin => planOrigin.Author);

        /// <summary>
        /// Gets the original <see cref="Origin"/> used as a basis for this one.
        /// Only applies to <see cref="PlanOrigin"/> objects. Otherwise returns <see cref="Origins.None"/>.
        /// </summary>
        /// <param name="origin">The Origin of the <see cref="Origin"/>, if any.</param>
        /// <returns></returns>
        public OriginDetail GetDetails() => origin.Map(
            noOrigin => OriginDetail.None,
            userOrigin => userOrigin.Detail,
            planOrigin => planOrigin.Detail);

        public Uri? Thread => origin.Map(
            noOrigin => null,
            userOrigin => userOrigin.Detail.GetThread(),
            planOrigin => planOrigin.Detail.GetThread());

        public Uri? Permalink => origin.Map(
            noOrigin => null,
            userOrigin => userOrigin.Detail.GetPermalink(),
            planOrigin => planOrigin.Detail.GetPermalink());

        public PostId PostId => origin.Map(
            noOrigin => PostId.None,
            userOrigin => userOrigin.Detail.GetPostId(),
            planOrigin => planOrigin.Detail.GetPostId());

        public PostNumber PostNumber => origin.Map(
            noOrigin => PostNumber.None,
            userOrigin => userOrigin.Detail.GetPostNumber(),
            planOrigin => planOrigin.Detail.GetPostNumber());

        /// <summary>
        /// Gets a formatted BBCode string containing the URL for the <see cref="Origin"/>'s author.
        /// </summary>
        /// <param name="origin"></param>
        /// <returns>A formatted BBCode string containing the URL for the <see cref="Origin"/>'s author.</returns>
        public string GetBBCodeLink() => origin.GetDetails() switch
        {
            OriginSource source => urlTemplate.FormatWith(source.Permalink, origin.GetBBCodeAuthorFormat()),
            _ => string.Empty
        };

        private string GetBBCodeAuthorFormat() => origin.Map(
            noOrigin => string.Empty,
            userOrigin => userOrigin.UserName.DisplayName,
            planOrigin => $"{Strings.PlanNameMarker}{planOrigin.PlanName.DisplayName}");
    }

    static readonly string urlTemplate = "[url=\"{0}\"]{1}[/url]";
}


