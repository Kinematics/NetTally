using Microsoft.VisualStudio.TestTools.UnitTesting;
using NetTally.Tally.Parsing;
using NetTally.Tally.Vote.Comparers;
using NetTally.Tally.Vote.Component;
using NetTally.Tally.Vote.Component.Creation;

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
    public void Parse_Empty_NonVote()
    {
        string text = string.Empty;

        var lines = VoteParser.ExtractVoteLines(text);

        Assert.IsNotNull(lines);
        Assert.AreEqual(0, lines.Count);
    }

    [TestMethod]
    public void Parse_Text_NonVote()
    {
        string text = """
            Some text on a line.
            """;

        var lines = VoteParser.ExtractVoteLines(text);

        Assert.IsNotNull(lines);
        Assert.AreEqual(0, lines.Count);
    }

    [TestMethod]
    public void Parse_EmptyLine_NonVote()
    {
        string text = """
            Some text on a line.

            Some more text.
            """;

        var lines = VoteParser.ExtractVoteLines(text);

        Assert.IsNotNull(lines);
        Assert.AreEqual(0, lines.Count);
    }

    [TestMethod]
    public void Parse_Dashes_NonVote()
    {
        string text = """
            --- Stuff
            """;

        var lines = VoteParser.ExtractVoteLines(text);

        Assert.IsNotNull(lines);
        Assert.AreEqual(0, lines.Count);
    }

    [TestMethod]
    public void Parse_BrokenMarkerBracket_NonVote()
    {
        string text = """
            ---[
            x] Stuff
            """;

        var lines = VoteParser.ExtractVoteLines(text);

        Assert.IsNotNull(lines);
        Assert.AreEqual(0, lines.Count);
    }

    [TestMethod]
    public void Parse_Prefix_FirstLineStripped()
    {
        string text = """
            - - [x] What I want to vote for
            """;

        var lines = VoteParser.ExtractVoteLines(text);

        Assert.IsNotNull(lines);
        Assert.AreEqual(1, lines.Count);
        Assert.AreEqual(0, lines[0].Depth);
    }

    [TestMethod]
    public void Parse_PrefixMultiline_OnlyFirstLineStripped()
    {
        string text = """
            --[x] What I want to vote for
            --[x] What else I want to vote for
            """;

        var lines = VoteParser.ExtractVoteLines(text);

        Assert.IsNotNull(lines);
        Assert.AreEqual(2, lines.Count);
        Assert.AreEqual(0, lines[0].Depth);
        Assert.AreEqual(2, lines[1].Depth);
    }

    [TestMethod]
    public void Parse_PrefixWithWhitespace_WhitespaceIgnored()
    {
        string text = """
            - - [x] What I want to vote for
            - - [x] What else I want to vote for
            """;

        var lines = VoteParser.ExtractVoteLines(text);

        Assert.IsNotNull(lines);
        Assert.AreEqual(2, lines.Count);
        Assert.AreEqual(0, lines[0].Depth);
        Assert.AreEqual(2, lines[1].Depth);
    }

    [TestMethod]
    public void Parse_PrefixWithEarlyWhitespace_WhitespaceIgnored()
    {
        string text = """
            - - [x] What I want to vote for
             - - [x] What else I want to vote for
            """;

        var lines = VoteParser.ExtractVoteLines(text);

        Assert.IsNotNull(lines);
        Assert.AreEqual(2, lines.Count);
        Assert.AreEqual(0, lines[0].Depth);
        Assert.AreEqual(2, lines[1].Depth);
    }

    [TestMethod]
    public void Parse_Basic_VoteLine()
    {
        string text = "[x] My vote";

        var lines = VoteParser.ExtractVoteLines(text);

        Assert.IsNotNull(lines);
        Assert.AreEqual(1, lines.Count);
        Assert.AreEqual(0, lines[0].Depth);
        Assert.IsTrue(lines[0].Marker is VoteMarker);
        Assert.AreEqual(VoteTask.Empty, lines[0].Task, VoteTaskComparer.Instance);
        Assert.AreEqual("My vote", lines[0].Content.Content);
        Assert.AreEqual("My vote", lines[0].Content.CleanContent);
    }

    [TestMethod]
    public void Parse_WhitespaceInMarker_VoteLine()
    {
        string text = "[ x ] My vote";

        var lines = VoteParser.ExtractVoteLines(text);

        Assert.IsNotNull(lines);
        Assert.AreEqual(1, lines.Count);
        Assert.AreEqual(0, lines[0].Depth);
        Assert.IsTrue(lines[0].Marker is VoteMarker);
        Assert.AreEqual(VoteTask.Empty, lines[0].Task, VoteTaskComparer.Instance);
        Assert.AreEqual("My vote", lines[0].Content.Content);
        Assert.AreEqual("My vote", lines[0].Content.CleanContent);
    }

    [TestMethod]
    public void Parse_BoxMarker_VoteLine()
    {
        string text = "☒ My vote";

        var lines = VoteParser.ExtractVoteLines(text);

        Assert.IsNotNull(lines);
        Assert.AreEqual(1, lines.Count);
        Assert.AreEqual(0, lines[0].Depth);
        Assert.IsTrue(lines[0].Marker is VoteMarker);
        Assert.AreEqual(VoteTask.Empty, lines[0].Task, VoteTaskComparer.Instance);
        Assert.AreEqual("My vote", lines[0].Content.Content);
        Assert.AreEqual("My vote", lines[0].Content.CleanContent);
    }

    [TestMethod]
    public void Parse_BoxMarkerWithPrefix_VoteLine()
    {
        string text = "--☒ My vote";

        var lines = VoteParser.ExtractVoteLines(text);

        Assert.IsNotNull(lines);
        Assert.AreEqual(1, lines.Count);
        Assert.AreEqual(0, lines[0].Depth);
        Assert.IsTrue(lines[0].Marker is VoteMarker);
        Assert.AreEqual(VoteTask.Empty, lines[0].Task, VoteTaskComparer.Instance);
        Assert.AreEqual("My vote", lines[0].Content.Content);
        Assert.AreEqual("My vote", lines[0].Content.CleanContent);
    }

    [TestMethod]
    public void Parse_CleanBold_BoldRetained()
    {
        string text = "[x] My 『b』vote『/b』";

        var lines = VoteParser.ExtractVoteLines(text);

        Assert.IsNotNull(lines);
        Assert.AreEqual(1, lines.Count);
        Assert.AreEqual(0, lines[0].Depth);
        Assert.IsTrue(lines[0].Marker is VoteMarker);
        Assert.AreEqual(VoteTask.Empty, lines[0].Task, VoteTaskComparer.Instance);
        Assert.AreEqual("My 『b』vote『/b』", lines[0].Content.Content);
        Assert.AreEqual("My vote", lines[0].Content.CleanContent);
    }

    [TestMethod]
    public void Parse_MultipleBold_BoldRetained()
    {
        string text = """
                [x] My 『b』vote『/b』
                [x] Over 『b』time『/b』
                """;

        var lines = VoteParser.ExtractVoteLines(text);

        Assert.IsNotNull(lines);
        Assert.AreEqual(2, lines.Count);
        Assert.AreEqual(0, lines[0].Depth);
        Assert.IsTrue(lines[0].Marker is VoteMarker);
        Assert.AreEqual(VoteTask.Empty, lines[0].Task, VoteTaskComparer.Instance);
        Assert.AreEqual("My 『b』vote『/b』", lines[0].Content.Content);
        Assert.AreEqual("My vote", lines[0].Content.CleanContent);
    }

    [TestMethod]
    public void Parse_PostedTally_NonVote()
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
    public void Parse_PostedTallyWithBold_NonVote()
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
    public void Parse_TextMixedWithVotes_OnlyKeepVotes()
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
    public void Parse_TextMixedWithVotesPrefix_PromoteFirstLine()
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
        Assert.IsTrue(lines[0].Marker is VoteMarker);
        Assert.AreEqual(VoteTask.Empty, lines[0].Task, VoteTaskComparer.Instance);
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
        Assert.IsTrue(lines[0].Marker is VoteMarker);
        Assert.AreEqual(VoteTask.Empty, lines[0].Task, VoteTaskComparer.Instance);
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
        Assert.IsTrue(line.Marker is VoteMarker);
        Assert.AreEqual("My vote", line.Content.Content);
        Assert.AreEqual("My vote", line.Content.CleanContent);
    }

}
