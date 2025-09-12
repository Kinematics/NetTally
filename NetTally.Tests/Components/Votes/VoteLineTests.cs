using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NetTally.Tally.Components.Votes;
using NetTally.Tally.Vote.Comparers;
using NetTally.Tally.Vote.Component;
using NetTally.Tally.Vote.Component.Creation;

namespace NetTally.Tests.Components.Votes;
[TestClass]
public class VoteLineTests
{
    [ClassInitialize]
    public static void ClassInit(TestContext _)
    {
        TestStartup.ConfigureServices();
    }

    [TestMethod]
    public void Construct_AllNull_Exception()
    {
        Assert.ThrowsExactly<ArgumentNullException>(
            () => VoteLine.Create(null!, null!, null!, null!));
    }

    [TestMethod]
    public void Construct_PrefixNull_Exception()
    {
        //var prefix = Prefix.Empty;
        var marker = Marker.Empty;
        var task = VoteTask.Empty;
        var content = VoteContent.Empty;

        Assert.ThrowsExactly<ArgumentNullException>(
            () => VoteLine.Create(null!, marker, task, content));
    }

    [TestMethod]
    public void Construct_MarkerNull_Exception()
    {
        var prefix = Prefix.Empty;
        //var marker = Marker.Empty;
        var task = VoteTask.Empty;
        var content = VoteContent.Empty;

        Assert.ThrowsExactly<ArgumentNullException>(
            () => VoteLine.Create(prefix, null!, task, content));
    }

    [TestMethod]
    public void Construct_TaskNull_Exception()
    {
        var prefix = Prefix.Empty;
        var marker = Marker.Empty;
        //var task = VoteTask.Empty;
        var content = VoteContent.Empty;

        Assert.ThrowsExactly<ArgumentNullException>(
            () => VoteLine.Create(prefix, marker, null!, content));
    }

    [TestMethod]
    public void Construct_ContentNull_Exception()
    {
        var prefix = Prefix.Empty;
        var marker = Marker.Empty;
        var task = VoteTask.Empty;
        //var content = VoteContent.Empty;

        Assert.ThrowsExactly<ArgumentNullException>(
            () => VoteLine.Create(prefix, marker, task, null!));
    }

    [TestMethod]
    public void Construct_ContentEmpty_Empty()
    {
        var prefix = Prefix.Empty;
        var marker = Marker.Empty;
        var task = VoteTask.Empty;
        var content = VoteContent.Empty;

        var line = VoteLine.Create(prefix, marker, task, content);
        Assert.AreEqual(VoteLine.Empty, line);
    }

    [TestMethod]
    public void Construct_Basic_Normal()
    {
        var prefix = Prefix.Empty;
        var marker = Marker.Empty;
        var task = VoteTask.Empty;
        var content = VoteContent.Create("A line of stuff");

        var line = VoteLine.Create(prefix, marker, task, content);
        Assert.IsNotNull(line);
    }

    [TestMethod]
    public void Construct_Promote_None()
    {
        var prefix = Prefix.Empty;
        var marker = Marker.Empty;
        var task = VoteTask.Empty;
        var content = VoteContent.Create("A line of stuff");

        var line = VoteLine.Create(prefix, marker, task, content);
        Assert.IsNotNull(line);
        Assert.AreEqual(0, line.Depth);
    }

    [TestMethod]
    public void Construct_Promote_One()
    {
        var prefix = Prefix.Create("-");
        var marker = Marker.Empty;
        var task = VoteTask.Empty;
        var content = VoteContent.Create("A line of stuff");

        var line = VoteLine.Create(prefix, marker, task, content);
        Assert.IsNotNull(line);
        Assert.AreEqual(1, line.Depth);
        var line2 = line.Promote();
        Assert.AreEqual(0, line2.Depth);
    }

    [TestMethod]
    public void Construct_Promote_Full()
    {
        var prefix = Prefix.Create("---");
        var marker = Marker.Empty;
        var task = VoteTask.Empty;
        var content = VoteContent.Create("A line of stuff");

        var line = VoteLine.Create(prefix, marker, task, content);
        Assert.IsNotNull(line);
        Assert.AreEqual(3, line.Depth);
        var line2 = line.FullPromote();
        Assert.AreEqual(0, line2.Depth);
    }

