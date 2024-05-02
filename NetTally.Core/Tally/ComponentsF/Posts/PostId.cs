using System.Globalization;

namespace NetTally.Tally.ComponentsF.Posts;
public record PostIdType(long Id);

public static class PostId
{
    public static PostIdType Zero { get; } = new PostIdType(0);

    public static PostIdType Create(long id)
    {
        if (id < 1)
            return Zero;

        return new PostIdType(id);
    }

    public static PostIdType? Create(string id)
    {
        if (string.IsNullOrEmpty(id))
            return null;

        if (long.TryParse(id, NumberStyles.AllowThousands, CultureInfo.InvariantCulture, out long idValue))
        {
            return idValue switch
            {
                > 0 => new PostIdType(idValue),
                _ => Zero
            };
        }

        return null;
    }
}

