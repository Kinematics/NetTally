namespace NetTally.Models;

/// <summary>
/// Class for creating <see cref="Origin"/> objects.
/// </summary>
public static class OriginCreation
{
    extension(Origin)
    {
        public static Origin? CreateUser(Author? username, OriginDetail? originDetail)
        {
            if (username is null or NoAuthor ||
                originDetail is null or NoOriginDetail)
                return null;

            return new UserOrigin(username, originDetail);
        }

        public static Origin? CreatePlan(Author? planname, Author? author, OriginDetail? originDetail)
        {
            if (planname is null or NoAuthor ||
                author is null or NoAuthor ||
                originDetail is null or NoOriginDetail)
                return null;

            return new PlanOrigin(planname, author, originDetail);
        }

        /// <summary>
        /// Name-only version of a user origin.
        /// </summary>
        /// <param name="username">The user Author.</param>
        /// <returns>An origin based on a user that can be compared to other full origins.</returns>
        public static Origin? CreateUser(Author? username)
        {
            if (username is null or NoAuthor)
                return null;

            return new UserOrigin(username, OriginDetail.None);
        }

        /// <summary>
        /// Name-only version of a plan origin.
        /// </summary>
        /// <param name="planname">The plan Author.</param>
        /// <returns>An origin based on a plan that can be compared to other full origins.</returns>
        public static Origin? CreatePlan(Author? planname)
        {
            if (planname is null or NoAuthor)
                return null;

            return new PlanOrigin(planname, Author.None, OriginDetail.None);
        }
    }
}

