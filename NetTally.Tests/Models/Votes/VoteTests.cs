using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NetTally.Configure;
using NetTally.Models;

namespace NetTally.Tests.Models.Votes;

[TestClass]
public class VoteTests
{
    [ClassInitialize]
    public static void ClassInit(TestContext _)
    {
        TestStartup.ConfigureServices();
    }

    private static Origin? GetOrigin()
    {
        Author? author = Author.Create("Kinematics");
        var postId = PostId.Create(1234567);
        var postNumber = PostNumber.Create(10);
        var permalink = Strings.ExampleUri;
        var thread = Strings.ExampleUri;
        var source = Source.Create(thread, permalink, postId, postNumber);
        var origin = Origin.CreateUser(author, source);

        return origin;
    }

    private static Post? GetNonVotePost()
    {
        var origin = GetOrigin();
        var post = Post.Create(origin, """
            Some discussion about the current events.
            Do you think we should vote to take the credit?
            """);

        return post;
    }

    private static Post? GetVotePost()
    {
        var origin = GetOrigin();
        var post = Post.Create(origin, """
            Some discussion about the current events.
            Do you think we should vote to take the credit?
            [X] Take the credit
            """);

        return post;
    }

    [TestMethod]
    public void Create_Null_Null()
    {
        var vote = Vote.Create(null);
        Assert.IsNull(vote);
    }

    [TestMethod]
    public void Create_NoVote_Null()
    {
        var post = GetNonVotePost();

        var vote = Vote.Create(post);
        Assert.IsNull(vote);
    }

    [TestMethod]
    public void Create_WithVote_Vote()
    {
        var post = GetVotePost();

        var vote = Vote.Create(post);
        Assert.IsNotNull(vote);
        Assert.HasCount(1, vote.VoteLines);
    }

    [TestMethod]
    public void Create_Component_Nulls_Null()
    {
        var vote = Vote.Create(null, null);
        Assert.IsNull(vote);
    }

    [TestMethod]
    public void Create_Component_NullLines_Null()
    {
        var origin = GetOrigin();

        var vote = Vote.Create(origin, null);
        Assert.IsNull(vote);
    }

    [TestMethod]
    public void Create_Component_EmptyLines_Null()
    {
        var origin = GetOrigin();

        var vote = Vote.Create(origin, []);
        Assert.IsNull(vote);
    }

    [TestMethod]
    public void Create_Component_NoOrigin_Null()
    {
        var post = GetVotePost();
        var vote1 = Vote.Create(post);
        Assert.IsNotNull(vote1);
        var voteLines = vote1.VoteLines.ToList();

        var vote = Vote.Create(Origin.None, voteLines);
        Assert.IsNull(vote);
    }

    [TestMethod]
    public void Create_Component_Normal_Valid()
    {
        var origin = GetOrigin();
        var post = GetVotePost();
        var vote1 = Vote.Create(post);
        Assert.IsNotNull(vote1);
        var voteLines = vote1.VoteLines.ToList();

        var vote = Vote.Create(origin, voteLines);
        Assert.IsNotNull(vote);
        Assert.HasCount(1, vote.VoteLines);
    }

    [TestMethod]
    public void Create_ToProcess_Nulls_Null()
    {
        var vote = Vote.CreateToProcess(null);
        Assert.IsNull(vote);
    }

    [TestMethod]
    public void Create_ToProcess_Vote_Valid()
    {
        var post = GetVotePost();

        var vote = Vote.Create(post);
        var voteToProcess = Vote.CreateToProcess(vote);
        Assert.IsNotNull(voteToProcess);
        Assert.HasCount(1, voteToProcess.VoteLines);
    }
}
