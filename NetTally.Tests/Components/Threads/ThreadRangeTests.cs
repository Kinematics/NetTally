using Microsoft.VisualStudio.TestTools.UnitTesting;
using NetTally.Tally.ComponentsF.Posts;
using NetTally.Tally.ComponentsF.Threads;

namespace NetTally.Tests.Components.Threads;
[TestClass]
public class ThreadRangeTests
{
    [ClassInitialize]
    public static void ClassInit(TestContext _)
    {
        TestStartup.ConfigureServices();
    }

    [TestMethod]
    public void Create_ByPost_Empty()
    {
        var range = ThreadRange.CreateRangeByPost();
        Assert.IsNotNull(range);
        Assert.AreEqual(ThreadRange.StartOfThread, range);
    }

    [TestMethod]
    public void Create_ByPost_Simple()
    {
        var range = ThreadRange.CreateRangeByPost(50);
        Assert.IsNotNull(range);
        Assert.AreEqual(50, range.StartPostNumber);
        Assert.AreEqual(0, range.PageCount);
    }

    [TestMethod]
    public void Create_ByPost_WithPages()
    {
        var range = ThreadRange.CreateRangeByPost(50, 20);
        Assert.IsNotNull(range);
        Assert.AreEqual(50, range.StartPostNumber);
        Assert.AreEqual(20, range.PageCount);
    }

    [TestMethod]
    public void Create_ByPostId_Simple()
    {
        var postId = PostId.Create(1234567);

        var range = ThreadRange.CreateRangeFromPostId(postId, 1);
        Assert.IsNotNull(range);
        Assert.AreEqual(0, range.StartPostNumber);
        Assert.AreEqual(postId, range.PostId);
        Assert.AreEqual(1, range.PageNumber);
    }
}
