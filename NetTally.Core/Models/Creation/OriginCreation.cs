using NetTally.Models.Creation;
using NetTally.Models.Posts;

namespace NetTally.Models.Creation;

/// <summary>
/// Class for creating <see cref="Origin"/> objects.
/// </summary>
public static class OriginCreation
{
    extension(Origin)
    {
        public static Origin? CreateUser(Author author, Uri thread, Uri permalink, PostId postId, PostId postNumber) =>
            CreateUser(author, thread, permalink, postId, postNumber, DateTimeOffset.MinValue);

        public static Origin? CreateUser(
            Author author,
            Uri thread,
            Uri permalink,
            PostId postId,
            PostId postNumber,
            DateTimeOffset timestamp)
        {
            if (author == Author.None)
                return null;

            return new UserOrigin(author, thread, permalink, postId, postNumber, timestamp);
        }

        public static Origin CreatePlan(Origin origin, Author planName)
        {
            return new PlanOrigin(origin, planName);
        }

        public static Origin CreateUserNameOnly(Author author)
        {
            return Origin.None with { Author = author };
        }

        public static Origin CreatePlanNameOnly(Author planName)
        {
            return new PlanOrigin(Origin.None, planName);
        }
    }
}


