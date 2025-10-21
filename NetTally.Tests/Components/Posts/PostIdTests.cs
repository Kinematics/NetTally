using Microsoft.VisualStudio.TestTools.UnitTesting;
using NetTally.Models.Creation;
using NetTally.Models.Defaults;
using NetTally.Models.Posts;

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
    public void Construct_Zero_Null()
    {
        var postId = PostId.Create(0);
        Assert.IsNull(postId);
    }

    [TestMethod]
    public void Construct_Negative1_Null()
    {
        var postId = PostId.Create(-1);
        Assert.IsNull(postId);
    }

    [TestMethod]
    public void Construct_Negative2_Null()
    {
        var postId = PostId.Create(-102366);
        Assert.IsNull(postId);
    }

    [TestMethod]
    public void Construct_Positive_Normal()
    {
        var postId = PostId.Create(102366);
        Assert.IsNotNull(postId);
        Assert.AreEqual(102366, postId.Value);
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
        Assert.AreEqual(102366, postId.Value);
    }

    [TestMethod]
    public void Construct_ZeroString_Null()
    {
        var postId = PostId.Create("0");
        Assert.IsNull(postId);
    }

    [TestMethod]
    public void Construct_String_Comma()
    {
        var postId = PostId.Create("102,366");
        Assert.IsNotNull(postId);
        Assert.AreEqual(102366, postId.Value);
    }

    [TestMethod]
    public void Construct_NegativeString_Null()
    {
        var postId = PostId.Create("-102366");
        Assert.IsNull(postId);
    }

    [TestMethod]
    public void Construct_NegativeString_Comma_Null()
    {
        var postId = PostId.Create("-102,366");
        Assert.IsNull(postId);
    }

    [TestMethod]
    public void Construct_Int_Overflow()
    {
        var postId = PostId.Create("4,294,967,296");
        Assert.IsNotNull(postId);
        Assert.AreEqual(4294967296, postId.Value);
        Assert.IsTrue(postId.Value == 4294967296);
    }

}
