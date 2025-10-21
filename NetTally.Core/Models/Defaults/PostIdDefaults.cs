using NetTally.Models.Posts;

namespace NetTally.Models.Defaults;

public static class PostIdDefaults
{
    extension(PostId)
    {
        public static PostId Zero => _zero;
    }

    public static readonly PostId _zero = new(0);
}
