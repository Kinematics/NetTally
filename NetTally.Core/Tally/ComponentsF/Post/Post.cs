namespace NetTally.Tally.ComponentsF.Post;
public record PostType(OriginType Origin, string Text);

public static class Post
{
    public static PostType? Create(OriginType origin, string text)
    {
        if (string.IsNullOrEmpty(text))
            return null;

        return new PostType(origin, text);
    }
}
