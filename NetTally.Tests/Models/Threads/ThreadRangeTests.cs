using Microsoft.VisualStudio.TestTools.UnitTesting;
using NetTally.Models;

namespace NetTally.Tests.Models.Threads;

[TestClass]
public class ThreadRangeTests
{
    [ClassInitialize]
    public static void ClassInit(TestContext _)
    {
        TestStartup.ConfigureServices();
    }

    [TestMethod]
    public void No_ThreadRange()
    {
        var range = ThreadRange.None;
        Assert.IsNotNull(range);
        Assert.AreEqual(1, range.StartPage);
        Assert.AreEqual(1, range.EndPage);
    }

    [TestMethod]
    public void Create_ByPostId_Empty()
    {
        var postId = PostId.Create(12345);
        var range = ThreadRangeCreation.CreateByPostId(postId, 0, 0);

        Assert.IsNotNull(range);
        Assert.IsTrue(range is ThreadRangeByStartingId);
        var idRange = range as ThreadRangeByStartingId;
        Assert.IsNotNull(idRange);
        Assert.AreEqual(postId, idRange.StartingPostId);
        Assert.AreEqual(1, range.StartPage);
        Assert.AreEqual(1, range.EndPage);
    }

    [TestMethod]
    public void Create_ByPostId_Simple()
    {
        var postId = PostId.Create(12345);
        var range = ThreadRangeCreation.CreateByPostId(postId, 5, 10);

        Assert.IsNotNull(range);
        Assert.IsTrue(range is ThreadRangeByStartingId);
        Assert.AreEqual(5, range.StartPage);
        Assert.AreEqual(10, range.EndPage);
    }

    [TestMethod]
    public void Create_ByStartOfRange_Simple()
    {
        var postId = PostId.Create(12345);
        var range = ThreadRangeCreation.CreateByStartOfRange(123, 25, 10);

        Assert.IsNotNull(range);
        Assert.IsTrue(range is ThreadRangeByStartingPost);
        var postsRange = range as ThreadRangeByStartingPost;
        Assert.IsNotNull(postsRange);
        Assert.IsTrue(postsRange.StartPostNumber == 123);
        Assert.AreEqual(5, range.StartPage);
        Assert.AreEqual(10, range.EndPage);
    }

    [TestMethod]
    public void Create_ByRange_NoEnd()
    {
        var postId = PostId.Create(12345);
        var range = ThreadRangeCreation.CreateByRange(123, 0, 25, 10);

        Assert.IsNotNull(range);
        Assert.IsTrue(range is ThreadRangeByStartingPost);
        var postsRange = range as ThreadRangeByStartingPost;
        Assert.IsNotNull(postsRange);
        Assert.IsTrue(postsRange.StartPostNumber == 123);
        Assert.AreEqual(5, range.StartPage);
        Assert.AreEqual(10, range.EndPage);
    }

    [TestMethod]
    public void Create_ByRange_Range()
    {
        var postId = PostId.Create(12345);
        var range = ThreadRangeCreation.CreateByRange(123, 180, 25, 10);

        Assert.IsNotNull(range);
        Assert.IsTrue(range is ThreadRangeByPostRange);
        var postsRange = range as ThreadRangeByPostRange;
        Assert.IsNotNull(postsRange);
        Assert.IsTrue(postsRange.StartPostNumber == 123);
        Assert.IsTrue(postsRange.EndPostNumber == 180);
        Assert.AreEqual(5, range.StartPage);
        Assert.AreEqual(8, range.EndPage);
    }
}
