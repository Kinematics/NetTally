using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using HtmlAgilityPack;
using Microsoft.Extensions.Logging;
using NetTally.Options;
using NetTally.Web;
using NetTally.Types.Enums;
using NetTally.Types.Components;

namespace NetTally.Forums.ForumAdapters
{
    class UnknownForumAdapter : IForumAdapter
    {
        #region Constructor
        readonly IGeneralInputOptions inputOptions;
        readonly ILogger<UnknownForumAdapter> logger;

        public UnknownForumAdapter(IGeneralInputOptions inputOptions, ILogger<UnknownForumAdapter> logger)
        {
            this.inputOptions = inputOptions;
            this.logger = logger;
        }
        #endregion

        public string GetDefaultLineBreak(Uri uri) => "";
        public int GetDefaultPostsPerPage(Uri uri) => 25;
        public BoolEx GetHasRssThreadmarksFeed(Uri uri) => BoolEx.False;
        public IEnumerable<Post> GetPosts(HtmlDocument page, Quest quest, int pageNumber) => Enumerable.Empty<Post>();
        public Task<ThreadRangeInfo> GetQuestRangeInfoAsync(Quest quest, IPageProvider pageProvider, CancellationToken token) 
            => Task.FromResult(new ThreadRangeInfo(false, 0));
        public ThreadInfo GetThreadInfo(HtmlDocument page) => new ThreadInfo("Unknown", "Unknown", 1);
        public string GetUrlForPage(Quest quest, int page) => "";
    }
}
