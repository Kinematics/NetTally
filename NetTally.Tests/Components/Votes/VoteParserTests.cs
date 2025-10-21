using Microsoft.VisualStudio.TestTools.UnitTesting;
using NetTally.Models.Behavior;
using NetTally.Models.Comparers;
using NetTally.Models.Defaults;
using NetTally.Models.Votes;
using NetTally.Tally.Parsing;
using static NetTally.Configure.Strings;

namespace NetTally.Tests.Components.Votes;

[TestClass]
public class VoteParserTests
{
    [ClassInitialize]
    public static void ClassInit(TestContext _)
    {
        TestStartup.ConfigureServices();
    }

    #region Non-Votes
    [TestMethod]
    [TestCategory("NonVote")]
    public void Parse_Empty_NonVote()
    {
        string text = string.Empty;

        var lines = VoteParser.ExtractVoteLines(text);

        Assert.IsNotNull(lines);
        Assert.AreEqual(0, lines.Count);
    }

    [TestMethod]
    [TestCategory("NonVote")]
    public void ParseParts_Empty_NonVote()
    {
        string text = string.Empty;

        var voteLine = VoteLineParser.ParseLineParts(text);

        Assert.IsNotNull(voteLine);
        Assert.AreEqual(VoteLine.Empty, voteLine, VoteLineComparer.Instance);
    }

    [TestMethod]
    [TestCategory("NonVote")]
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
    [TestCategory("NonVote")]
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
    [TestCategory("NonVote")]
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
    [TestCategory("NonVote")]
    public void Parse_BrokenMarkerBracketOpen_NonVote()
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
    [TestCategory("NonVote")]
    public void Parse_BrokenMarkerBracketClosed_NonVote()
    {
        string text = """
            ---[x
            ] Stuff
            """;

        var lines = VoteParser.ExtractVoteLines(text);

        Assert.IsNotNull(lines);
        Assert.AreEqual(0, lines.Count);
    }

    [TestMethod]
    [TestCategory("NonVote")]
    public void Parse_NoContent_NonVote()
    {
        string text = """
            ---[x]
            """;

        var lines = VoteParser.ExtractVoteLines(text);

        Assert.IsNotNull(lines);
        Assert.AreEqual(0, lines.Count);
    }

    [TestMethod]
    [TestCategory("NonVote")]
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
    [TestCategory("NonVote")]
    public void Parse_PostedTallyWithBBCode_NonVote()
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
    [TestCategory("NonVote")]
    public void Parse_Joke_NonVote()
    {
        string text = """
            ---[jk] Kill them all
            """;

        var lines = VoteParser.ExtractVoteLines(text);

        Assert.IsNotNull(lines);
        Assert.AreEqual(0, lines.Count);
    }
    #endregion Non-Votes

    #region Valid Votes
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
    public void Parse_Prefix_PrefixStripped()
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
    public void Parse_PrefixMultiline_FirstPrefixStripped()
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
    public void Parse_BoldNotask_BoldRetained_SpacesTrimmed()
    {
        string text = "[x] 『b』 My vote『/b』";

        var lines = VoteParser.ExtractVoteLines(text);

        Assert.IsNotNull(lines);
        Assert.AreEqual(1, lines.Count);
        Assert.AreEqual(0, lines[0].Depth);
        Assert.IsTrue(lines[0].Marker is VoteMarker);
        Assert.AreEqual(VoteTask.Empty, lines[0].Task, VoteTaskComparer.Instance);
        Assert.AreEqual("『b』 My vote『/b』", lines[0].Content.Content);
        Assert.AreEqual("My vote", lines[0].Content.CleanContent);
    }

