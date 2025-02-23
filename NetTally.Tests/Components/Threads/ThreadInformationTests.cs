using Microsoft.VisualStudio.TestTools.UnitTesting;
using NetTally.Tally.Components.Posts;
using NetTally.Tally.Components.Threads;
using NetTally.Utility;

namespace NetTally.Tests.Components.Threads;
[TestClass]
public class ThreadInformationTests
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

        var postRange = PostRanges.CreateByPostId(postId, pageNumberOfStartPost, pagesInThread);
        var threadInfo = ThreadInformation.Create(title, author, postRange);

        Assert.IsNotNull(threadInfo);
        Assert.AreEqual(title, threadInfo.Title);
        Assert.AreEqual(authorName, threadInfo.Author.Name);
        Assert.AreEqual(pageNumberOfStartPost, threadInfo.PostRange.GetStartPage());
        Assert.AreEqual(pagesInThread, threadInfo.PostRange.GetEndPage());
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

        var postRange = PostRanges.CreateByPostId(postId, pageNumberOfStartPost, pagesInThread);
        var threadInfo = ThreadInformation.Create(title, author, postRange);

        Assert.IsNotNull(threadInfo);
        Assert.AreEqual(title, threadInfo.Title);
        Assert.AreEqual(authorName, threadInfo.Author.Name);
        Assert.AreEqual(5, threadInfo.PostRange.GetStartPage());
        Assert.AreEqual(1, threadInfo.PostRange.GetEndPage());
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

        var postRange = PostRanges.CreateByStartOfRange(startPost, postsPerPage, pagesInThread);
        var threadInfo = ThreadInformation.Create(title, author, postRange);

        Assert.IsNotNull(threadInfo);
        Assert.AreEqual(title, threadInfo.Title);
        Assert.AreEqual(authorName, threadInfo.Author.Name);
        Assert.AreEqual(10, threadInfo.PostRange.GetStartPage());
        Assert.AreEqual(14, threadInfo.PostRange.GetEndPage());
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

        var postRange = PostRanges.CreateByStartOfRange(startPost, postsPerPage, pagesInThread);
        var threadInfo = ThreadInformation.Create(title, author, postRange);

        Assert.IsNotNull(threadInfo);
        Assert.AreEqual(title, threadInfo.Title);
        Assert.AreEqual(authorName, threadInfo.Author.Name);
        Assert.AreEqual(1, threadInfo.PostRange.GetStartPage());
        Assert.AreEqual(14, threadInfo.PostRange.GetEndPage());
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

        var postRange = PostRanges.CreateByStartOfRange(startPost, postsPerPage, pagesInThread);
        var threadInfo = ThreadInformation.Create(title, author, postRange);

        Assert.IsNotNull(threadInfo);
        Assert.AreEqual(Strings.UntitledThread, threadInfo.Title);
        Assert.AreEqual(authorName, threadInfo.Author.Name);
        Assert.AreEqual(1, threadInfo.PostRange.GetStartPage());
        Assert.AreEqual(14, threadInfo.PostRange.GetEndPage());
    }

    [TestMethod]
    public void Construct_NoAuthor()
    {
        string title = "";
        AuthorType? author = null;
        int startPost = 0;
        int postsPerPage = 30;
        int pagesInThread = 14;

        var postRange = PostRanges.CreateByStartOfRange(startPost, postsPerPage, pagesInThread);
        var threadInfo = ThreadInformation.Create(title, author, postRange);

        Assert.IsNotNull(threadInfo);
        Assert.AreEqual(Strings.UntitledThread, threadInfo.Title);
        Assert.AreEqual(Strings.UnknownAuthor, threadInfo.Author.Name);
        Assert.AreEqual(1, threadInfo.PostRange.GetStartPage());
        Assert.AreEqual(14, threadInfo.PostRange.GetEndPage());
    }

    private static (ThreadInformationType ThreadInfo, Quest Quest) GetStandardInfoByPostNumber(
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

        var postRange = PostRanges.CreateByRange(quest.StartPost, endPost, quest.PostsPerPage, pagesInThread);
        var threadInfo = ThreadInformation.Create(title, author, postRange);

        return (threadInfo, quest);
    }

    private static (ThreadInformationType ThreadInfo, Quest Quest) GetStandardInfoByPostId()
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

        var postRange = PostRanges.CreateByPostId(postId, pageNumberOfStartPost, pagesInThread);
        var threadInfo = ThreadInformation.Create(title, author, postRange);

        return (threadInfo, quest);
    }

    [TestMethod]
    public void Calculate_StartPage_PostNumber()
    {
        var (threadInfo, quest) = GetStandardInfoByPostNumber(0);

        Assert.IsNotNull(threadInfo);
        Assert.IsNotNull(quest);

        Assert.AreEqual(17, threadInfo.PostRange.GetStartPage());
    }

    [TestMethod]
    public void Calculate_StartPage_PostId()
    {
        var (threadInfo, quest) = GetStandardInfoByPostId();

        Assert.IsNotNull(threadInfo);
        Assert.IsNotNull(quest);

        Assert.AreEqual(5, threadInfo.PostRange.GetStartPage());
    }

    [TestMethod]
    public void Calculate_EndPage_PostNumber_EndOfThread_PageLimit()
    {
        var (threadInfo, quest) = GetStandardInfoByPostNumber(0);

        Assert.IsNotNull(threadInfo);
        Assert.IsNotNull(quest);

        Assert.AreEqual(25, threadInfo.PostRange.GetEndPage());
    }

    [TestMethod]
    public void Calculate_EndPage_PostNumber_EndPost()
    {
        var (threadInfo, quest) = GetStandardInfoByPostNumber(510);

        Assert.IsNotNull(threadInfo);
        Assert.IsNotNull(quest);

        Assert.AreEqual(21, threadInfo.PostRange.GetEndPage());
    }

    [TestMethod]
    public void Calculate_EndPage_PostNumber_EndPost_OverLimit()
    {
        var (threadInfo, quest) = GetStandardInfoByPostNumber(650);

        Assert.IsNotNull(threadInfo);
        Assert.IsNotNull(quest);

        Assert.AreEqual(25, threadInfo.PostRange.GetEndPage());
    }

    [TestMethod]
    public void Calculate_EndPage_PostId_EndOfThread_PageLimit()
    {
        var (threadInfo, quest) = GetStandardInfoByPostId();

        Assert.IsNotNull(threadInfo);
        Assert.IsNotNull(quest);

        Assert.AreEqual(25, threadInfo.PostRange.GetEndPage());
    }
}
