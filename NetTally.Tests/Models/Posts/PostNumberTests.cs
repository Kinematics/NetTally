using Microsoft.VisualStudio.TestTools.UnitTesting;
using NetTally.Models;

namespace NetTally.Tests.Models.Posts;

[TestClass]
public class PostNumberTests
{
    [ClassInitialize]
    public static void ClassInit(TestContext _)
    {
        TestStartup.ConfigureServices();
    }

    [TestMethod]
    public void Construct_Zero_Null()
    {
        var postNumber = PostNumber.Create(0);
        Assert.IsNull(postNumber);
    }

    [TestMethod]
    public void Construct_Negative1_Null()
    {
        var postNumber = PostNumber.Create(-1);
        Assert.IsNull(postNumber);
    }

    [TestMethod]
    public void Construct_Negative2_Null()
    {
        var postNumber = PostNumber.Create(-102366);
        Assert.IsNull(postNumber);
    }

    [TestMethod]
    public void Construct_Positive_Normal()
    {
        var postNumber = PostNumber.Create(102366);
        Assert.IsNotNull(postNumber);
        Assert.AreEqual(102366, postNumber.Value);
    }

    [TestMethod]
    public void Construct_String_Null()
    {
        var postNumber = PostNumber.Create(null!);
        Assert.IsNull(postNumber);
    }

    [TestMethod]
    public void Construct_String_Empty()
    {
        var postNumber = PostNumber.Create("");
        Assert.IsNull(postNumber);
    }

    [TestMethod]
    public void Construct_String_Invalid()
    {
        var postNumber = PostNumber.Create("Ab123");
        Assert.IsNull(postNumber);
    }

    [TestMethod]
    public void Construct_String_Normal()
    {
        var postNumber = PostNumber.Create("102366");
        Assert.IsNotNull(postNumber);
        Assert.AreEqual(102366, postNumber.Value);
    }

    [TestMethod]
    public void Construct_ZeroString_Null()
    {
        var postNumber = PostNumber.Create("0");
        Assert.IsNull(postNumber);
    }

    [TestMethod]
    public void Construct_String_Comma()
    {
        var postNumber = PostNumber.Create("102,366");
        Assert.IsNotNull(postNumber);
        Assert.AreEqual(102366, postNumber.Value);
    }

    [TestMethod]
    public void Construct_NegativeString_Null()
    {
        var postNumber = PostNumber.Create("-102366");
        Assert.IsNull(postNumber);
    }

    [TestMethod]
    public void Construct_NegativeString_Comma_Null()
    {
        var postNumber = PostNumber.Create("-102,366");
        Assert.IsNull(postNumber);
    }

    [TestMethod]
    public void Construct_Int_Overflow()
    {
        var postNumber = PostNumber.Create("4,294,967,296");
        Assert.IsNotNull(postNumber);
        Assert.AreEqual(4294967296, postNumber.Value);
#pragma warning disable MSTEST0037 // Use proper 'Assert' methods
        Assert.IsTrue(postNumber.Value == 4294967296);
#pragma warning restore MSTEST0037 // Use proper 'Assert' methods
    }

}
