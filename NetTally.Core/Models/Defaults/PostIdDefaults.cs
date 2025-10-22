namespace NetTally.Models;

public static class PostIdDefaults
{
    extension(PostId)
    {
        public static PostId None => _none;
    }

    public static readonly PostId _none = new(0);
}
