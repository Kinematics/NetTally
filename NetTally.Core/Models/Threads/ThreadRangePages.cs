namespace NetTally.Models.Threads;

/// <summary>
/// Extension class to get the start and end pages from a <see cref="ThreadRange"/>.
/// </summary>
internal static class ThreadRangePages
{
    extension(ThreadRange threadRange)
    {
        public int StartPage => threadRange.Map(
            idRange => idRange.StartPage,
            postRange => GetPageNumberOfPost(postRange.StartPostNumber, postRange.PostsPerPage));

        public int EndPage => threadRange.Map(
            idRange => idRange.PagesInThread,
            postRange => postRange.EndPostNumber == 0
                ? postRange.PagesInThread
                : Math.Min(GetPageNumberOfPost(postRange.EndPostNumber, postRange.PostsPerPage), postRange.PagesInThread));
    }

    private static int GetPageNumberOfPost(int postNumber, int postsPerPage) =>
        (postNumber - 1) / postsPerPage + 1;

}

/// <summary>
/// Extension class to handle mapping of different behavior to each
/// type of <see cref="ThreadRange"/>.
/// </summary>
internal static class ThreadRangeMapping
{
    extension(ThreadRange threadRange)
    {
        public T Map<T>(
            Func<ThreadRangeById, T> idMap,
            Func<ThreadRangeByPosts, T> postMap)
        {
            return threadRange switch
            {
                ThreadRangeById idRange => idMap(idRange),
                ThreadRangeByPosts postRange => postMap(postRange),
                _ => throw new InvalidOperationException("Unknown Thread Range type.")
            };
        }
    }
}
