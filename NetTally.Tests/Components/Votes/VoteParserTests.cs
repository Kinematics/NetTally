using Microsoft.VisualStudio.TestTools.UnitTesting;
using NetTally.Enums;
using NetTally.Tally.ComponentsF.Votes;

namespace NetTally.Tests.Components.Votes;
[TestClass]
public class VoteParserTests
{
    [ClassInitialize]
    public static void ClassInit(TestContext _)
    {
        TestStartup.ConfigureServices();
    }

    [TestMethod]
    public void ParseEmpty_Empty()
    {
        string text = string.Empty;

        var lines = VoteParser.ExtractVoteLines(text);

        Assert.IsNotNull(lines);
        Assert.AreEqual(0, lines.Count);
    }

    [TestMethod]
    public void ParseVoteLine_One()
    {
        string text = "[x] My vote";

        var lines = VoteParser.ExtractVoteLines(text);

        Assert.IsNotNull(lines);
        Assert.AreEqual(1, lines.Count);
        Assert.AreEqual(0, lines[0].Depth);
        Assert.AreEqual(MarkerType.Vote, lines[0].Marker.MarkerType);
        Assert.IsTrue(VoteTaskComparer.Instance.Equals(lines[0].Task, VoteTask.Empty));
        Assert.AreEqual("My vote", lines[0].Content.Content);
        Assert.AreEqual("My vote", lines[0].Content.CleanContent);
    }

    [TestMethod]
    public void ParseVoteLine_CleanBold()
    {
        string text = "[x] My 『b』vote『/b』";

        var lines = VoteParser.ExtractVoteLines(text);

        Assert.IsNotNull(lines);
        Assert.AreEqual(1, lines.Count);
        Assert.AreEqual(0, lines[0].Depth);
        Assert.AreEqual(MarkerType.Vote, lines[0].Marker.MarkerType);
        Assert.IsTrue(VoteTaskComparer.Instance.Equals(lines[0].Task, VoteTask.Empty));
        Assert.AreEqual("My 『b』vote『/b』", lines[0].Content.Content);
        Assert.AreEqual("My vote", lines[0].Content.CleanContent);
    }

    [TestMethod]
    public void ParseVoteLine_Two()
    {
        string text = """
                [x] My 『b』vote『/b』
                [x] Over 『b』time『/b』
                """;

        var lines = VoteParser.ExtractVoteLines(text);

        Assert.IsNotNull(lines);
        Assert.AreEqual(2, lines.Count);
        Assert.AreEqual(0, lines[0].Depth);
        Assert.AreEqual(MarkerType.Vote, lines[0].Marker.MarkerType);
        Assert.IsTrue(VoteTaskComparer.Instance.Equals(lines[0].Task, VoteTask.Empty));
        Assert.AreEqual("My 『b』vote『/b』", lines[0].Content.Content);
        Assert.AreEqual("My vote", lines[0].Content.CleanContent);
    }

    [TestMethod]
    public void ParseVoteLine_NonVote()
    {
        string text = """
                Someone posted a tally:
                『color=Transparent』##### NetTally『/color』
                [X] A count of votes
                """;

        var lines = VoteParser.ExtractVoteLines(text);

        Assert.IsNotNull(lines);
        Assert.AreEqual(0, lines.Count);
    }

    [TestMethod]
    public void ParseVoteLine_NonVoteBold()
    {
        string text = """
                Someone posted a tally:
                『color=Transparent』#『b』####『/b』 NetTally『/color』
                [X] A count of votes
                """;

        var lines = VoteParser.ExtractVoteLines(text);

        Assert.IsNotNull(lines);
        Assert.AreEqual(0, lines.Count);
    }

    [TestMethod]
    public void ParseVoteLines_Mixed()
    {
        string text = """
                Tentative vote idea:
                [x] Line 1

                But might include something else...
                [x] Line 2
                """;

        var lines = VoteParser.ExtractVoteLines(text);

        Assert.IsNotNull(lines);
        Assert.AreEqual(2, lines.Count);
    }

    [TestMethod]
    public void ParseVoteLines_PromoteFirstLine()
    {
        string text = """
                Tentative vote idea:
                -[x] Line 1

                But might include something else...
                -[x] Line 2
                """;

        var lines = VoteParser.ExtractVoteLines(text);

        Assert.IsNotNull(lines);
        Assert.AreEqual(2, lines.Count);
        Assert.AreEqual(0, lines[0].Depth);
    }

    [TestMethod]
    public void Nomination_Fail()
    {
        string text = """
                Tentative vote idea:
                『url="https://forums.sufficientvelocity.com/members/4076/"』@Kinematics『/url』
                『url="https://forums.sufficientvelocity.com/members/4078/"』@TheInnerHollow『/url』
                """;

        var lines = VoteParser.ExtractVoteLines(text);

        Assert.IsNotNull(lines);
        Assert.AreEqual(0, lines.Count);
    }

    [TestMethod]
    public void Nomination_Pass()
    {
        string text = """
                『url="https://forums.sufficientvelocity.com/members/4076/"』@Kinematics『/url』
                『url="https://forums.sufficientvelocity.com/members/4078/"』@TheInnerHollow『/url』
                """;

        var lines = VoteParser.ExtractVoteLines(text);

        Assert.IsNotNull(lines);
        Assert.AreEqual(2, lines.Count);
    }

