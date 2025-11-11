using NetTally.Configure;
using NetTally.Models.Mapping;

namespace NetTally.Models;

public static class OriginDisplay
{
    extension(Origin origin)
    {
        /// <summary>
        /// Gets a formatted BBCode string containing the URL for the <see cref="Origin"/>'s author.
        /// </summary>
        /// <param name="origin"></param>
        /// <returns>A formatted BBCode string containing the URL for the <see cref="Origin"/>'s author.</returns>
        public string GetBBCodeLink() => origin.GetSource().GetBBCodeLink(origin.GetBBCodeAuthorFormat());

        private string GetBBCodeAuthorFormat() => origin.Map(
            noOrigin => string.Empty,
            userOrigin => userOrigin.UserName.DisplayName,
            planOrigin => $"{Strings.PlanNameMarker}{planOrigin.PlanName.DisplayName}");

    }
}