    [TestMethod]
    public void Parse_ImproperBoldInPrefix1_BoldRemoved()
    {
        string text = "『b』[x] My vote『/b』";

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
    public void Parse_ImproperBoldInPrefix2_BoldRemoved()
    {
        string text = "--『b』[x] My vote『/b』";

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
    public void Parse_ImproperBoldInMarker_BoldRemoved()
    {
        string text = "[『b』x] My vote『/b』";

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
    public void Parse_ImproperBoldInTask_BoldRemoved()
    {
        string text = "[x][『b』one] My vote『/b』";

        var lines = VoteParser.ExtractVoteLines(text);

        Assert.IsNotNull(lines);
        Assert.AreEqual(1, lines.Count);
        Assert.AreEqual(0, lines[0].Depth);
        Assert.IsTrue(lines[0].Marker is VoteMarker);
        Assert.AreEqual("one", lines[0].Task.Name);
        Assert.AreEqual("My vote", lines[0].Content.Content);
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
    public void Parse_StrikeContent_StrikeRetained()
    {
        string text = $"[x] My {OpenStrike}vote{CloseStrike}";

        var lines = VoteParser.ExtractVoteLines(text);

        Assert.IsNotNull(lines);
        Assert.AreEqual(1, lines.Count);
        Assert.AreEqual(0, lines[0].Depth);
        Assert.IsTrue(lines[0].Marker is VoteMarker);
        Assert.AreEqual(VoteTask.Empty, lines[0].Task, VoteTaskComparer.Instance);
        Assert.AreEqual("My 『s』vote『/s』", lines[0].Content.Content);
        Assert.AreEqual("My vote", lines[0].Content.CleanContent);
    }

    [TestMethod]
    public void Parse_InitialStrikeContent_StrikeRetained()
    {
        string text = $"[x] {OpenStrike}Not{CloseStrike} Today!";

        var lines = VoteParser.ExtractVoteLines(text);

        Assert.IsNotNull(lines);
        Assert.AreEqual(1, lines.Count);
        Assert.AreEqual(0, lines[0].Depth);
        Assert.IsTrue(lines[0].Marker is VoteMarker);
        Assert.AreEqual(VoteTask.Empty, lines[0].Task, VoteTaskComparer.Instance);
        Assert.AreEqual("『s』Not『/s』 Today!", lines[0].Content.Content);
        Assert.AreEqual("Not Today!", lines[0].Content.CleanContent);
    }

    [TestMethod]
    public void Parse_StrikeContentNotClosed_ContentRemoved()
    {
        string text = $"[x] My {OpenStrike}vote";

        var lines = VoteParser.ExtractVoteLines(text);

        Assert.IsNotNull(lines);
        Assert.AreEqual(1, lines.Count);
        Assert.AreEqual(0, lines[0].Depth);
        Assert.IsTrue(lines[0].Marker is VoteMarker);
        Assert.AreEqual(VoteTask.Empty, lines[0].Task, VoteTaskComparer.Instance);
        Assert.AreEqual("My", lines[0].Content.Content);
        Assert.AreEqual("My", lines[0].Content.CleanContent);
    }

    [TestMethod]
    public void Parse_StrikeContentNewLines_AbandonStruckContent()
    {
        string text = $"[x] My {OpenStrike}vote{StrikeNewLine} for water {CloseStrike}";
        var lines = VoteParser.ExtractVoteLines(text);

        Assert.IsNotNull(lines);
        Assert.AreEqual(1, lines.Count);
        Assert.AreEqual(0, lines[0].Depth);
        Assert.IsTrue(lines[0].Marker is VoteMarker);
        Assert.AreEqual(VoteTask.Empty, lines[0].Task, VoteTaskComparer.Instance);
        Assert.AreEqual("My", lines[0].Content.Content);
        Assert.AreEqual("My", lines[0].Content.CleanContent);
    }

    [TestMethod]
    public void Parse_StrikeTask_StrikeRemoved()
    {
        string text = $"[x][{OpenStrike}vote{CloseStrike}] Stuff";

        var lines = VoteParser.ExtractVoteLines(text);

        Assert.IsNotNull(lines);
        Assert.AreEqual(1, lines.Count);
        Assert.AreEqual(0, lines[0].Depth);
        Assert.IsTrue(lines[0].Marker is VoteMarker);
        Assert.AreEqual(VoteTask.Empty, lines[0].Task, VoteTaskComparer.Instance);
        Assert.AreEqual("Stuff", lines[0].Content.Content);
        Assert.AreEqual("Stuff", lines[0].Content.CleanContent);
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
    public void Parse_MultilineVote_Complex()
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
    public void Parse_UnbalancedBBCode_StripBBCode()
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
    public void Parse_CleanColorNamed_KeepColor()
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
    public void Parse_CleanColorHex_KeepColor()
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
    public void ParseParts_EmbeddedNewLine_Normal()
    {
        // Should never be able to happen, but make sure it gets ignored.
        string text = "[x] A vote with\n a newline";

        var voteLine = VoteLineParser.ParseLineParts(text);

        Assert.IsNotNull(voteLine);
        Assert.AreNotEqual(VoteLine.Empty, voteLine, VoteLineComparer.Instance);
        Assert.AreEqual("A vote with a newline", voteLine.Content.CleanContent);
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
    public void StrikeThroughContent_KeepStrike()
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
    public void Parse_ParenTask_FindTask()
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

    [TestMethod]
    [TestCategory("Nominations")]
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
    [TestCategory("Nominations")]
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
    #endregion Valid Votes
}
