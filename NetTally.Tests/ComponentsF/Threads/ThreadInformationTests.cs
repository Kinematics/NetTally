using Microsoft.VisualStudio.TestTools.UnitTesting;
using NetTally.Data;
using NetTally.Tally.ComponentsF.Posts;
using NetTally.Tally.ComponentsF.Threads;

namespace NetTally.Tests.ComponentsF.Threads;
[TestClass]
public class ThreadInformationTests
{
    [ClassInitialize]
    public static void ClassInit(TestContext _)
    {
        TestStartup.ConfigureServices();
    }

    [TestMethod]
    public void Construct_PostId_Basic()
    {
        string title = "Jupiter Hop";
        string authorName = "Jack";
        var author = Author.Create(authorName);
        var postId = PostId.Create(123456);
        int pageNumberOfStartPost = 5;
        int pagesInThread = 14;

        var threadInfo = ThreadInformation.CreateByPostId(title, author, postId, pageNumberOfStartPost, pagesInThread);

        Assert.IsNotNull(threadInfo);
        Assert.AreEqual(title, threadInfo.Title);
        Assert.AreEqual(authorName, threadInfo.Author.Name);
        Assert.AreEqual(0, threadInfo.StartPostNumber);
        Assert.AreEqual(pageNumberOfStartPost, threadInfo.PageNumberOfStartPost);
        Assert.AreEqual(pagesInThread, threadInfo.PagesInThread);
    }

    [TestMethod]
    public void Construct_PostNumber_Basic()
    {
        string title = "Jupiter Hop";
        string authorName = "Jack";
        var author = Author.Create(authorName);
        int startPost = 300;
        int pagesInThread = 14;

        var threadInfo = ThreadInformation.CreateByPostNumber(title, author, startPost, pagesInThread);

        Assert.IsNotNull(threadInfo);
        Assert.AreEqual(title, threadInfo.Title);
        Assert.AreEqual(authorName, threadInfo.Author.Name);
        Assert.AreEqual(startPost, threadInfo.StartPostNumber);
        Assert.AreEqual(0, threadInfo.PageNumberOfStartPost);
        Assert.AreEqual(pagesInThread, threadInfo.PagesInThread);
    }

    [TestMethod]
    public void Construct_PostNumber_ZeroStart()
    {
        string title = "Jupiter Hop";
        string authorName = "Jack";
        var author = Author.Create(authorName);
        int startPost = 0;
        int pagesInThread = 14;

        var threadInfo = ThreadInformation.CreateByPostNumber(title, author, startPost, pagesInThread);

        Assert.IsNotNull(threadInfo);
        Assert.AreEqual(title, threadInfo.Title);
        Assert.AreEqual(authorName, threadInfo.Author.Name);
        Assert.AreEqual(1, threadInfo.StartPostNumber);
        Assert.AreEqual(0, threadInfo.PageNumberOfStartPost);
        Assert.AreEqual(pagesInThread, threadInfo.PagesInThread);
    }

    [TestMethod]
    public void Construct_Untitled()
    {
        string title = "";
        string authorName = "Jack";
        var author = Author.Create(authorName);
        int startPost = 0;
        int pagesInThread = 14;

        var threadInfo = ThreadInformation.CreateByPostNumber(title, author, startPost, pagesInThread);

        Assert.IsNotNull(threadInfo);
        Assert.AreEqual(Strings.UntitledThread, threadInfo.Title);
        Assert.AreEqual(authorName, threadInfo.Author.Name);
        Assert.AreEqual(1, threadInfo.StartPostNumber);
        Assert.AreEqual(0, threadInfo.PageNumberOfStartPost);
        Assert.AreEqual(pagesInThread, threadInfo.PagesInThread);
    }

