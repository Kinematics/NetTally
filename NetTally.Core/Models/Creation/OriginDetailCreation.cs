namespace NetTally.Models;

public static class OriginDetailCreation
{
    extension(OriginDetail)
    {
        public static OriginDetail? Create(
            Uri? thread,
            Uri? permalink,
            PostId? postId,
            PostNumber? postNumber)
        {
            if (thread is null ||
                permalink is null ||
                postId is null ||
                postNumber is null)
                return null;

            return new OriginSource(thread, permalink, postId, postNumber);
        }
    }
}

