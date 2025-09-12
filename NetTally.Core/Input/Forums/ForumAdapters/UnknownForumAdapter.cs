using HtmlAgilityPack;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NetTally.Configure;
using NetTally.Enums;
using NetTally.Tally.Posts.Component;
using NetTally.Tally.Threads;
using NetTally.Web;

namespace NetTally.Input.Forums.ForumAdapters;

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
    public string GetUrlForPage(Quest quest, int page) => "";

    public Task<ThreadInfo> GetThreadInfoAsync(
        Quest _0, IPageProvider _1, CancellationToken _2)
    {
        return Task.FromResult(ThreadInfos.None);
    }
}
