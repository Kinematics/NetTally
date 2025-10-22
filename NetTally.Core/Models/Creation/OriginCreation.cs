using NetTally.Models.Defaults;
using NetTally.Models.Posts;

namespace NetTally.Models.Creation;

/// <summary>
/// Class for creating <see cref="Origin"/> objects.
/// </summary>
public static class OriginCreation
{
    extension(Origin)
    {
        public static Origin? CreateUser(Author? author, Uri thread, Uri permalink, PostId? postId, PostNumber? postNumber) =>
            CreateUser(author, thread, permalink, postId, postNumber, DateTimeOffset.MinValue);

        public static Origin? CreateUser(
            Author? author,
            Uri thread,
            Uri permalink,
            PostId? postId,
            PostNumber? postNumber,
            DateTimeOffset timestamp)
        {
            if (author is null or NoAuthor ||
                postId is null ||
                postNumber is null)
                return null;

            return new UserOrigin(author, thread, permalink, postId, postNumber, timestamp);
        }

        public static Origin? CreatePlan(Origin origin, Author? planName)
        {
            if (planName is null or NoAuthor)
                return null;

            return new PlanOrigin(origin, planName);
        }

        public static Origin? CreateUserNameOnly(Author? author)
        {
            if (author is null or NoAuthor)
                return null;

            return Origin.None with { Author = author };
        }

        public static Origin? CreatePlanNameOnly(Author? planName)
        {
            if (planName is null or NoAuthor)
                return null;

            return new PlanOrigin(Origin.None, planName);
        }
    }
}


