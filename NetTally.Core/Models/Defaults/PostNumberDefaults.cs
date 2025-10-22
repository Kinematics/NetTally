using NetTally.Models.Posts;

namespace NetTally.Models.Defaults;

public static class PostNumberDefaults
{
    extension(PostNumber)
    {
        public static PostNumber None => _none;
    }

    public static readonly PostNumber _none = new(0);
}
