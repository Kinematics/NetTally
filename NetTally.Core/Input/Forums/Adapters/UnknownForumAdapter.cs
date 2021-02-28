using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using HtmlAgilityPack;
using NetTally.Web;
using NetTally.Types.Enums;

namespace NetTally.Forums.Adapters
{
    /// <summary>
    /// A dummy forum adapter if the forum type is unknown.
    /// </summary>
    public class UnknownForumAdapter : IForumAdapter1
    {
        private static readonly Uri exampleUri = new Uri("http://www.example.com/");

        public Uri Site { get { return exampleUri; } set { } }

        public int DefaultPostsPerPage => 25;

        public BoolEx HasRSSThreadmarks => BoolEx.False;

        public string LineBreak => "";

        public string GetPermalinkForId(string postId) => "";
        public IEnumerable<Post> GetPosts(HtmlDocument page, IQuest quest) => Enumerable.Empty<Post>();
        public Task<ThreadRangeInfo> GetStartingPostNumberAsync(IQuest quest, IPageProvider pageProvider, CancellationToken token) =>
            Task.FromResult(new ThreadRangeInfo(false, 0));
        public ThreadInfo GetThreadInfo(HtmlDocument page) => new ThreadInfo("Unknown", "Unknown", 1);
        public string GetUrlForPage(int page, int postsPerPage) => "";
        public long GetValueOfPostID(string postID) => 0;
    }
}