    [TestMethod]
    public void Construct_NoAuthor()
    {
        string title = "";
        AuthorType? author = null;
        int startPost = 0;
        int pagesInThread = 14;

        var threadInfo = ThreadInformation.CreateByPostNumber(title, author, startPost, pagesInThread);

        Assert.IsNotNull(threadInfo);
        Assert.AreEqual(Strings.UntitledThread, threadInfo.Title);
        Assert.AreEqual(Strings.UnknownAuthor, threadInfo.Author.Name);
        Assert.AreEqual(1, threadInfo.StartPostNumber);
        Assert.AreEqual(0, threadInfo.PageNumberOfStartPost);
        Assert.AreEqual(pagesInThread, threadInfo.PagesInThread);
    }

    private static (ThreadInformationType ThreadInfo, Quest Quest) GetStandardInfoByPostNumber()
    {
        Quest quest = new Quest
        {
            StartPost = 410,
            EndPost = 0,
            PostsPerPage = 25
        };

        string title = "Jupiter Hop";
        string authorName = "Jack";
        var author = Author.Create(authorName);
        int pagesInThread = 25;

        var threadInfo = ThreadInformation.CreateByPostNumber(title, author, quest.StartPost, pagesInThread);

        return (threadInfo, quest);
    }

    private static (ThreadInformationType ThreadInfo, Quest Quest) GetStandardInfoByPostId()
    {
        Quest quest = new Quest
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

        var threadInfo = ThreadInformation.CreateByPostId(title, author, postId, pageNumberOfStartPost, pagesInThread);

        return (threadInfo, quest);
    }

    [TestMethod]
    public void Calculate_StartPage_PostNumber()
    {
        var (threadInfo, quest) = GetStandardInfoByPostNumber();

        Assert.IsNotNull(threadInfo);
        Assert.IsNotNull(quest);

        int startPage = ThreadInformation.GetStartPage(threadInfo, quest);
        Assert.AreEqual(17, startPage);
    }

    [TestMethod]
    public void Calculate_StartPage_PostId()
    {
        var (threadInfo, quest) = GetStandardInfoByPostId();

        Assert.IsNotNull(threadInfo);
        Assert.IsNotNull(quest);

        int startPage = ThreadInformation.GetStartPage(threadInfo, quest);
        Assert.AreEqual(5, startPage);
    }

    [TestMethod]
    public void Calculate_EndPage_PostNumber_EndOfThread_PageLimit()
    {
        var (threadInfo, quest) = GetStandardInfoByPostNumber();

        Assert.IsNotNull(threadInfo);
        Assert.IsNotNull(quest);

        int endPage = ThreadInformation.GetEndPage(threadInfo, quest);
        Assert.AreEqual(25, endPage);
    }

    [TestMethod]
    public void Calculate_EndPage_PostNumber_EndPost()
    {
        var (threadInfo, quest) = GetStandardInfoByPostNumber();

        Assert.IsNotNull(threadInfo);
        Assert.IsNotNull(quest);

        quest.EndPost = 510;

        int endPage = ThreadInformation.GetEndPage(threadInfo, quest);
        Assert.AreEqual(21, endPage);
    }

    [TestMethod]
    public void Calculate_EndPage_PostNumber_EndPost_OverLimit()
    {
        var (threadInfo, quest) = GetStandardInfoByPostNumber();

        Assert.IsNotNull(threadInfo);
        Assert.IsNotNull(quest);

        quest.EndPost = 650;

        int endPage = ThreadInformation.GetEndPage(threadInfo, quest);
        Assert.AreEqual(25, endPage);
    }

    [TestMethod]
    public void Calculate_EndPage_PostId_EndOfThread_PageLimit()
    {
        var (threadInfo, quest) = GetStandardInfoByPostId();

        Assert.IsNotNull(threadInfo);
        Assert.IsNotNull(quest);

        int endPage = ThreadInformation.GetEndPage(threadInfo, quest);
        Assert.AreEqual(25, endPage);
    }

    [TestMethod]
    public void Calculate_EndPage_PostId_NoPages()
    {
        var (threadInfo, quest) = GetStandardInfoByPostId();

        Assert.IsNotNull(threadInfo);
        Assert.IsNotNull(quest);

        threadInfo = threadInfo with { PagesInThread = 0 };

        int endPage = ThreadInformation.GetEndPage(threadInfo, quest);
        Assert.AreEqual(1, endPage);
    }
}
