using System;
using System.Collections.Generic;
using HtmlAgilityPack;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NetTally.Configure;
using NetTally.Enums;
using NetTally.Tally.ComponentsF.Posts;
using NetTally.Tally.ComponentsF.Threads;

namespace NetTally.Input.Forums.ForumAdaptersF
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
        public IEnumerable<PostType> GetPosts(HtmlDocument page, Quest quest, int pageNumber) => [];
        public ThreadRangeType GetQuestRangeInfo(Quest quest, HtmlDocument page) => ThreadRange.Empty;
        public ThreadInfoType GetThreadInfo(HtmlDocument page) => ThreadInfo.None;
        public string GetUrlForPage(Quest quest, int page) => "";
    }
}
