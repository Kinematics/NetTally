using HtmlAgilityPack;

namespace NetTally.Votes
{
    /// <summary>
    /// Class to contain the author and title of a forum thread.
    /// </summary>
    public class ThreadInfo
    {
        public string Title { get; }
        public string Author { get; }

        public ThreadInfo(HtmlDocument page, IQuest quest)
        {
            IForumAdapter forumAdapter = quest.GetForumAdapter();

            Title = forumAdapter.GetPageTitle(page);
            Author = forumAdapter.GetAuthorOfThread(page);
        }
    }
}