    [TestMethod]
    public void Construct_Compare_Same()
    {
        var prefix = Prefix.Empty;
        var marker = Marker.Empty;
        var task = VoteTask.Empty;
        var content = VoteContent.Create("A line of stuff");

        var line1 = VoteLine.Create(prefix, marker, task, content);
        Assert.IsNotNull(line1);

        var line2 = VoteLine.Create(prefix, marker, task, content);
        Assert.IsNotNull(line1);

        Assert.IsTrue(VoteLineComparer.Instance.Equals(line1, line2));
    }

    [TestMethod]
    public void Construct_CompareDiffMarkers_Same()
    {
        var prefix = Prefix.Empty;
        var marker1 = Marker.Create("X");
        var marker2 = Marker.Create("x");
        var task = VoteTask.Empty;
        var content = VoteContent.Create("A line of stuff");

        var line1 = VoteLine.Create(prefix, marker1, task, content);
        Assert.IsNotNull(line1);

        var line2 = VoteLine.Create(prefix, marker2, task, content);
        Assert.IsNotNull(line1);

        Assert.IsTrue(VoteLineComparer.Instance.Equals(line1, line2));
    }

    [TestMethod]
    public void Construct_CompareDiffMarkerTypes_Same()
    {
        var prefix = Prefix.Empty;
        var marker1 = Marker.Create("X");
        var marker2 = Marker.Create("90%");
        var task = VoteTask.Empty;
        var content = VoteContent.Create("A line of stuff");

        var line1 = VoteLine.Create(prefix, marker1, task, content);
        Assert.IsNotNull(line1);

        var line2 = VoteLine.Create(prefix, marker2, task, content);
        Assert.IsNotNull(line1);

        Assert.IsTrue(VoteLineComparer.Instance.Equals(line1, line2));
    }

    [TestMethod]
    public void Construct_CompareContent_Same()
    {
        var prefix = Prefix.Empty;
        var marker = Marker.Create("X");
        var task = VoteTask.Empty;
        var content1 = VoteContent.Create("A line of stuff");
        var content2 = VoteContent.Create("A line of stuff.");

        var line1 = VoteLine.Create(prefix, marker, task, content1);
        Assert.IsNotNull(line1);

        var line2 = VoteLine.Create(prefix, marker, task, content2);
        Assert.IsNotNull(line1);

        Assert.IsTrue(VoteLineComparer.Instance.Equals(line1, line2));
    }

    [TestMethod]
    public void Construct_CompareTaskOrder_Less()
    {
        var prefix = Prefix.Empty;
        var marker = Marker.Create("X");
        var task1 = VoteTask.Empty;
        var task2 = VoteTask.Create("Rig");
        var content1 = VoteContent.Create("A line of stuff");
        var content2 = VoteContent.Create("A line of stuff.");

        var line1 = VoteLine.Create(prefix, marker, task1, content1);
        Assert.IsNotNull(line1);

        var line2 = VoteLine.Create(prefix, marker, task2, content2);
        Assert.IsNotNull(line1);

        Assert.AreEqual(-1, VoteLineComparer.Instance.Compare(line1, line2));
    }

    [TestMethod]
    public void Construct_CompareContentOrder_Less()
    {
        var prefix = Prefix.Empty;
        var marker = Marker.Create("X");
        var task = VoteTask.Empty;
        var content1 = VoteContent.Create("Over a line of stuff");
        var content2 = VoteContent.Create("A line of stuff.");

        var line1 = VoteLine.Create(prefix, marker, task, content1);
        Assert.IsNotNull(line1);

        var line2 = VoteLine.Create(prefix, marker, task, content2);
        Assert.IsNotNull(line1);

        Assert.AreEqual(1, VoteLineComparer.Instance.Compare(line1, line2));
    }

    [TestMethod]
    public void Display_Output_SimpleLine()
    {
        string text = """
            [X] A line of stuff
            """;
        var voteLines = VoteParser.ExtractVoteLines(text);

        Assert.AreEqual(1, voteLines.Count);

        Assert.AreEqual("[X] A line of stuff", VoteLineDisplay.ToOutputString(voteLines[0]));
    }

    [TestMethod]
    public void Display_Output_RetainCheckmarkMarker()
    {
        string text = """
            [✓] A line of stuff
            """;
        var voteLines = VoteParser.ExtractVoteLines(text);

        Assert.AreEqual(1, voteLines.Count);

        Assert.AreEqual("[X] A line of stuff", VoteLineDisplay.ToOutputString(voteLines[0]));
    }

