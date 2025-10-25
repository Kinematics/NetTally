using Microsoft.VisualStudio.TestTools.UnitTesting;
using NetTally.Configure;
using NetTally.Enums;
using NetTally.Models;

namespace NetTally.Tests.Models.Votes;

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
        var marker = Marker.None;
        Assert.IsNotNull(marker);
        Assert.IsTrue(marker is NoMarker);
    }

    [TestMethod]
    public void Construct_Null_Empty()
    {
        var marker = Marker.Create(null!);
        Assert.IsNotNull(marker);
        Assert.IsTrue(marker is NoMarker);
    }

    [TestMethod]
    public void Construct_Empty_Error()
    {
        var marker = Marker.Create("");
        Assert.IsNotNull(marker);
        Assert.IsTrue(marker is NoMarker);
    }

    [TestMethod]
    public void Construct_Whitespace_Error()
    {
        var marker = Marker.Create("   ");
        Assert.IsNotNull(marker);
        Assert.IsTrue(marker is NoMarker);
    }

    [TestMethod]
    public void Check_Plan()
    {
        var marker = Marker.PlanMarker;
        Assert.IsNotNull(marker);
        Assert.IsTrue(marker is PlanMarker);
        Assert.AreEqual(Strings.PlanNameMarker, marker.Display);
    }

    [TestMethod]
    public void Construct_Standard()
    {
        var marker = Marker.Create("X");
        Assert.IsNotNull(marker);
        Assert.IsTrue(marker is VoteMarker);
    }

    [TestMethod]
    public void Construct_Standard_Display()
    {
        var marker = Marker.Create("X");
        Assert.IsNotNull(marker);
        Assert.AreEqual("X", marker.Display);
    }

    [TestMethod]
    public void Construct_Standard_Whitespace()
    {
        var marker = Marker.Create(" X");
        Assert.IsNotNull(marker);
        Assert.IsTrue(marker is VoteMarker);
    }

    [TestMethod]
    public void Construct_Standard_Value()
    {
        var marker = Marker.Create("X");
        Assert.IsNotNull(marker);
        Assert.IsTrue(marker is VoteMarker);
        Assert.AreEqual(100, marker.Value);
    }

    [TestMethod]
    public void Construct_Standard_IsPositive()
    {
        var marker = Marker.Create(" X");
        Assert.IsNotNull(marker);
        Assert.IsTrue(marker.IsPositive);
    }

    [TestMethod]
    public void Construct_Standard_Lowercase()
    {
        var marker = Marker.Create("x");
        Assert.IsNotNull(marker);
        Assert.IsTrue(marker is VoteMarker);
        Assert.AreEqual("X", marker.Display);
    }

    [TestMethod]
    public void Construct_Standard_Checkmark()
    {
        var marker = Marker.Create("✓");
        Assert.IsNotNull(marker);
        Assert.IsTrue(marker is VoteMarker);
        Assert.AreEqual("X", marker.Display);
    }

    [TestMethod]
    public void Construct_Standard_CheckmarkBold()
    {
        var marker = Marker.Create("✔");
        Assert.IsNotNull(marker);
        Assert.IsTrue(marker is VoteMarker);
        Assert.AreEqual("X", marker.Display);
    }

    [TestMethod]
    public void Construct_Standard_StyleX()
    {
        var marker = Marker.Create("✗");
        Assert.IsNotNull(marker);
        Assert.IsTrue(marker is VoteMarker);
        Assert.AreEqual("X", marker.Display);
    }

    [TestMethod]
    public void Construct_Standard_StyleXBold()
    {
        var marker = Marker.Create("✘");
        Assert.IsNotNull(marker);
        Assert.IsTrue(marker is VoteMarker);
        Assert.AreEqual("X", marker.Display);
    }

    [TestMethod]
    public void Construct_Standard_Chi()
    {
        var marker = Marker.Create("Х");
        Assert.IsNotNull(marker);
        Assert.IsTrue(marker is VoteMarker);
        Assert.AreEqual("X", marker.Display);
    }

    [TestMethod]
    public void Construct_Standard_XBox()
    {
        var marker = Marker.Create("☒");
        Assert.IsNotNull(marker);
        Assert.IsTrue(marker is VoteMarker);
        Assert.AreEqual("X", marker.Display);
    }

    [TestMethod]
    public void Construct_Standard_Checkbox()
    {
        var marker = Marker.Create("☑");
        Assert.IsNotNull(marker);
        Assert.IsTrue(marker is VoteMarker);
        Assert.AreEqual("X", marker.Display);
    }

    [TestMethod]
    public void Construct_Invalid_SingleLetter()
    {
        var marker = Marker.Create("A");
        Assert.AreEqual(MarkerType.None, marker.Type);
    }

    [TestMethod]
    public void Construct_Invalid_MultiLetter()
    {
        var marker = Marker.Create("XZ");
        Assert.AreEqual(MarkerType.None, marker.Type);
    }

    [TestMethod]
    public void Construct_Invalid_NumberLetter()
    {
        var marker = Marker.Create("2Z");
        Assert.AreEqual(MarkerType.None, marker.Type);
    }

    [TestMethod]
    public void Construct_Invalid_LetterRank()
    {
        var marker = Marker.Create("#X");
        Assert.AreEqual(MarkerType.None, marker.Type);
    }

    [TestMethod]
    public void Construct_Invalid_MixRankScore()
    {
        var marker = Marker.Create("#19%");
        Assert.AreEqual(MarkerType.None, marker.Type);
    }

    [TestMethod]
    public void Construct_Invalid_OverflowRank()
    {
        var marker = Marker.Create("#1999");
        Assert.AreEqual(MarkerType.None, marker.Type);
    }

    [TestMethod]
    public void Construct_Approval_Up()
    {
        var marker = Marker.Create("+");

        Assert.IsTrue(marker is ApprovalMarker);
        Assert.AreEqual(MarkerType.Approval, marker.Type);
        Assert.AreEqual(80, marker.Value);
        Assert.IsTrue(marker.IsPositive);
        Assert.AreEqual("+", marker.Display);
        Assert.AreEqual("[+]", marker.BracketedDisplay);
    }

    [TestMethod]
    public void Construct_Approval_Down()
    {
        var marker = Marker.Create("-");
        Assert.AreEqual(MarkerType.Approval, marker.Type);
        Assert.AreEqual(20, marker.Value);
        Assert.IsFalse(marker.IsPositive);
        Assert.AreEqual("-", marker.Display);
        Assert.AreEqual("[-]", marker.BracketedDisplay);
    }

    [TestMethod]
    public void Construct_Rank_Legacy()
    {
        var marker = Marker.Create("1");
        Assert.AreEqual(MarkerType.Rank, marker.Type);
        Assert.AreEqual(1, marker.Value);
        Assert.IsNull(marker.IsPositive);
        Assert.AreEqual("#1", marker.Display);
        Assert.AreEqual("[#1]", marker.BracketedDisplay);
    }

    [TestMethod]
    public void Construct_Rank1()
    {
        var marker = Marker.Create("#1");
        Assert.AreEqual(MarkerType.Rank, marker.Type);
        Assert.AreEqual(1, marker.Value);
        Assert.IsNull(marker.IsPositive);
        Assert.AreEqual("#1", marker.Display);
        Assert.AreEqual("[#1]", marker.BracketedDisplay);
    }

    [TestMethod]
    public void Construct_Rank_Cap99()
    {
        var marker = Marker.Create("#200");
        Assert.AreEqual(MarkerType.Rank, marker.Type);
        Assert.AreEqual(99, marker.Value);
        Assert.IsNull(marker.IsPositive);
        Assert.AreEqual("#99", marker.Display);
        Assert.AreEqual("[#99]", marker.BracketedDisplay);
    }

    [TestMethod]
    public void Construct_Score_99()
    {
        var marker = Marker.Create("99%");
        Assert.AreEqual(MarkerType.Score, marker.Type);
        Assert.AreEqual(99, marker.Value);
        Assert.IsTrue(marker.IsPositive);
        Assert.AreEqual("99%", marker.Display);
        Assert.AreEqual("[99%]", marker.BracketedDisplay);
    }

    [TestMethod]
    public void Construct_Score_39()
    {
        var marker = Marker.Create("39%");
        Assert.AreEqual(MarkerType.Score, marker.Type);
        Assert.AreEqual(39, marker.Value);
        Assert.IsFalse(marker.IsPositive);
        Assert.AreEqual("39%", marker.Display);
        Assert.AreEqual("[39%]", marker.BracketedDisplay);
    }

    [TestMethod]
    public void Construct_Score_Cap100()
    {
        var marker = Marker.Create("110%");
        Assert.AreEqual(MarkerType.Score, marker.Type);
        Assert.AreEqual(100, marker.Value);
        Assert.IsTrue(marker.IsPositive);
        Assert.AreEqual("100%", marker.Display);
        Assert.AreEqual("[100%]", marker.BracketedDisplay);
    }

    [TestMethod]
    public void Construct_None_Default()
    {
        var marker = Marker.None;
        Assert.AreEqual(MarkerType.None, marker.Type);
        Assert.AreEqual(0, marker.Value);
        Assert.IsNull(marker.IsPositive);
        Assert.AreEqual("", marker.Display);
        Assert.AreEqual("[]", marker.BracketedDisplay);
    }

    [TestMethod]
    public void Construct_Plan_Default()
    {
        var marker = Marker.PlanMarker;
        Assert.AreEqual(MarkerType.Plan, marker.Type);
        Assert.AreEqual(0, marker.Value);
        Assert.IsNull(marker.IsPositive);
        Assert.AreEqual(Strings.PlanNameMarker, marker.Display);
        Assert.AreEqual($"[{Strings.PlanNameMarker}]", marker.BracketedDisplay);
    }

    [TestMethod]
    public void Compare_None()
    {
        var marker = Marker.Create("95%");
        Assert.IsNotNull(marker);

        Assert.AreNotEqual(Marker.None, marker);
    }

    [TestMethod]
    public void Compare_Vote_Same()
    {
        var marker1 = Marker.Create("X");
        var marker2 = Marker.Create("x");
        Assert.IsNotNull(marker1);
        Assert.IsNotNull(marker2);

        Assert.AreEqual(marker1, marker2);
    }

    [TestMethod]
    public void Compare_Rank_Same()
    {
        var marker1 = Marker.Create("3");
        var marker2 = Marker.Create("#3");
        Assert.IsNotNull(marker1);
        Assert.IsNotNull(marker2);

        Assert.AreEqual(marker1, marker2);
    }

    [TestMethod]
    public void Compare_Score_Same()
    {
        var marker1 = Marker.Create("90%");
        var marker2 = Marker.Create("90%");
        Assert.IsNotNull(marker1);
        Assert.IsNotNull(marker2);

        Assert.AreEqual(marker1, marker2);
    }

    [TestMethod]
    public void Compare_Score_Lower()
    {
        var marker1 = Marker.Create("80%");
        var marker2 = Marker.Create("90%");
        Assert.IsNotNull(marker1);
        Assert.IsNotNull(marker2);

        Assert.AreNotEqual(marker1, marker2);
        Assert.IsTrue(marker1.Value < marker2.Value);
    }

    [TestMethod]
    public void Compare_Score_Higher()
    {
        var marker1 = Marker.Create("80%");
        var marker2 = Marker.Create("70%");
        Assert.IsNotNull(marker1);
        Assert.IsNotNull(marker2);

        Assert.AreNotEqual(marker1, marker2);
        Assert.IsTrue(marker1.Value > marker2.Value);
    }

    [TestMethod]
    public void Compare_Rank_Lower()
    {
        var marker1 = Marker.Create("#1");
        var marker2 = Marker.Create("#2");
        Assert.IsNotNull(marker1);
        Assert.IsNotNull(marker2);

        Assert.AreNotEqual(marker1, marker2);
        Assert.IsTrue(marker1.Value < marker2.Value);
    }

    [TestMethod]
    public void Compare_Rank_Higher()
    {
        var marker1 = Marker.Create("#3");
        var marker2 = Marker.Create("#2");
        Assert.IsNotNull(marker1);
        Assert.IsNotNull(marker2);

        Assert.AreNotEqual(marker1, marker2);
        Assert.IsTrue(marker1.Value > marker2.Value);
    }

    [TestMethod]
    public void Comparer_EmptyMatchesAll()
    {
        var marker = Marker.Create("95%");
        Assert.IsNotNull(marker);

        Assert.AreEqual(Marker.None, marker, MarkerComparer.Instance);
    }

    [TestMethod]
    public void Comparer_PlanMatchesAll()
    {
        var marker = Marker.Create("95%");
        Assert.IsNotNull(marker);

        Assert.AreEqual(Marker.PlanMarker, marker, MarkerComparer.Instance);
    }

    [TestMethod]
    public void Comparer_Vote_Same()
    {
        var marker1 = Marker.Create("X");
        var marker2 = Marker.Create("x");
        Assert.IsNotNull(marker1);
        Assert.IsNotNull(marker2);

        Assert.AreEqual(marker1, marker2, MarkerComparer.Instance);
    }

    [TestMethod]
    public void Comparer_Rank_Same()
    {
        var marker1 = Marker.Create("3");
        var marker2 = Marker.Create("#3");
        Assert.IsNotNull(marker1);
        Assert.IsNotNull(marker2);

        Assert.AreEqual(marker1, marker2, MarkerComparer.Instance);
    }

    [TestMethod]
    public void Comparer_Score_Same()
    {
        var marker1 = Marker.Create("90%");
        var marker2 = Marker.Create("90%");
        Assert.IsNotNull(marker1);
        Assert.IsNotNull(marker2);

        Assert.AreEqual(marker1, marker2, MarkerComparer.Instance);
    }

    [TestMethod]
    public void Comparer_Score_Lower()
    {
        var marker1 = Marker.Create("80%");
        var marker2 = Marker.Create("90%");
        Assert.IsNotNull(marker1);
        Assert.IsNotNull(marker2);

        Assert.AreNotEqual(marker1, marker2, MarkerComparer.Instance);
        Assert.AreEqual(-1, MarkerComparer.Instance.Compare(marker1, marker2));
    }

    [TestMethod]
    public void Comparer_Score_Higher()
    {
        var marker1 = Marker.Create("80%");
        var marker2 = Marker.Create("70%");
        Assert.IsNotNull(marker1);
        Assert.IsNotNull(marker2);

        Assert.AreNotEqual(marker1, marker2, MarkerComparer.Instance);
        Assert.AreEqual(1, MarkerComparer.Instance.Compare(marker1, marker2));
    }

    [TestMethod]
    public void Comparer_Rank_Lower()
    {
        var marker1 = Marker.Create("#1");
        var marker2 = Marker.Create("#2");
        Assert.IsNotNull(marker1);
        Assert.IsNotNull(marker2);

        Assert.AreNotEqual(marker1, marker2, MarkerComparer.Instance);
        Assert.AreEqual(-1, MarkerComparer.Instance.Compare(marker1, marker2));
    }

    [TestMethod]
    public void Comparer_Rank_Higher()
    {
        var marker1 = Marker.Create("#3");
        var marker2 = Marker.Create("#2");
        Assert.IsNotNull(marker1);
        Assert.IsNotNull(marker2);

        Assert.AreNotEqual(marker1, marker2, MarkerComparer.Instance);
        Assert.AreEqual(1, MarkerComparer.Instance.Compare(marker1, marker2));
    }
}
