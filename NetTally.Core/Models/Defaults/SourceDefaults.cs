namespace NetTally.Models;

public static class SourceDefaults
{
    extension(Source)
    {
        public static Source None => _noSource;
    }

    private static readonly Source _noSource = new NoSource();
}