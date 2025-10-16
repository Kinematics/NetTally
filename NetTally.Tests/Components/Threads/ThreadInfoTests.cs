using Microsoft.VisualStudio.TestTools.UnitTesting;
using NetTally.Configure;
using NetTally.Tally.Posts.Component;
using NetTally.Tally.Posts.Component.Creation;
using NetTally.Tally.Posts.Component.Utility;
using NetTally.Tally.Threads;

namespace NetTally.Tests.Components.Threads;
[TestClass]
public class ThreadInfoTests
{
    [ClassInitialize]
    public static void ClassInit(TestContext _)
    {
        TestStartup.ConfigureServices();
    }

    [TestMethod]
    public void Construct_ByPostId_Basic()
    {
        string title = "Jupiter Hop";
        string authorName = "Jack";
        var author = Author.Create(authorName);
        var postId = PostId.Create(123456);
        int pageNumberOfStartPost = 5;
        int pagesInThread = 14;

        var threadRange = ThreadRangeCreation.CreateByPostId(postId, pageNumberOfStartPost, pagesInThread);
        var threadInfo = ThreadInfo.Create(title, author, threadRange);

        Assert.IsNotNull(threadInfo);
        Assert.AreEqual(title, threadInfo.Title);
        Assert.AreEqual(authorName, threadInfo.Author.DisplayName);
        Assert.AreEqual(pageNumberOfStartPost, threadInfo.ThreadRange.StartPage);
        Assert.AreEqual(pagesInThread, threadInfo.ThreadRange.EndPage);
    }

    [TestMethod]
    public void Construct_ByPostId_NoPages()
    {
        string title = "Jupiter Hop";
        string authorName = "Jack";
        var author = Author.Create(authorName);
        var postId = PostId.Create(123456);
        int pageNumberOfStartPost = 5;
        int pagesInThread = 0;

        var threadRange = ThreadRangeCreation.CreateByPostId(postId, pageNumberOfStartPost, pagesInThread);
        var threadInfo = ThreadInfo.Create(title, author, threadRange);

        Assert.IsNotNull(threadInfo);
        Assert.AreEqual(title, threadInfo.Title);
        Assert.AreEqual(authorName, threadInfo.Author.DisplayName);
        Assert.AreEqual(5, threadInfo.ThreadRange.StartPage);
        Assert.AreEqual(1, threadInfo.ThreadRange.EndPage);
    }

    [TestMethod]
    public void Construct_ByPostNumber_Basic()
    {
        string title = "Jupiter Hop";
        string authorName = "Jack";
        var author = Author.Create(authorName);
        int startPost = 300;
        int postsPerPage = 30;
        int pagesInThread = 14;

        var threadRange = ThreadRangeCreation.CreateByStartOfRange(startPost, postsPerPage, pagesInThread);
        var threadInfo = ThreadInfo.Create(title, author, threadRange);

        Assert.IsNotNull(threadInfo);
        Assert.AreEqual(title, threadInfo.Title);
        Assert.AreEqual(authorName, threadInfo.Author.DisplayName);
        Assert.AreEqual(10, threadInfo.ThreadRange.StartPage);
        Assert.AreEqual(14, threadInfo.ThreadRange.EndPage);
    }

    [TestMethod]
    public void Construct_PostNumber_ZeroStart()
    {
        string title = "Jupiter Hop";
        string authorName = "Jack";
        var author = Author.Create(authorName);
        int startPost = 0;
        int postsPerPage = 30;
        int pagesInThread = 14;

        var threadRange = ThreadRangeCreation.CreateByStartOfRange(startPost, postsPerPage, pagesInThread);
        var threadInfo = ThreadInfo.Create(title, author, threadRange);

        Assert.IsNotNull(threadInfo);
        Assert.AreEqual(title, threadInfo.Title);
        Assert.AreEqual(authorName, threadInfo.Author.DisplayName);
        Assert.AreEqual(1, threadInfo.ThreadRange.StartPage);
        Assert.AreEqual(14, threadInfo.ThreadRange.EndPage);
    }

    [TestMethod]
    public void Construct_Untitled()
    {
        string title = "";
        string authorName = "Jack";
        var author = Author.Create(authorName);
        int startPost = 0;
        int postsPerPage = 30;
        int pagesInThread = 14;

        var threadRange = ThreadRangeCreation.CreateByStartOfRange(startPost, postsPerPage, pagesInThread);
        var threadInfo = ThreadInfo.Create(title, author, threadRange);

        Assert.IsNotNull(threadInfo);
        Assert.AreEqual(Strings.UntitledThread, threadInfo.Title);
        Assert.AreEqual(authorName, threadInfo.Author.DisplayName);
        Assert.AreEqual(1, threadInfo.ThreadRange.StartPage);
        Assert.AreEqual(14, threadInfo.ThreadRange.EndPage);
    }

