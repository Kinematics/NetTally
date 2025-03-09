using Microsoft.VisualStudio.TestTools.UnitTesting;
using NetTally.Tally.Components.Votes;
using NetTally.Utility;

namespace NetTally.Tests.Components.Votes;
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
        var marker = Markers.Empty;
        Assert.IsNotNull(marker);
        Assert.IsTrue(marker is NoMarker);
    }

    [TestMethod]
    public void Construct_Null_Empty()
    {
        var marker = Markers.Create(null!);
        Assert.IsNotNull(marker);
        Assert.IsTrue(marker is NoMarker);
    }

    [TestMethod]
    public void Construct_Empty_Error()
    {
        var marker = Markers.Create("");
        Assert.IsNotNull(marker);
        Assert.IsTrue(marker is NoMarker);
    }

    [TestMethod]
    public void Construct_Whitespace_Error()
    {
        var marker = Markers.Create("   ");
        Assert.IsNotNull(marker);
        Assert.IsTrue(marker is NoMarker);
    }

    [TestMethod]
    public void Check_Plan()
    {
        var marker = Markers.PlanMarker;
        Assert.IsNotNull(marker);
        Assert.IsTrue(marker is PlanMarker);
        Assert.AreEqual(Strings.PlanNameMarker, marker.Display());
    }

    [TestMethod]
    public void Construct_Standard()
    {
        var marker = Markers.Create("X");
        Assert.IsNotNull(marker);
        Assert.IsTrue(marker is VoteMarker);
    }

    [TestMethod]
    public void Construct_Standard_Display()
    {
        var marker = Markers.Create("X");
        Assert.IsNotNull(marker);
        Assert.AreEqual("X", marker.Display());
    }

    [TestMethod]
    public void Construct_Standard_Whitespace()
    {
        var marker = Markers.Create(" X");
        Assert.IsNotNull(marker);
        Assert.IsTrue(marker is VoteMarker);
    }

    [TestMethod]
    public void Construct_Standard_Value()
    {
        var marker = Markers.Create("X");
        Assert.IsNotNull(marker);
        Assert.IsTrue(marker is VoteMarker);
        Assert.AreEqual(100, marker.GetValue());
    }

    [TestMethod]
    public void Construct_Standard_IsPositive()
    {
        var marker = Markers.Create(" X");
        Assert.IsNotNull(marker);
        Assert.IsTrue(marker.IsPositive());
    }

    [TestMethod]
    public void Construct_Standard_Lowercase()
    {
        var marker = Markers.Create("x");
        Assert.IsNotNull(marker);
        Assert.IsTrue(marker is VoteMarker);
        Assert.AreEqual("X", marker.Display());
    }

    [TestMethod]
    public void Construct_Standard_Checkmark()
    {
        var marker = Markers.Create("✓");
        Assert.IsNotNull(marker);
        Assert.IsTrue(marker is VoteMarker);
        Assert.AreEqual("X", marker.Display());
    }

    [TestMethod]
    public void Construct_Standard_CheckmarkBold()
    {
        var marker = Markers.Create("✔");
        Assert.IsNotNull(marker);
        Assert.IsTrue(marker is VoteMarker);
        Assert.AreEqual("X", marker.Display());
    }

    [TestMethod]
    public void Construct_Standard_StyleX()
    {
        var marker = Markers.Create("✗");
        Assert.IsNotNull(marker);
        Assert.IsTrue(marker is VoteMarker);
        Assert.AreEqual("X", marker.Display());
    }

    [TestMethod]
    public void Construct_Standard_StyleXBold()
    {
        var marker = Markers.Create("✘");
        Assert.IsNotNull(marker);
        Assert.IsTrue(marker is VoteMarker);
        Assert.AreEqual("X", marker.Display());
    }

    [TestMethod]
    public void Construct_Standard_Chi()
    {
        var marker = Markers.Create("Х");
        Assert.IsNotNull(marker);
        Assert.IsTrue(marker is VoteMarker);
        Assert.AreEqual("X", marker.Display());
    }

    [TestMethod]
    public void Construct_Standard_XBox()
    {
        var marker = Markers.Create("☒");
        Assert.IsNotNull(marker);
        Assert.IsTrue(marker is VoteMarker);
        Assert.AreEqual("X", marker.Display());
    }

    [TestMethod]
    public void Construct_Standard_Checkbox()
    {
        var marker = Markers.Create("☑");
        Assert.IsNotNull(marker);
        Assert.IsTrue(marker is VoteMarker);
        Assert.AreEqual("X", marker.Display());
    }

    [TestMethod]
    public void Construct_Invalid_SingleLetter()
    {
        var marker = Markers.Create("A");
        Assert.IsNull(marker);
    }

    [TestMethod]
    public void Construct_Invalid_MultiLetter()
    {
        var marker = Markers.Create("XZ");
        Assert.IsNull(marker);
    }

    [TestMethod]
    public void Construct_Invalid_NumberLetter()
    {
        var marker = Markers.Create("2Z");
        Assert.IsNull(marker);
    }

    [TestMethod]
    public void Construct_Invalid_LetterRank()
    {
        var marker = Markers.Create("#X");
        Assert.IsNull(marker);
    }

    [TestMethod]
    public void Construct_Invalid_MixRankScore()
    {
        var marker = Markers.Create("#19%");
        Assert.IsNull(marker);
    }

    [TestMethod]
    public void Construct_Invalid_OverflowRank()
    {
        var marker = Markers.Create("#1999");
        Assert.IsNull(marker);
    }

    [TestMethod]
    public void Construct_Approval_Up()
    {
        var marker = Markers.Create("+");

        if (marker is ApprovalMarker approval)
        {
            Assert.IsTrue(approval.Approve);
            Assert.AreEqual("+", marker.Display());
            Assert.AreEqual(80, marker.GetValue());
            Assert.IsTrue(marker.IsPositive());
        }
        else
        {
            Assert.Fail();
        }
    }

    [TestMethod]
    public void Construct_Approval_Down()
    {
        var marker = Markers.Create("-");

        if (marker is ApprovalMarker approval)
        {
            Assert.IsFalse(approval.Approve);
            Assert.AreEqual("-", marker.Display());
            Assert.AreEqual(20, marker.GetValue());
            Assert.IsFalse(marker.IsPositive());
        }
        else
        {
            Assert.Fail();
        }
    }

    [TestMethod]
    public void Construct_Rank_Legacy()
    {
        var marker = Markers.Create("1");

        if (marker is RankMarker rank)
        {
            Assert.AreEqual(1, rank.Rank);
            Assert.AreEqual(1, marker.GetValue());
            Assert.IsNull(marker.IsPositive());
        }
        else
        {
            Assert.Fail();
        }
    }

    [TestMethod]
    public void Construct_Rank1()
    {
        var marker = Markers.Create("#1");

        if (marker is RankMarker rank)
        {
            Assert.AreEqual(1, rank.Rank);
            Assert.AreEqual(1, marker.GetValue());
            Assert.IsNull(marker.IsPositive());
        }
        else
        {
            Assert.Fail();
        }
    }

    [TestMethod]
    public void Construct_Rank_Cap99()
    {
        var marker = Markers.Create("#200");

        if (marker is RankMarker rank)
        {
            Assert.AreEqual(99, rank.Rank);
            Assert.AreEqual(99, marker.GetValue());
            Assert.IsNull(marker.IsPositive());
        }
        else
        {
            Assert.Fail();
        }
    }

    [TestMethod]
    public void Construct_Score_99()
    {
        var marker = Markers.Create("99%");

        if (marker is ScoreMarker score)
        {
            Assert.AreEqual(99, score.Score);
            Assert.AreEqual(99, marker.GetValue());
            Assert.IsTrue(marker.IsPositive());
        }
        else
        {
            Assert.Fail();
        }
    }

    [TestMethod]
    public void Construct_Score_39()
    {
        var marker = Markers.Create("39%");

        if (marker is ScoreMarker score)
        {
            Assert.AreEqual(39, score.Score);
            Assert.AreEqual(39, marker.GetValue());
            Assert.IsFalse(marker.IsPositive());
        }
        else
        {
            Assert.Fail();
        }
    }

    [TestMethod]
    public void Construct_Score_Cap100()
    {
        var marker = Markers.Create("110%");

        if (marker is ScoreMarker score)
        {
            Assert.AreEqual(100, score.Score);
            Assert.AreEqual(100, marker.GetValue());
            Assert.IsTrue(marker.IsPositive());
        }
        else
        {
            Assert.Fail();
        }
    }

    [TestMethod]
    public void Compare_None()
    {
        var marker = Markers.Create("95%");
        Assert.IsNotNull(marker);

        Assert.AreNotEqual(Markers.Empty, marker);
    }

    [TestMethod]
    public void Compare_Vote_Same()
    {
        var marker1 = Markers.Create("X");
        var marker2 = Markers.Create("x");
        Assert.IsNotNull(marker1);
        Assert.IsNotNull(marker2);

        Assert.AreEqual(marker1, marker2);
    }

    [TestMethod]
    public void Compare_Rank_Same()
    {
        var marker1 = Markers.Create("3");
        var marker2 = Markers.Create("#3");
        Assert.IsNotNull(marker1);
        Assert.IsNotNull(marker2);

        Assert.AreEqual(marker1, marker2);
    }

    [TestMethod]
    public void Compare_Score_Same()
    {
        var marker1 = Markers.Create("90%");
        var marker2 = Markers.Create("90%");
        Assert.IsNotNull(marker1);
        Assert.IsNotNull(marker2);

        Assert.AreEqual(marker1, marker2);
    }

    [TestMethod]
    public void Compare_Score_Lower()
    {
        var marker1 = Markers.Create("80%");
        var marker2 = Markers.Create("90%");
        Assert.IsNotNull(marker1);
        Assert.IsNotNull(marker2);

        Assert.AreNotEqual(marker1, marker2);
        Assert.IsTrue(marker1.GetValue() < marker2.GetValue());
    }

    [TestMethod]
    public void Compare_Score_Higher()
    {
        var marker1 = Markers.Create("80%");
        var marker2 = Markers.Create("70%");
        Assert.IsNotNull(marker1);
        Assert.IsNotNull(marker2);

        Assert.AreNotEqual(marker1, marker2);
        Assert.IsTrue(marker1.GetValue() > marker2.GetValue());
    }

    [TestMethod]
    public void Compare_Rank_Lower()
    {
        var marker1 = Markers.Create("#1");
        var marker2 = Markers.Create("#2");
        Assert.IsNotNull(marker1);
        Assert.IsNotNull(marker2);

        Assert.AreNotEqual(marker1, marker2);
        Assert.IsTrue(marker1.GetValue() < marker2.GetValue());
    }

    [TestMethod]
    public void Compare_Rank_Higher()
    {
        var marker1 = Markers.Create("#3");
        var marker2 = Markers.Create("#2");
        Assert.IsNotNull(marker1);
        Assert.IsNotNull(marker2);

        Assert.AreNotEqual(marker1, marker2);
        Assert.IsTrue(marker1.GetValue() > marker2.GetValue());
    }

    [TestMethod]
    public void Comparer_EmptyMatchesAll()
    {
        var marker = Markers.Create("95%");
        Assert.IsNotNull(marker);

        Assert.AreEqual(Markers.Empty, marker, MarkersComparer.Instance);
    }

    [TestMethod]
    public void Comparer_PlanMatchesAll()
    {
        var marker = Markers.Create("95%");
        Assert.IsNotNull(marker);

        Assert.AreEqual(Markers.PlanMarker, marker, MarkersComparer.Instance);
    }

    [TestMethod]
    public void Comparer_Vote_Same()
    {
        var marker1 = Markers.Create("X");
        var marker2 = Markers.Create("x");
        Assert.IsNotNull(marker1);
        Assert.IsNotNull(marker2);

        Assert.AreEqual(marker1, marker2, MarkersComparer.Instance);
    }

    [TestMethod]
    public void Comparer_Rank_Same()
    {
        var marker1 = Markers.Create("3");
        var marker2 = Markers.Create("#3");
        Assert.IsNotNull(marker1);
        Assert.IsNotNull(marker2);

        Assert.AreEqual(marker1, marker2, MarkersComparer.Instance);
    }

    [TestMethod]
    public void Comparer_Score_Same()
    {
        var marker1 = Markers.Create("90%");
        var marker2 = Markers.Create("90%");
        Assert.IsNotNull(marker1);
        Assert.IsNotNull(marker2);

        Assert.AreEqual(marker1, marker2, MarkersComparer.Instance);
    }

    [TestMethod]
    public void Comparer_Score_Lower()
    {
        var marker1 = Markers.Create("80%");
        var marker2 = Markers.Create("90%");
        Assert.IsNotNull(marker1);
        Assert.IsNotNull(marker2);

        Assert.AreNotEqual(marker1, marker2, MarkersComparer.Instance);
        Assert.IsTrue(MarkersComparer.Instance.Compare(marker1, marker2) == -1);
    }

    [TestMethod]
    public void Comparer_Score_Higher()
    {
        var marker1 = Markers.Create("80%");
        var marker2 = Markers.Create("70%");
        Assert.IsNotNull(marker1);
        Assert.IsNotNull(marker2);

        Assert.AreNotEqual(marker1, marker2, MarkersComparer.Instance);
        Assert.IsTrue(MarkersComparer.Instance.Compare(marker1, marker2) == 1);
    }

    [TestMethod]
    public void Comparer_Rank_Lower()
    {
        var marker1 = Markers.Create("#1");
        var marker2 = Markers.Create("#2");
        Assert.IsNotNull(marker1);
        Assert.IsNotNull(marker2);

        Assert.AreNotEqual(marker1, marker2, MarkersComparer.Instance);
        Assert.IsTrue(MarkersComparer.Instance.Compare(marker1, marker2) == -1);
    }

    [TestMethod]
    public void Comparer_Rank_Higher()
    {
        var marker1 = Markers.Create("#3");
        var marker2 = Markers.Create("#2");
        Assert.IsNotNull(marker1);
        Assert.IsNotNull(marker2);

        Assert.AreNotEqual(marker1, marker2, MarkersComparer.Instance);
        Assert.IsTrue(MarkersComparer.Instance.Compare(marker1, marker2) == 1);
    }
}
