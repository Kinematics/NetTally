using Microsoft.VisualStudio.TestTools.UnitTesting;
using NetTally.Tally.Posts.Component;
using NetTally.Tally.Posts.Component.Creation;
using NetTally.Tally.Threads;

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
        var range = ThreadRanges.CreateByPostId(postId, 0, 0);

        Assert.IsNotNull(range);
        Assert.IsTrue(range is ThreadRangeById);
        var idRange = range as ThreadRangeById;
        Assert.IsNotNull(idRange);
        Assert.AreEqual(postId, idRange.PostId);
        Assert.AreEqual(1, range.GetStartPage());
        Assert.AreEqual(1, range.GetEndPage());
    }

    [TestMethod]
    public void Create_ByPostId_Simple()
    {
        var postId = PostId.Create(12345);
        var range = ThreadRanges.CreateByPostId(postId, 5, 10);

        Assert.IsNotNull(range);
        Assert.IsTrue(range is ThreadRangeById);
        Assert.AreEqual(5, range.GetStartPage());
        Assert.AreEqual(10, range.GetEndPage());
    }

    [TestMethod]
    public void Create_ByStartOfRange_Simple()
    {
        var postId = PostId.Create(12345);
        var range = ThreadRanges.CreateByStartOfRange(123, 25, 10);

        Assert.IsNotNull(range);
        Assert.IsTrue(range is ThreadRangeByPosts);
        var postsRange = range as ThreadRangeByPosts;
        Assert.IsNotNull(postsRange);
        Assert.AreEqual(123, postsRange.StartPostNumber);
        Assert.AreEqual(0, postsRange.EndPostNumber);
        Assert.AreEqual(5, range.GetStartPage());
        Assert.AreEqual(10, range.GetEndPage());
    }

    [TestMethod]
    public void Create_ByRange_NoEnd()
    {
        var postId = PostId.Create(12345);
        var range = ThreadRanges.CreateByRange(123, 0, 25, 10);

        Assert.IsNotNull(range);
        Assert.IsTrue(range is ThreadRangeByPosts);
        var postsRange = range as ThreadRangeByPosts;
        Assert.IsNotNull(postsRange);
        Assert.AreEqual(123, postsRange.StartPostNumber);
        Assert.AreEqual(0, postsRange.EndPostNumber);
        Assert.AreEqual(5, range.GetStartPage());
        Assert.AreEqual(10, range.GetEndPage());
    }

    [TestMethod]
    public void Create_ByRange_Range()
    {
        var postId = PostId.Create(12345);
        var range = ThreadRanges.CreateByRange(123, 180, 25, 10);

        Assert.IsNotNull(range);
        Assert.IsTrue(range is ThreadRangeByPosts);
        var postsRange = range as ThreadRangeByPosts;
        Assert.IsNotNull(postsRange);
        Assert.AreEqual(123, postsRange.StartPostNumber);
        Assert.AreEqual(180, postsRange.EndPostNumber);
        Assert.AreEqual(5, range.GetStartPage());
        Assert.AreEqual(8, range.GetEndPage());
    }
}
