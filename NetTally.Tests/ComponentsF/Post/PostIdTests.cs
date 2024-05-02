using Microsoft.VisualStudio.TestTools.UnitTesting;
using NetTally.Tally.ComponentsF.Post;

namespace NetTally.Tests.ComponentsF.Post;
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
        var postId = PostId.Create(0);
        Assert.AreEqual(PostId.Zero, postId);
    }

    [TestMethod]
    public void Construct_Negative1_Zero()
    {
        var postId = PostId.Create(-1);
        Assert.AreEqual(PostId.Zero, postId);
    }

    [TestMethod]
    public void Construct_Negative2_Zero()
    {
        var postId = PostId.Create(-102366);
        Assert.AreEqual(PostId.Zero, postId);
    }

    [TestMethod]
    public void Construct_Positive_Normal()
    {
        var postId = PostId.Create(102366);
        Assert.AreEqual(102366, postId.Id);
    }

    [TestMethod]
    public void Construct_String_Null()
    {
        var postId = PostId.Create(null!);
        Assert.IsNull(postId);
    }

    [TestMethod]
    public void Construct_String_Empty()
    {
        var postId = PostId.Create("");
        Assert.IsNull(postId);
    }

    [TestMethod]
    public void Construct_String_Invalid()
    {
        var postId = PostId.Create("Ab123");
        Assert.IsNull(postId);
    }

    [TestMethod]
    public void Construct_String_Normal()
    {
        var postId = PostId.Create("102366");
        Assert.IsNotNull(postId);
        Assert.AreEqual(102366, postId.Id);
    }

    [TestMethod]
    public void Construct_String_Zero()
    {
        var postId = PostId.Create("0");
        Assert.IsNotNull(postId);
        Assert.AreEqual(PostId.Zero, postId);
    }

    [TestMethod]
    public void Construct_String_Comma()
    {
        var postId = PostId.Create("102,366");
        Assert.IsNotNull(postId);
        Assert.AreEqual(102366, postId.Id);
    }

    [TestMethod]
    public void Construct_String_Negative()
    {
        var postId = PostId.Create("-102366");
        Assert.IsNull(postId);
    }

    [TestMethod]
    public void Construct_String_NegativeComma()
    {
        var postId = PostId.Create("-102,366");
        Assert.IsNull(postId);
    }

    [TestMethod]
    public void Construct_Int_Overflow()
    {
        var postId = PostId.Create("4,294,967,296");
        Assert.IsNotNull(postId);
        Assert.AreEqual(4294967296, postId.Id);
        Assert.IsTrue(postId.Id == 4294967296);
    }

}
