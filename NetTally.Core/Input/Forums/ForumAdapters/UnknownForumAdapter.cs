using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using HtmlAgilityPack;
using Microsoft.Extensions.Logging;
using NetTally.Options;
using NetTally.Tally.Components;
using NetTally.Types.Enums;
using NetTally.Web;

namespace NetTally.Forums.ForumAdapters
{
    class UnknownForumAdapter(
        IGeneralInputOptions inputOptions,
        ILogger<UnknownForumAdapter> logger) : IForumAdapter
    {
        #region Constructor
        readonly IGeneralInputOptions inputOptions = inputOptions;
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
