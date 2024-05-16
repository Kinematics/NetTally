using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using HtmlAgilityPack;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NetTally.Configure;
using NetTally.Enums;
using NetTally.Tally.ComponentsF.Posts;
using NetTally.Tally.ComponentsF.Threads;
using NetTally.Web;

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
        public string GetUrlForPage(Quest quest, int page) => "";

        public Task<ThreadInformationType?> GetThreadInformationAsync(
            Quest _0, IPageProvider _1, CancellationToken _2)
        {
            ThreadInformationType? value = ThreadInformation.None;
            return Task.FromResult(value)!;
        }
    }
}
