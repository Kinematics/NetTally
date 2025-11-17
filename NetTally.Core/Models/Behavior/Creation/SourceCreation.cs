namespace NetTally.Models;

public static class SourceCreation
{
    extension(Source)
    {
        public static Source? Create(
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

            return new SourceLocation(thread, permalink, postId, postNumber);
        }
    }
}

