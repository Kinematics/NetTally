using NetTally.Models.Posts;

namespace NetTally.Models.Defaults;

public static class PostNumberDefaults
{
    extension(PostNumber)
    {
        public static PostNumber Zero => _zero;
    }

    public static readonly PostNumber _zero = new(0);
}
