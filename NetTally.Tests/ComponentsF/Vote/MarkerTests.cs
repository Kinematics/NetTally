using Microsoft.VisualStudio.TestTools.UnitTesting;
using NetTally.Enums;
using NetTally.Tally.ComponentsF.Vote;

namespace NetTally.Tests.ComponentsF.Vote;
[TestClass]
public class MarkerTests
{
    [ClassInitialize]
    public static void ClassInit(TestContext _)
    {
        TestStartup.ConfigureServices();
    }

    [TestMethod]
    public void Check_Empty()
    {
        var marker = Marker.Empty;
        Assert.IsNotNull(marker);
        Assert.AreEqual(MarkerType.None, marker.MarkerType);
        Assert.AreEqual(0, marker.MarkerValue);
    }

    [TestMethod]
    public void Construct_Null_Error()
    {
        var marker = Marker.Create(null!);
        Assert.IsNull(marker);
    }

    [TestMethod]
    public void Construct_Empty_Error()
    {
        var marker = Marker.Create("");
        Assert.IsNull(marker);
    }

    [TestMethod]
    public void Construct_Whitespace_Error()
    {
        var marker = Marker.Create("   ");
        Assert.IsNull(marker);
    }

    [TestMethod]
    public void Construct_Vote1()
    {
        var marker = Marker.Create("X");
        Assert.IsNotNull(marker);
        Assert.AreEqual(MarkerType.Vote, marker.MarkerType);
        Assert.AreEqual(100, marker.MarkerValue);
    }

    [TestMethod]
    public void Construct_Vote1_Whitespace()
    {
        var marker = Marker.Create(" X");
        Assert.IsNotNull(marker);
        Assert.AreEqual(MarkerType.Vote, marker.MarkerType);
        Assert.AreEqual(100, marker.MarkerValue);
        Assert.AreEqual("X", marker.MarkerSymbol);
    }

    [TestMethod]
    public void Construct_Vote2()
    {
        var marker = Marker.Create("x");
        Assert.IsNotNull(marker);
        Assert.AreEqual(MarkerType.Vote, marker.MarkerType);
        Assert.AreEqual(100, marker.MarkerValue);
    }

    [TestMethod]
    public void Construct_Vote3()
    {
        var marker = Marker.Create("✓");
        Assert.IsNotNull(marker);
        Assert.AreEqual(MarkerType.Vote, marker.MarkerType);
        Assert.AreEqual(100, marker.MarkerValue);
    }

    [TestMethod]
    public void Construct_Vote4()
    {
        var marker = Marker.Create("✔");
        Assert.IsNotNull(marker);
        Assert.AreEqual(MarkerType.Vote, marker.MarkerType);
        Assert.AreEqual(100, marker.MarkerValue);
    }

    [TestMethod]
    public void Construct_Vote5()
    {
        var marker = Marker.Create("✗");
        Assert.IsNotNull(marker);
        Assert.AreEqual(MarkerType.Vote, marker.MarkerType);
        Assert.AreEqual(100, marker.MarkerValue);
    }

    [TestMethod]
    public void Construct_Vote6()
    {
        var marker = Marker.Create("✘");
        Assert.IsNotNull(marker);
        Assert.AreEqual(MarkerType.Vote, marker.MarkerType);
        Assert.AreEqual(100, marker.MarkerValue);
    }

    [TestMethod]
    public void Construct_Vote7()
    {
        var marker = Marker.Create("Х");
        Assert.IsNotNull(marker);
        Assert.AreEqual(MarkerType.Vote, marker.MarkerType);
        Assert.AreEqual(100, marker.MarkerValue);
    }

    [TestMethod]
    public void Construct_Vote8()
    {
        var marker = Marker.Create("☒");
        Assert.IsNotNull(marker);
        Assert.AreEqual(MarkerType.Vote, marker.MarkerType);
        Assert.AreEqual(100, marker.MarkerValue);
    }

    [TestMethod]
    public void Construct_Vote9()
    {
        var marker = Marker.Create("☑");
        Assert.IsNotNull(marker);
        Assert.AreEqual(MarkerType.Vote, marker.MarkerType);
        Assert.AreEqual(100, marker.MarkerValue);
    }

    [TestMethod]
    public void Construct_Invalid1()
    {
        var marker = Marker.Create("A");
        Assert.IsNull(marker);
    }

    [TestMethod]
    public void Construct_Invalid2()
    {
        var marker = Marker.Create("XZ");
        Assert.IsNull(marker);
    }

    [TestMethod]
    public void Construct_Invalid3()
    {
        var marker = Marker.Create("#X");
        Assert.IsNull(marker);
    }

