using NetTally.Models.Mapping;

namespace NetTally.Models;

/// <summary>
/// Extension methods for <see cref="Origin"/> objects.
/// </summary>
public static class OriginProperties
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
        public Author Name => origin.Map(
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
        public Source Source => origin.Map(
            noOrigin => Source.None,
            userOrigin => userOrigin.Source,
            planOrigin => planOrigin.Source);

        public Uri? Thread => origin.Map(
            noOrigin => null,
            userOrigin => userOrigin.Source.Thread,
            planOrigin => planOrigin.Source.Thread);

        public Uri? Permalink => origin.Map(
            noOrigin => null,
            userOrigin => userOrigin.Source.Permalink,
            planOrigin => planOrigin.Source.Permalink);

        public PostId PostId => origin.Map(
            noOrigin => PostId.None,
            userOrigin => userOrigin.Source.PostId,
            planOrigin => planOrigin.Source.PostId);

        public PostNumber PostNumber => origin.Map(
            noOrigin => PostNumber.None,
            userOrigin => userOrigin.Source.PostNumber,
            planOrigin => planOrigin.Source.PostNumber);

    }
}


