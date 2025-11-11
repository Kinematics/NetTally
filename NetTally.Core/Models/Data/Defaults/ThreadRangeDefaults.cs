namespace NetTally.Models;

public static class ThreadRangeDefaults
{
    extension(ThreadRange)
    {
        public static ThreadRange None => _none;
    }

    private static readonly ThreadRange _none =
        new ThreadRangeByPostRange(PostNumber.None, PostNumber.None, 20, 1);
}

