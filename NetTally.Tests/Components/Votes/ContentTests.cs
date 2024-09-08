using Microsoft.VisualStudio.TestTools.UnitTesting;
using NetTally.Tally.Components.Votes;

namespace NetTally.Tests.Components.Votes;
[TestClass]
public class ContentTests
{
    [ClassInitialize]
    public static void ClassInit(TestContext _)
    {
        TestStartup.ConfigureServices();
    }

    [TestMethod]
    public void Construct_Null_Error()
    {
        var content = VoteContent.Create(null!);
        Assert.IsNull(content);
    }

    [TestMethod]
    public void Construct_Empty_Error()
    {
        var content = VoteContent.Create("");
        Assert.IsNull(content);
    }

    [TestMethod]
    public void Construct_Whitespace_Error()
    {
        var content = VoteContent.Create("    ");
        Assert.IsNull(content);
    }

    [TestMethod]
    public void Construct_Standard()
    {
        string text = "Go to the farm";
        var content = VoteContent.Create(text);
        Assert.IsNotNull(content);
        Assert.AreEqual(text, content.Content);
        Assert.AreEqual(text, content.CleanContent);
    }

    [TestMethod]
    public void Construct_Standard_WithTrim()
    {
        string text1 = "Go to the farm ";
        string text2 = "Go to the farm";
        var content = VoteContent.Create(text1);
        Assert.IsNotNull(content);
        Assert.AreEqual(text2, content.Content);
        Assert.AreEqual(text2, content.CleanContent);
    }

    [TestMethod]
    public void Construct_Unsafe_Removed()
    {
        string text1 = "Go to​​\u200b the farm ";
        string text2 = "Go to the farm";
        var content = VoteContent.Create(text1);
        Assert.IsNotNull(content);
        Assert.AreEqual(text2, content.Content);
        Assert.AreEqual(text2, content.CleanContent);
    }

    [TestMethod]
    public void Construct_BBCode_Bold()
    {
        string text1 = "Go to the 『b』farm『/b』";
        string text2 = "Go to the farm";
        var content = VoteContent.Create(text1);
        Assert.IsNotNull(content);
        Assert.AreEqual(text1, content.Content);
        Assert.AreEqual(text2, content.CleanContent);
    }

    [TestMethod]
    public void Construct_BBCode_Italic()
    {
        string text1 = "Go to the 『i』farm『/i』";
        string text2 = "Go to the farm";
        var content = VoteContent.Create(text1);
        Assert.IsNotNull(content);
        Assert.AreEqual(text1, content.Content);
        Assert.AreEqual(text2, content.CleanContent);
    }

    [TestMethod]
    public void Construct_BBCode_Underline()
    {
        string text1 = "Go to the 『u』farm『/u』";
        string text2 = "Go to the farm";
        var content = VoteContent.Create(text1);
        Assert.IsNotNull(content);
        Assert.AreEqual(text1, content.Content);
        Assert.AreEqual(text2, content.CleanContent);
    }

    [TestMethod]
    public void Construct_BBCode_Strike()
    {
        string text1 = "Go to the 『s』farm『/s』";
        string text2 = "Go to the farm";
        var content = VoteContent.Create(text1);
        Assert.IsNotNull(content);
        Assert.AreEqual(text1, content.Content);
        Assert.AreEqual(text2, content.CleanContent);
    }

    [TestMethod]
    public void Construct_BBCode_ColorName()
    {
        string text1 = "Go to the 『color=orange』farm『/color』";
        string text2 = "Go to the farm";
        var content = VoteContent.Create(text1);
        Assert.IsNotNull(content);
        Assert.AreEqual(text1, content.Content);
        Assert.AreEqual(text2, content.CleanContent);
    }

    [TestMethod]
    public void Construct_BBCode_ColorCode()
    {
        string text1 = "Go to the 『color=#ff00AA』farm『/color』";
        string text2 = "Go to the farm";
        var content = VoteContent.Create(text1);
        Assert.IsNotNull(content);
        Assert.AreEqual(text1, content.Content);
        Assert.AreEqual(text2, content.CleanContent);
    }
}
