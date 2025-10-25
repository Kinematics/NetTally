using Microsoft.VisualStudio.TestTools.UnitTesting;
using NetTally.Models;

namespace NetTally.Tests.Models.Votes;

[TestClass]
public class PrefixTests
{
    [ClassInitialize]
    public static void ClassInit(TestContext _)
    {
        TestStartup.ConfigureServices();
    }

    [TestMethod]
    public void Construct_Null_Empty()
    {
        var prefix = Prefix.Create(null!);
        Assert.IsNotNull(prefix);
        Assert.AreEqual(Prefix.None, prefix);
    }

    [TestMethod]
    public void Construct_Empty_Empty()
    {
        var prefix = Prefix.Create("");
        Assert.IsNotNull(prefix);
        Assert.AreEqual(Prefix.None, prefix);
    }

    [TestMethod]
    public void Construct_Whitespace_Empty()
    {
        var prefix = Prefix.Create("  ");
        Assert.IsNotNull(prefix);
        Assert.AreEqual(Prefix.None, prefix);
    }

    [TestMethod]
    public void Construct_Invalid_Empty()
    {
        var prefix = Prefix.Create("~~~");
        Assert.IsNotNull(prefix);
        Assert.AreEqual(Prefix.None, prefix);
    }

    [TestMethod]
    public void Construct_Standard1()
    {
        var prefix = Prefix.Create("-");
        Assert.IsNotNull(prefix);
        Assert.AreEqual(1, prefix.Depth);
    }

    [TestMethod]
    public void Construct_Standard3()
    {
        var prefix = Prefix.Create("---");
        Assert.IsNotNull(prefix);
        Assert.AreEqual(3, prefix.Depth);
    }

    [TestMethod]
    public void Construct_Standard3_WithSpacing()
    {
        var prefix = Prefix.Create("- - -");
        Assert.IsNotNull(prefix);
        Assert.AreEqual(3, prefix.Depth);
        Assert.AreEqual("---", prefix.Indent);
    }

    [TestMethod]
    public void Construct_EmDash()
    {
        var prefix = Prefix.Create("—");
        Assert.IsNotNull(prefix);
        Assert.AreEqual(1, prefix.Depth);
    }

    [TestMethod]
    public void Construct_EnDash()
    {
        var prefix = Prefix.Create("– ");
        Assert.IsNotNull(prefix);
        Assert.AreEqual(1, prefix.Depth);
    }

    [TestMethod]
    public void Reduce_FromThree()
    {
        var prefix = Prefix.Create("- - -");
        Assert.IsNotNull(prefix);
        Assert.AreEqual(3, prefix.Depth);

        var reduced = prefix.Promote();
        Assert.AreEqual(2, reduced.Depth);
    }

    [TestMethod]
    public void Reduce_FromOne()
    {
        var prefix = Prefix.Create("-");
        Assert.IsNotNull(prefix);
        Assert.AreEqual(1, prefix.Depth);

        var reduced = prefix.Promote();
        Assert.AreEqual(0, reduced.Depth);
    }

    [TestMethod]
    public void Reduce_FromZero()
    {
        var prefix = Prefix.Create("");
        Assert.IsNotNull(prefix);
        Assert.AreEqual(0, prefix.Depth);

        var reduced = prefix.Promote();
        Assert.AreEqual(0, reduced.Depth);
    }

    [TestMethod]
    public void Reduce_ByZero()
    {
        var prefix = Prefix.Create("--");
        Assert.IsNotNull(prefix);
        Assert.AreEqual(2, prefix.Depth);

        var reduced = prefix.Promote(0);
        Assert.AreEqual(2, reduced.Depth);
    }

    [TestMethod]
    public void Reduce_ByNegative()
    {
        var prefix = Prefix.Create("--");
        Assert.IsNotNull(prefix);
        Assert.AreEqual(2, prefix.Depth);

        var reduced = prefix.Promote(-5);
        Assert.AreEqual(2, reduced.Depth);
    }

}
