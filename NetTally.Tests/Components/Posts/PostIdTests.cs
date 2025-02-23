using Microsoft.VisualStudio.TestTools.UnitTesting;
using NetTally.Tally.Components.Posts;

namespace NetTally.Tests.Components.Posts;
[TestClass]
public class PostIdTests
{
    [ClassInitialize]
    public static void ClassInit(TestContext _)
    {
        TestStartup.ConfigureServices();
    }

    [TestMethod]
    public void Construct_Zero_Zero()
    {
        var postId = PostIds.Create(0);
        Assert.AreEqual(PostIds.Zero, postId);
    }

    [TestMethod]
    public void Construct_Negative1_Zero()
    {
        var postId = PostIds.Create(-1);
        Assert.AreEqual(PostIds.Zero, postId);
    }

    [TestMethod]
    public void Construct_Negative2_Zero()
    {
        var postId = PostIds.Create(-102366);
        Assert.AreEqual(PostIds.Zero, postId);
    }

    [TestMethod]
    public void Construct_Positive_Normal()
    {
        var postId = PostIds.Create(102366);
        Assert.AreEqual(102366, postId.Id);
    }

    [TestMethod]
    public void Construct_String_Null()
    {
        var postId = PostIds.Create(null!);
        Assert.IsNull(postId);
    }

    [TestMethod]
    public void Construct_String_Empty()
    {
        var postId = PostIds.Create("");
        Assert.IsNull(postId);
    }

    [TestMethod]
    public void Construct_String_Invalid()
    {
        var postId = PostIds.Create("Ab123");
        Assert.IsNull(postId);
    }

    [TestMethod]
    public void Construct_String_Normal()
    {
        var postId = PostIds.Create("102366");
        Assert.IsNotNull(postId);
        Assert.AreEqual(102366, postId.Id);
    }

    [TestMethod]
    public void Construct_String_Zero()
    {
        var postId = PostIds.Create("0");
        Assert.IsNotNull(postId);
        Assert.AreEqual(PostIds.Zero, postId);
    }

    [TestMethod]
    public void Construct_String_Comma()
    {
        var postId = PostIds.Create("102,366");
        Assert.IsNotNull(postId);
        Assert.AreEqual(102366, postId.Id);
    }

    [TestMethod]
    public void Construct_String_Negative()
    {
        var postId = PostIds.Create("-102366");
        Assert.IsNull(postId);
    }

    [TestMethod]
    public void Construct_String_NegativeComma()
    {
        var postId = PostIds.Create("-102,366");
        Assert.IsNull(postId);
    }

    [TestMethod]
    public void Construct_Int_Overflow()
    {
        var postId = PostIds.Create("4,294,967,296");
        Assert.IsNotNull(postId);
        Assert.AreEqual(4294967296, postId.Id);
        Assert.IsTrue(postId.Id == 4294967296);
    }

}
