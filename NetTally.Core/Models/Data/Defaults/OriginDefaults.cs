namespace NetTally.Models;

public static class OriginDefaults
{
    extension(Origin)
    {
        public static Origin None => _noOrigin;
    }

    private static readonly Origin _noOrigin = new NoOrigin();
}
