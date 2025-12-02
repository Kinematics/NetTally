using System;
using System.Threading;
using System.Threading.Tasks;
using HtmlAgilityPack;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NetTally.Input.Forums;
using NetTally.Input.Forums.ForumAdapters;
using NetTally.Models;

namespace NetTally.Tests.Forums;

[TestClass]
[Ignore]
public class ForumAdapterFactoryTests
{
    static ForumAdapterFactory forumAdapterFactory = null!;

    [ClassInitialize]
    public static void ClassInit(TestContext _)
    {
        IServiceProvider serviceProvider = TestStartup.ConfigureServices();
        forumAdapterFactory = serviceProvider.GetRequiredService<ForumAdapterFactory>();
    }


    [TestMethod]
    [Ignore]
    public async Task Select_XenForo_SV()
    {
        Quest quest = new() { ThreadName = "http://forums.sufficientvelocity.com/threads/vote-tally-program.199/" };
        var adapter = await forumAdapterFactory.CreateForumAdapterAsync(quest, CancellationToken.None);

        Assert.IsInstanceOfType<XenForo2Adapter>(adapter);
    }

    [TestMethod]
    [Ignore]
    public async Task Select_XenForo_SB()
    {
        Quest quest = new() { ThreadName = "https://forums.spacebattles.com/threads/vote-tally-program-v3.260204/" };
        var adapter = await forumAdapterFactory.CreateForumAdapterAsync(quest, CancellationToken.None);

        Assert.IsInstanceOfType<XenForo1Adapter>(adapter);
    }

    [TestMethod]
    [Ignore]
    public async Task Select_XenForo_QQ()
    {
        Quest quest = new() { ThreadName = "https://forum.questionablequesting.com/threads/qq-vote-tally-program.1065/" };
        var adapter = await forumAdapterFactory.CreateForumAdapterAsync(quest, CancellationToken.None);

        Assert.IsInstanceOfType<XenForo1Adapter>(adapter);
    }

    [TestMethod]
    [Ignore]
    public async Task Select_vBulletin3()
    {
        Quest quest = new() { ThreadName = "http://forums.animesuki.com/showthread.php?t=128882" };
        var adapter = await forumAdapterFactory.CreateForumAdapterAsync(quest, CancellationToken.None);

        Assert.IsInstanceOfType<VBulletin3Adapter>(adapter);
    }

    [TestMethod]
    [Ignore]
    public async Task Select_vBulletin4()
    {
        // Fandompost changed to vBulletin 5.  Need to find another vBulletin 4 for testing.
        Quest quest = new() { ThreadName = "http://www.fandompost.com/oldforums/showthread.php?48716-One-Punch-Man-Discussion-Thread/page1" };
        var adapter = await forumAdapterFactory.CreateForumAdapterAsync(quest, CancellationToken.None);

        Assert.IsInstanceOfType<VBulletin4Adapter>(adapter);
    }

    [TestMethod]
    [Ignore]
    public async Task Select_vBulletin5()
    {
        Quest quest = new() { ThreadName = "http://www.vbulletin.com/forum/forum/vbulletin-announcements/vbulletin-announcements_aa/4333101-vbulletin-5-1-10-connect-is-now-available" };
        var adapter = await forumAdapterFactory.CreateForumAdapterAsync(quest, CancellationToken.None);

        Assert.IsInstanceOfType<VBulletin5Adapter>(adapter);
    }

    // "https://community.nodebb.org/topic/6298/nodebb-v0-7-3"
    // Don't know how to handle NodeBB forums.

    [TestMethod]
    [Ignore]
    public async Task Select_phpBB()
    {
        Quest quest = new() { ThreadName = "http://www.ilovephilosophy.com/viewtopic.php?f=1&t=175054" };
        var adapter = await forumAdapterFactory.CreateForumAdapterAsync(quest, CancellationToken.None);

        Assert.IsInstanceOfType<PhpBBAdapter>(adapter);
    }

    [TestMethod]
    public async Task Select_Explicit()
    {
        Uri uri = new("https://example.com/threads/RenascenceSV.html.100/");
        var resourceContent = await LoadResource.Read("Resources/RenascenceSV.html");
        Assert.IsNotNull(resourceContent);
        HtmlDocument doc = new();
        doc.LoadHtml(resourceContent);
        var forumType = ForumIdentifier.IdentifyForumTypeFromHtmlDocument(doc);

        var adapter = forumAdapterFactory.CreateForumAdapter(forumType, uri);
        Assert.IsInstanceOfType<XenForo1Adapter>(adapter);
    }
}