    [TestMethod]
    public void ParseVoteLines_Complex()
    {
        string text = """
                [90%] Line 1
                -[x] Subunit one
                --[x] Special case two
                [30%][Exception] Line 2
                -[x] Really don't care for this option
                """;

        var lines = VoteParser.ExtractVoteLines(text);

        Assert.IsNotNull(lines);
        Assert.AreEqual(5, lines.Count);
    }

    [TestMethod]
    public void ParseVoteLines_UnbalancedBBCode()
    {
        string text = """
                What do you think they'll be doing now?
                『b』[x] Ferris wheel
                -[x] At the top『/b』
                """;

        var lines = VoteParser.ExtractVoteLines(text);

        Assert.IsNotNull(lines);
        Assert.AreEqual(2, lines.Count);
        Assert.AreEqual(lines[0].Content.CleanContent, lines[0].Content.Content);
        Assert.AreEqual(lines[1].Content.CleanContent, lines[1].Content.Content);
    }

    [TestMethod]
    public void ParseVoteLine_CleanColorNamed()
    {
        string text = """
            What do you think they'll be doing now?
            [x] Ferris wheel
            [x] 『color=orange』Teacups『/color』
            """;

        var lines = VoteParser.ExtractVoteLines(text);

        Assert.IsNotNull(lines);
        Assert.AreEqual(2, lines.Count);
        Assert.AreEqual(0, lines[0].Depth);
        Assert.AreEqual(MarkerType.Vote, lines[0].Marker.MarkerType);
        Assert.IsTrue(VoteTaskComparer.Instance.Equals(lines[0].Task, VoteTask.Empty));
        Assert.AreEqual("『color=orange』Teacups『/color』", lines[1].Content.Content);
        Assert.AreEqual("Teacups", lines[1].Content.CleanContent);
    }

    [TestMethod]
    public void ParseVoteLine_CleanColorHex()
    {
        string text = """
            What do you think they'll be doing now?
            [x] Ferris wheel
            [x] 『color=#ff00AA』Teacups『/color』
            """;

        var lines = VoteParser.ExtractVoteLines(text);

        Assert.IsNotNull(lines);
        Assert.AreEqual(2, lines.Count);
        Assert.AreEqual(0, lines[0].Depth);
        Assert.AreEqual(MarkerType.Vote, lines[0].Marker.MarkerType);
        Assert.IsTrue(VoteTaskComparer.Instance.Equals(lines[0].Task, VoteTask.Empty));
        Assert.AreEqual("『color=#ff00AA』Teacups『/color』", lines[1].Content.Content);
        Assert.AreEqual("Teacups", lines[1].Content.CleanContent);
    }

    [TestMethod]
    public void StrikeThroughPlan_1()
    {
        string text = """
            Considering:
            ❰[X] Plan Triplemancer⦂-[X] Aeromancy (40%)⦂-[X] Hydromancy (30%)⦂-[X] Pyromancy (30%)❱
            """;

        var lines = VoteParser.ExtractVoteLines(text);

        Assert.IsNotNull(lines);
        Assert.AreEqual(0, lines.Count);
    }

    [TestMethod]
    public void StrikeThroughPlan_2()
    {
        string text = """
            Considering:
            ❰⦂[X] Plan Triplemancer⦂-[X] Aeromancy (40%)⦂-[X] Hydromancy (30%)⦂-[X] Pyromancy (30%)❱
            """;

        var lines = VoteParser.ExtractVoteLines(text);

        Assert.IsNotNull(lines);
        Assert.AreEqual(0, lines.Count);
    }

    [TestMethod]
    public void StrikeThroughContent_Keep()
    {
        string text = """
            Considering:
            [X] Plan Air, ❰Earth, Water,❱ Fire
            -[X] Aeromancy (40%)
            -[X] Pyromancy (25%)
            -[X] Pyromantic Divination (35%)
            """;

        var lines = VoteParser.ExtractVoteLines(text);

        Assert.IsNotNull(lines);
        Assert.AreEqual(4, lines.Count);
        Assert.AreEqual("Plan Air, 『s』Earth, Water,『/s』 Fire", lines[0].Content.Content);
        Assert.AreEqual("Plan Air, Earth, Water, Fire", lines[0].Content.CleanContent);
    }

    [TestMethod]
    public void ParseVoteLine_ParenTask()
    {
        string text = "[x](Info) My vote";

        var lines = VoteParser.ExtractVoteLines(text);
        Assert.IsNotNull(lines);

        Assert.AreEqual(1, lines.Count);
        var line = lines[0];

        Assert.AreEqual(0, line.Depth);
        Assert.IsTrue(line.HasTask);
        Assert.AreEqual("Info", line.Task.Name);
        Assert.AreEqual(MarkerType.Vote, line.Marker.MarkerType);
        Assert.AreEqual("My vote", line.Content.Content);
        Assert.AreEqual("My vote", line.Content.CleanContent);
    }

}