    [TestMethod]
    public void Construct_NoAuthor()
    {
        string title = "";
        Author? author = null;
        int startPost = 0;
        int postsPerPage = 30;
        int pagesInThread = 14;

        var threadRange = ThreadRangeCreation.CreateByStartOfRange(startPost, postsPerPage, pagesInThread);
        var threadInfo = ThreadInfo.Create(title, author!, threadRange);

        Assert.IsNotNull(threadInfo);
        Assert.AreEqual(Strings.UntitledThread, threadInfo.Title);
        Assert.IsTrue(threadInfo.Author is UnknownAuthor);
        Assert.AreEqual(1, threadInfo.ThreadRange.StartPage);
        Assert.AreEqual(14, threadInfo.ThreadRange.EndPage);
    }

    private static (ThreadInfo ThreadInfo, Quest Quest) GetStandardInfoByPostNumber(
        int endPost)
    {
        Quest quest = new()
        {
            StartPost = 410,
            EndPost = 0,
            PostsPerPage = 25
        };

        string title = "Jupiter Hop";
        string authorName = "Jack";
        var author = Author.Create(authorName);
        int pagesInThread = 25;

        var threadRange = ThreadRangeCreation.CreateByRange(quest.StartPost, endPost, quest.PostsPerPage, pagesInThread);
        var threadInfo = ThreadInfo.Create(title, author, threadRange);

        return (threadInfo, quest);
    }

    private static (ThreadInfo ThreadInfo, Quest Quest) GetStandardInfoByPostId()
    {
        Quest quest = new()
        {
            StartPost = 410,
            EndPost = 0,
            PostsPerPage = 25
        };

        string title = "Jupiter Hop";
        string authorName = "Jack";
        var author = Author.Create(authorName);
        var postId = PostId.Create(123456);
        int pageNumberOfStartPost = 5;
        int pagesInThread = 25;

        var threadRange = ThreadRangeCreation.CreateByPostId(postId, pageNumberOfStartPost, pagesInThread);
        var threadInfo = ThreadInfo.Create(title, author, threadRange);

        return (threadInfo, quest);
    }

    [TestMethod]
    public void Calculate_StartPage_PostNumber()
    {
        var (threadInfo, quest) = GetStandardInfoByPostNumber(0);

        Assert.IsNotNull(threadInfo);
        Assert.IsNotNull(quest);

        Assert.AreEqual(17, threadInfo.ThreadRange.StartPage);
    }

    [TestMethod]
    public void Calculate_StartPage_PostId()
    {
        var (threadInfo, quest) = GetStandardInfoByPostId();

        Assert.IsNotNull(threadInfo);
        Assert.IsNotNull(quest);

        Assert.AreEqual(5, threadInfo.ThreadRange.StartPage);
    }

    [TestMethod]
    public void Calculate_EndPage_PostNumber_EndOfThread_PageLimit()
    {
        var (threadInfo, quest) = GetStandardInfoByPostNumber(0);

        Assert.IsNotNull(threadInfo);
        Assert.IsNotNull(quest);

        Assert.AreEqual(25, threadInfo.ThreadRange.EndPage);
    }

    [TestMethod]
    public void Calculate_EndPage_PostNumber_EndPost()
    {
        var (threadInfo, quest) = GetStandardInfoByPostNumber(510);

        Assert.IsNotNull(threadInfo);
        Assert.IsNotNull(quest);

        Assert.AreEqual(21, threadInfo.ThreadRange.EndPage);
    }

    [TestMethod]
    public void Calculate_EndPage_PostNumber_EndPost_OverLimit()
    {
        var (threadInfo, quest) = GetStandardInfoByPostNumber(650);

        Assert.IsNotNull(threadInfo);
        Assert.IsNotNull(quest);

        Assert.AreEqual(25, threadInfo.ThreadRange.EndPage);
    }

    [TestMethod]
    public void Calculate_EndPage_PostId_EndOfThread_PageLimit()
    {
        var (threadInfo, quest) = GetStandardInfoByPostId();

        Assert.IsNotNull(threadInfo);
        Assert.IsNotNull(quest);

        Assert.AreEqual(25, threadInfo.ThreadRange.EndPage);
    }
}
