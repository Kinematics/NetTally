using NetTally.Models.Mapping;

namespace NetTally.Models;

/// <summary>
/// Extension class to get the start and end pages from a <see cref="ThreadRange"/>.
/// </summary>
internal static class ThreadRangePages
{
    extension(ThreadRange threadRange)
    {
        public int StartPage => threadRange.Map(
            idRange => idRange.StartPage,
            fullPostRange => GetPageNumberOfPost(fullPostRange.StartPostNumber, fullPostRange.PostsPerPage, fullPostRange.PagesInThread),
            startPostRange => GetPageNumberOfPost(startPostRange.StartPostNumber, startPostRange.PostsPerPage, startPostRange.PagesInThread));

        public int EndPage => threadRange.Map(
            idRange => idRange.PagesInThread,
            fullPostRange => GetPageNumberOfPost(fullPostRange.EndPostNumber, fullPostRange.PostsPerPage, fullPostRange.PagesInThread),
            startPostRange => startPostRange.PagesInThread);
    }

    private static int GetPageNumberOfPost(PostNumber postNumber, int postsPerPage, int pagesInThread) =>
        Math.Clamp((int)(postNumber.Value - 1) / postsPerPage + 1, 1, pagesInThread);
}
