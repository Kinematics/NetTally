using NetTally.Models.Behavior;
using NetTally.Models.Mapping;
using NetTally.Models.Threads;

namespace NetTally.Models.Behavior;

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
