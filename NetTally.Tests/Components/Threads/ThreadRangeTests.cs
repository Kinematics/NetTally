using Microsoft.VisualStudio.TestTools.UnitTesting;
using NetTally.Tally.Components.Posts;
using NetTally.Tally.Components.Threads;

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
        var range = PostRanges.CreateByPostId(postId, 0, 0);

        Assert.IsNotNull(range);
        Assert.IsTrue(range is PostRangeById);
        var idRange = range as PostRangeById;
        Assert.IsNotNull(idRange);
        Assert.AreEqual(postId, idRange.PostId);
        Assert.AreEqual(1, range.GetStartPage());
        Assert.AreEqual(1, range.GetEndPage());
    }

    [TestMethod]
    public void Create_ByPostId_Simple()
    {
        var postId = PostId.Create(12345);
        var range = PostRanges.CreateByPostId(postId, 5, 10);

        Assert.IsNotNull(range);
        Assert.IsTrue(range is PostRangeById);
        Assert.AreEqual(5, range.GetStartPage());
        Assert.AreEqual(10, range.GetEndPage());
    }

    [TestMethod]
    public void Create_ByStartOfRange_Simple()
    {
        var postId = PostId.Create(12345);
        var range = PostRanges.CreateByStartOfRange(123, 25, 10);

        Assert.IsNotNull(range);
        Assert.IsTrue(range is PostRangeByPosts);
        var postsRange = range as PostRangeByPosts;
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
        var range = PostRanges.CreateByRange(123, 0, 25, 10);

        Assert.IsNotNull(range);
        Assert.IsTrue(range is PostRangeByPosts);
        var postsRange = range as PostRangeByPosts;
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
        var range = PostRanges.CreateByRange(123, 180, 25, 10);

        Assert.IsNotNull(range);
        Assert.IsTrue(range is PostRangeByPosts);
        var postsRange = range as PostRangeByPosts;
        Assert.IsNotNull(postsRange);
        Assert.AreEqual(123, postsRange.StartPostNumber);
        Assert.AreEqual(180, postsRange.EndPostNumber);
        Assert.AreEqual(5, range.GetStartPage());
        Assert.AreEqual(8, range.GetEndPage());
    }
}