    [TestMethod]
    public void Construct_Approval_Up()
    {
        var marker = Marker.Create("+");
        Assert.IsNotNull(marker);
        Assert.AreEqual(MarkerType.Approval, marker.MarkerType);
        Assert.AreEqual(80, marker.MarkerValue);
    }

    [TestMethod]
    public void Construct_Approval_Down()
    {
        var marker = Marker.Create("-");
        Assert.IsNotNull(marker);
        Assert.AreEqual(MarkerType.Approval, marker.MarkerType);
        Assert.AreEqual(20, marker.MarkerValue);
    }

    [TestMethod]
    public void Construct_Legacy_Rank1()
    {
        var marker = Marker.Create("1");
        Assert.IsNotNull(marker);
        Assert.AreEqual(MarkerType.Rank, marker.MarkerType);
        Assert.AreEqual(1, marker.MarkerValue);
    }

    [TestMethod]
    public void Construct_Rank1()
    {
        var marker = Marker.Create("#1");
        Assert.IsNotNull(marker);
        Assert.AreEqual(MarkerType.Rank, marker.MarkerType);
        Assert.AreEqual(1, marker.MarkerValue);
    }

    [TestMethod]
    public void Construct_Rank200()
    {
        var marker = Marker.Create("#200");
        Assert.IsNotNull(marker);
        Assert.AreEqual(MarkerType.Rank, marker.MarkerType);
        Assert.AreEqual(99, marker.MarkerValue);
    }

    [TestMethod]
    public void Construct_Score_99()
    {
        var marker = Marker.Create("99%");
        Assert.IsNotNull(marker);
        Assert.AreEqual(MarkerType.Score, marker.MarkerType);
        Assert.AreEqual(99, marker.MarkerValue);
    }

    [TestMethod]
    public void Construct_Score_110()
    {
        var marker = Marker.Create("110%");
        Assert.IsNotNull(marker);
        Assert.AreEqual(MarkerType.Score, marker.MarkerType);
        Assert.AreEqual(100, marker.MarkerValue);
    }

    [TestMethod]
    public void Compare_None()
    {
        var marker = Marker.Create("95%");
        Assert.IsNotNull(marker);

        Assert.AreEqual(marker, Marker.Empty, new MarkerComparer());
        Assert.IsTrue(MarkerComparer.AreEqual(marker, Marker.Empty));
    }

    [TestMethod]
    public void Compare_Vote_Same()
    {
        var marker1 = Marker.Create("X");
        var marker2 = Marker.Create("x");
        Assert.IsNotNull(marker1);
        Assert.IsNotNull(marker2);

        Assert.AreEqual(marker1, marker2, new MarkerComparer());
    }

    [TestMethod]
    public void Compare_Rank_Same()
    {
        var marker1 = Marker.Create("3");
        var marker2 = Marker.Create("#3");
        Assert.IsNotNull(marker1);
        Assert.IsNotNull(marker2);

        Assert.AreEqual(marker1, marker2, new MarkerComparer());
    }

    [TestMethod]
    public void Compare_Score_Same()
    {
        var marker1 = Marker.Create("90%");
        var marker2 = Marker.Create("90%");
        Assert.IsNotNull(marker1);
        Assert.IsNotNull(marker2);

        Assert.AreEqual(marker1, marker2, new MarkerComparer());
    }

    [TestMethod]
    public void Compare_Score_Lower()
    {
        var marker1 = Marker.Create("80%");
        var marker2 = Marker.Create("90%");
        Assert.IsNotNull(marker1);
        Assert.IsNotNull(marker2);

        Assert.AreEqual(-1, MarkerComparer.CompareWith(marker1, marker2));
    }

    [TestMethod]
    public void Compare_Score_Higher()
    {
        var marker1 = Marker.Create("80%");
        var marker2 = Marker.Create("70%");
        Assert.IsNotNull(marker1);
        Assert.IsNotNull(marker2);

        Assert.AreEqual(1, MarkerComparer.CompareWith(marker1, marker2));
    }

    [TestMethod]
    public void Compare_Rank_Lower()
    {
        var marker1 = Marker.Create("#1");
        var marker2 = Marker.Create("#2");
        Assert.IsNotNull(marker1);
        Assert.IsNotNull(marker2);

        Assert.AreEqual(-1, MarkerComparer.CompareWith(marker1, marker2));
    }

    [TestMethod]
    public void Compare_Rank_Higher()
    {
        var marker1 = Marker.Create("#3");
        var marker2 = Marker.Create("#2");
        Assert.IsNotNull(marker1);
        Assert.IsNotNull(marker2);

        Assert.AreEqual(1, MarkerComparer.CompareWith(marker1, marker2));
    }
}
