using Microsoft.VisualStudio.TestTools.UnitTesting;
using NetTally.Models.Behavior;
using NetTally.Models.Creation;
using NetTally.Models.Posts;
using NetTally.Models.Threads;

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
    public void Create_ByPostId_Empty()
    {
        var postId = PostId.Create(12345);
        var range = ThreadRangeCreation.CreateByPostId(postId, 0, 0);

        Assert.IsNotNull(range);
        Assert.IsTrue(range is ThreadRangeById);
        var idRange = range as ThreadRangeById;
        Assert.IsNotNull(idRange);
        Assert.AreEqual(postId, idRange.PostId);
        Assert.AreEqual(1, range.StartPage);
        Assert.AreEqual(1, range.EndPage);
    }

    [TestMethod]
    public void Create_ByPostId_Simple()
    {
        var postId = PostId.Create(12345);
        var range = ThreadRangeCreation.CreateByPostId(postId, 5, 10);

        Assert.IsNotNull(range);
        Assert.IsTrue(range is ThreadRangeById);
        Assert.AreEqual(5, range.StartPage);
        Assert.AreEqual(10, range.EndPage);
    }

    [TestMethod]
    public void Create_ByStartOfRange_Simple()
    {
        var postId = PostId.Create(12345);
        var range = ThreadRangeCreation.CreateByStartOfRange(123, 25, 10);

        Assert.IsNotNull(range);
        Assert.IsTrue(range is ThreadRangeByPosts);
        var postsRange = range as ThreadRangeByPosts;
        Assert.IsNotNull(postsRange);
        Assert.AreEqual(123, postsRange.StartPostNumber);
        Assert.AreEqual(0, postsRange.EndPostNumber);
        Assert.AreEqual(5, range.StartPage);
        Assert.AreEqual(10, range.EndPage);
    }

    [TestMethod]
    public void Create_ByRange_NoEnd()
    {
        var postId = PostId.Create(12345);
        var range = ThreadRangeCreation.CreateByRange(123, 0, 25, 10);

        Assert.IsNotNull(range);
        Assert.IsTrue(range is ThreadRangeByPosts);
        var postsRange = range as ThreadRangeByPosts;
        Assert.IsNotNull(postsRange);
        Assert.AreEqual(123, postsRange.StartPostNumber);
        Assert.AreEqual(0, postsRange.EndPostNumber);
        Assert.AreEqual(5, range.StartPage);
        Assert.AreEqual(10, range.EndPage);
    }

    [TestMethod]
    public void Create_ByRange_Range()
    {
        var postId = PostId.Create(12345);
        var range = ThreadRangeCreation.CreateByRange(123, 180, 25, 10);

        Assert.IsNotNull(range);
        Assert.IsTrue(range is ThreadRangeByPosts);
        var postsRange = range as ThreadRangeByPosts;
        Assert.IsNotNull(postsRange);
        Assert.AreEqual(123, postsRange.StartPostNumber);
        Assert.AreEqual(180, postsRange.EndPostNumber);
        Assert.AreEqual(5, range.StartPage);
        Assert.AreEqual(8, range.EndPage);
    }
}
