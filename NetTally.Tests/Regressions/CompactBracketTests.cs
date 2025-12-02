using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using HtmlAgilityPack;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NetTally.Enums;
using NetTally.Input.Forums.ForumAdapters;
using NetTally.Models;

namespace NetTally.Tests.Regressions;

[TestClass]
public class CompactBracketTests
{
    static IServiceProvider serviceProvider = null!;

    [ClassInitialize]
    public static void ClassInit(TestContext _)
    {
        serviceProvider = TestStartup.ConfigureServices();
    }

    private static async Task<string> GetSamplePage()
    {
        string filename = Path.Combine("Resources", "TheYeerk.htm");

        return await LoadResource.Read(filename) ?? "";
    }

    private static async Task<HtmlDocument> GetSampleDocument()
    {
        HtmlDocument doc = new();
        string rawPage = await GetSamplePage();
        doc.LoadHtml(rawPage);

        return doc;
    }

    private static Quest GetSampleQuest()
    {
        var quest = TestStartup.GetExampleQuest(serviceProvider);
        quest.ThreadUri = new Uri("https://www.example.com/forum/the-yeerk.12345/");
        //quest.ThreadName = "The Yeerk";
        quest.DisplayName = "The Yeerk";
        quest.ForumType = ForumType.XenForo2;
        quest.StartPost = 156;
        quest.EndPost = 158;
        quest.CheckForLastThreadmark = false;

        return quest;
    }

    private static IForumAdapter GetForumAdapter(Quest quest)
    {
        var forumAdapterFactory = serviceProvider.GetService<ForumAdapterFactory>() ??
            throw new InvalidOperationException("Failed to get ForumAdapterFactory from service provider.");

        IForumAdapter adapter = forumAdapterFactory.CreateForumAdapter(ForumType.XenForo2, quest.ThreadUri);

        return adapter;
    }

    public static async Task<List<Post>> GetTestPosts()
    {
        var quest = GetSampleQuest();
        HtmlDocument page = await GetSampleDocument();
        var adapter = GetForumAdapter(quest);
        Assert.IsNotNull(adapter);
        quest.UseForumAdapter(adapter);
        var startPost = PostNumber.Create(quest.StartPost);
        var endPost = PostNumber.Create(quest.EndPost);
        var threadRange = ThreadRange.CreateByRange(startPost, endPost, 25, 6);
        Assert.IsNotNull(threadRange);

        var posts = adapter.GetPosts(page, quest, 6)
            .Where(p => !p.IsBeforeStart(threadRange) && !p.IsAfterEnd(threadRange))
            .ToList();

        return posts;
    }

    [TestMethod]
    public async Task CheckOnPosts()
    {
        var posts = await GetTestPosts();

        Assert.HasCount(3, posts);
    }
}
