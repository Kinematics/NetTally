using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using HtmlAgilityPack;
using NetTally.Web;

namespace NetTally.Forums.Adapters2
{
    class UnknownForumAdapter2 : IForumAdapter2
    {
        public string GetDefaultLineBreak(Uri uri) => "";
        public int GetDefaultPostsPerPage(Uri uri) => 25;
        public BoolEx GetHasRssThreadmarksFeed(Uri uri) => BoolEx.False;
        public IEnumerable<Post> GetPosts(HtmlDocument page, IQuest quest, int pageNumber) => Enumerable.Empty<Post>();
        public Task<ThreadRangeInfo> GetQuestRangeInfoAsync(IQuest quest, IPageProvider pageProvider, CancellationToken token) 
            => Task.FromResult(new ThreadRangeInfo(false, 0));
        public ThreadInfo GetThreadInfo(HtmlDocument page) => new ThreadInfo("Unknown", "Unknown", 1);
        public string GetUrlForPage(IQuest quest, int page) => "";
    }
}