    [TestMethod]
    public void Display_Output_Prefix()
    {
        string text = """
            [X] Starter line
            --[X] A line of stuff
            """;
        var voteLines = VoteParser.ExtractVoteLines(text);

        Assert.AreEqual(2, voteLines.Count);

        Assert.AreEqual("--[X] A line of stuff", VoteLineDisplay.ToOutputString(voteLines[1]));
    }

    [TestMethod]
    public void Display_Output_AdjustSpacing()
    {
        string text = """
            [X]A line of stuff
            """;
        var voteLines = VoteParser.ExtractVoteLines(text);

        Assert.AreEqual(1, voteLines.Count);

        Assert.AreEqual("[X] A line of stuff", VoteLineDisplay.ToOutputString(voteLines[0]));
    }

    [TestMethod]
    public void Display_Output_AdjustSpacingTask()
    {
        string text = """
            [X] [Reward]A line of stuff
            """;
        var voteLines = VoteParser.ExtractVoteLines(text);

        Assert.AreEqual(1, voteLines.Count);

        Assert.AreEqual("[X][Reward] A line of stuff", VoteLineDisplay.ToOutputString(voteLines[0]));
    }

    [TestMethod]
    public void Display_Output_Rank()
    {
        string text = """
            [#7] A line of stuff
            """;
        var voteLines = VoteParser.ExtractVoteLines(text);

        Assert.AreEqual(1, voteLines.Count);

        Assert.AreEqual("[#7] A line of stuff", VoteLineDisplay.ToOutputString(voteLines[0]));
    }

    [TestMethod]
    public void Display_Output_Score()
    {
        string text = """
            [77%] A line of stuff
            """;
        var voteLines = VoteParser.ExtractVoteLines(text);

        Assert.AreEqual(1, voteLines.Count);

        Assert.AreEqual("[77%] A line of stuff", VoteLineDisplay.ToOutputString(voteLines[0]));
    }

    [TestMethod]
    public void Display_Output_Box()
    {
        string text = """
            ☒ A line of stuff
            """;
        var voteLines = VoteParser.ExtractVoteLines(text);

        Assert.AreEqual(1, voteLines.Count);

        Assert.AreEqual("[X] A line of stuff", VoteLineDisplay.ToOutputString(voteLines[0]));
    }

    [TestMethod]
    public void Display_Comparable_SimpleLine()
    {
        string text = """
            [X] A line of stuff
            """;
        var voteLines = VoteParser.ExtractVoteLines(text);

        Assert.AreEqual(1, voteLines.Count);

        Assert.AreEqual("[] A line of stuff", VoteLineDisplay.ToComparableString(voteLines[0]));
    }

    [TestMethod]
    public void Display_Comparable_Indent()
    {
        string text = """
            [X] A line of stuff
            -[X]With other stuff
            """;
        var voteLines = VoteParser.ExtractVoteLines(text);

        Assert.AreEqual(2, voteLines.Count);

        Assert.AreEqual("-[] With other stuff", VoteLineDisplay.ToComparableString(voteLines[1]));
    }

    [TestMethod]
    public void Display_Comparable_Rank()
    {
        string text = """
            [#7] A line of stuff
            """;
        var voteLines = VoteParser.ExtractVoteLines(text);

        Assert.AreEqual(1, voteLines.Count);

        Assert.AreEqual("[] A line of stuff", VoteLineDisplay.ToComparableString(voteLines[0]));
    }

    [TestMethod]
    public void Display_Comparable_SimpleLine_Task()
    {
        string text = """
            [X][Today]A line of stuff
            """;
        var voteLines = VoteParser.ExtractVoteLines(text);

        Assert.AreEqual(1, voteLines.Count);

        Assert.AreEqual("[][Today] A line of stuff", VoteLineDisplay.ToComparableString(voteLines[0]));
    }

    [TestMethod]
    public void Display_Comparable_Rank_Task()
    {
        string text = """
            [#7][Today] A line of stuff
            """;
        var voteLines = VoteParser.ExtractVoteLines(text);

        Assert.AreEqual(1, voteLines.Count);

        Assert.AreEqual("[][Today] A line of stuff", VoteLineDisplay.ToComparableString(voteLines[0]));
    }

}
