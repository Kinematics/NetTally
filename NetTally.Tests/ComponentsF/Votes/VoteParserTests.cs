using Microsoft.VisualStudio.TestTools.UnitTesting;
using NetTally.Enums;
using NetTally.Tally.ComponentsF.Votes;

namespace NetTally.Tests.ComponentsF.Votes;
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
}
