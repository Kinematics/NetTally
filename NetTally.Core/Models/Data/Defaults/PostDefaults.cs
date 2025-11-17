namespace NetTally.Models;

public static class PostDefaults
{
    extension(Post)
    {
        public static Post None => _none;
    }

    private static readonly Post _none = new(Origin.None, "");
}
