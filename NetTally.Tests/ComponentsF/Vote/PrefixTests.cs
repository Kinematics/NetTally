using Microsoft.VisualStudio.TestTools.UnitTesting;
using NetTally.Tally.ComponentsF.Vote;

namespace NetTally.Tests.ComponentsF.Vote;
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
        Assert.AreEqual(Prefix.Empty, prefix);
    }

    [TestMethod]
    public void Construct_Empty_Empty()
    {
        var prefix = Prefix.Create("");
        Assert.IsNotNull(prefix);
        Assert.AreEqual(Prefix.Empty, prefix);
    }

    [TestMethod]
    public void Construct_Whitespace_Empty()
    {
        var prefix = Prefix.Create("  ");
        Assert.IsNotNull(prefix);
        Assert.AreEqual(Prefix.Empty, prefix);
    }

    [TestMethod]
    public void Construct_Invalid_Empty()
    {
        var prefix = Prefix.Create("~~~");
        Assert.IsNotNull(prefix);
        Assert.AreEqual(Prefix.Empty, prefix);
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
    public void Reduce_FromThree()
    {
        var prefix = Prefix.Create("- - -");
        Assert.IsNotNull(prefix);
        Assert.AreEqual(3, prefix.Depth);

        var reduced = Prefix.Reduce(prefix);
        Assert.AreEqual(2, reduced.Depth);
    }

    [TestMethod]
    public void Reduce_FromOne()
    {
        var prefix = Prefix.Create("-");
        Assert.IsNotNull(prefix);
        Assert.AreEqual(1, prefix.Depth);

        var reduced = Prefix.Reduce(prefix);
        Assert.AreEqual(0, reduced.Depth);
    }

    [TestMethod]
    public void Reduce_FromZero()
    {
        var prefix = Prefix.Create("");
        Assert.IsNotNull(prefix);
        Assert.AreEqual(0, prefix.Depth);

        var reduced = Prefix.Reduce(prefix);
        Assert.AreEqual(0, reduced.Depth);
    }
}
