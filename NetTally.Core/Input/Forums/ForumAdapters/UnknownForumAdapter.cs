using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using HtmlAgilityPack;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NetTally.Configure;
using NetTally.Enums;
using NetTally.Tally.Components;
using NetTally.Web;

namespace NetTally.Input.Forums.ForumAdapters
{
    class UnknownForumAdapter(
        IOptions<GlobalSettings> options,
        ILogger<UnknownForumAdapter> logger) : IForumAdapter
    {
        #region Constructor
        readonly GlobalSettings inputOptions = options.Value;
        readonly ILogger<UnknownForumAdapter> logger = logger;
        #endregion

        public string GetDefaultLineBreak(Uri uri) => "";
        public int GetDefaultPostsPerPage(Uri uri) => 25;
        public BoolEx HasRssThreadmarksFeed(Uri uri) => BoolEx.False;
        public IEnumerable<Post> GetPosts(HtmlDocument page, Quest quest, int pageNumber) => [];
        public Task<ThreadRangeInfo> GetQuestRangeInfoAsync(Quest quest, IPageProvider pageProvider, CancellationToken token)
            => Task.FromResult(new ThreadRangeInfo(false, 0));
        public ThreadInfo GetThreadInfo(HtmlDocument page) => new("Unknown", "Unknown", 1);
        public string GetUrlForPage(Quest quest, int page) => "";
    }
}
