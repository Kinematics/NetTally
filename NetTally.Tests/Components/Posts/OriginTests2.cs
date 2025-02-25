using System;
using System.Threading;
using Microsoft.VisualStudio.TestPlatform.PlatformAbstractions.Interfaces;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NetTally.Tally.Components.Posts;

namespace NetTally.Tests.Components.Posts;

[TestClass]
public class OriginTests2
{
    [ClassInitialize]
    public static void ClassInit(TestContext _)
    {
        TestStartup.ConfigureServices();
    }

    private static (string authorName, Author author, string threadUrl, Uri thread,
        string permalinkUrl, Uri permalink, long postIdNumber, PostId postId,
        int threadSeqNumber, PostId postNumber, DateTimeOffset timestamp)
        GetDefaults1()
    {
        string authorName = "Kinematics";
        Author author = Authors.Create(authorName);
        string threadUrl = "https://forums.sufficientvelocity.com/threads/renascence-a-homura-quest.10402/";
        Uri thread = new Uri(threadUrl);
        string permalinkUrl = "https://forums.sufficientvelocity.com/threads/renascence-a-homura-quest.10402/post-2236809";
        Uri permalink = new Uri(permalinkUrl);
        long postIdNumber = 2236809;
        PostId postId = PostIds.Create(postIdNumber);
        int threadSeqNumber = 2490;
        PostId postNumber = PostIds.Create(threadSeqNumber);
        DateTimeOffset timestamp = DateTimeOffset.Now;

        return (authorName, author, threadUrl, thread, permalinkUrl, permalink,
            postIdNumber, postId, threadSeqNumber, postNumber, timestamp);
    }

    [TestMethod]
    public void Construct_DefaultUser_NoTimestamp()
    {
        var (authorName, author, threadUrl, thread, permalinkUrl, permalink, postIdNumber,
                postId, threadSeqNumber, postNumber, timestamp) = GetDefaults1();

        var origin = Origins.CreateUser(author, thread, permalink,
            postId, postNumber);

        Assert.IsNotNull(origin);
        Assert.IsTrue(origin is UserOrigin);
        Assert.AreEqual(author, origin.Author);
        Assert.AreEqual(thread, origin.Thread);
        Assert.AreEqual(permalink, origin.Permalink);
        Assert.AreEqual(postId, origin.PostId);
        Assert.AreEqual(postNumber, origin.PostNumber);
        Assert.AreEqual(DateTimeOffset.MinValue, origin.Timestamp);
    }

    [TestMethod]
    public void Construct_DefaultUser_WithTimestamp()
    {
        var (authorName, author, threadUrl, thread, permalinkUrl, permalink, postIdNumber,
                postId, threadSeqNumber, postNumber, timestamp) = GetDefaults1();

        var origin = Origins.CreateUser(author, thread, permalink,
            postId, postNumber, timestamp);

        Assert.IsNotNull(origin);
        Assert.IsTrue(origin is UserOrigin);
        Assert.AreEqual(author, origin.Author);
        Assert.AreEqual(thread, origin.Thread);
        Assert.AreEqual(permalink, origin.Permalink);
        Assert.AreEqual(postId, origin.PostId);
        Assert.AreEqual(postNumber, origin.PostNumber);
        Assert.AreEqual(timestamp, origin.Timestamp);
    }

    [TestMethod]
    public void Construct_User_NoAuthor_Null()
    {
        var (authorName, author, threadUrl, thread, permalinkUrl, permalink, postIdNumber,
                postId, threadSeqNumber, postNumber, timestamp) = GetDefaults1();

        var origin = Origins.CreateUser(Authors.None, thread, permalink, postId, postNumber, timestamp);
        Assert.IsNull(origin);
    }

    [TestMethod]
    public void Construct_Plan()
    {
        var (authorName, author, threadUrl, thread, permalinkUrl, permalink, postIdNumber,
                postId, threadSeqNumber, postNumber, timestamp) = GetDefaults1();

        string planName = "Assault";
        Author plan = Authors.Create(planName);

        var userOrigin = Origins.CreateUser(author, thread, permalink, postId, postNumber);
        Assert.IsNotNull(userOrigin);

        var planOrigin = Origins.CreatePlan(userOrigin, plan);
        Assert.IsNotNull(planOrigin);
        Assert.IsTrue(planOrigin is PlanOrigin);

        PlanOrigin? asPlanOrigin = planOrigin as PlanOrigin;
        Assert.IsNotNull(asPlanOrigin);

        Assert.AreEqual(planName, asPlanOrigin.PlanName.Name);
    }

    [TestMethod]
    public void Construct_UserNameOnly_Default()
    {
        var (authorName, author, threadUrl, thread, permalinkUrl, permalink, postIdNumber,
                postId, threadSeqNumber, postNumber, timestamp) = GetDefaults1();

        var origin = Origins.CreateUserNameOnly(author);
        Assert.IsNotNull(origin);
        Assert.IsTrue(origin is UserOrigin);
    }

    [TestMethod]
    public void Construct_UserNameOnly_NoAuthor_Null()
    {
        var origin = Origins.CreateUserNameOnly(Authors.None);
        Assert.AreEqual(Origins.None, origin);
    }

    [TestMethod]
    public void Construct_Compare_Equal()
    {
        var (authorName, author, threadUrl, thread, permalinkUrl, permalink, postIdNumber,
            postId, threadSeqNumber, postNumber, timestamp) = GetDefaults1();

        var origin1 = Origins.CreateUser(author, thread, permalink, postId, postNumber, timestamp);
        var origin2 = Origins.CreateUser(author, thread, permalink, postId, postNumber, timestamp);
        Assert.IsNotNull(origin1);
        Assert.IsNotNull(origin2);

        Assert.AreEqual(origin1, origin2);
    }

    [TestMethod]
    public void Compare_PostsDiffer_NotEqual()
    {
        var (authorName, author, threadUrl, thread, permalinkUrl, permalink, postIdNumber,
            postId, threadSeqNumber, postNumber, timestamp) = GetDefaults1();

        PostId postId2 = PostIds.Create(postIdNumber + 1);

        var origin1 = Origins.CreateUser(author, thread, permalink, postId, postNumber, timestamp);
        var origin2 = Origins.CreateUser(author, thread, permalink, postId2, postNumber, timestamp);

        Assert.IsNotNull(origin1);
        Assert.IsNotNull(origin2);

        Assert.AreNotEqual(origin1, origin2, OriginComparer.Instance);
    }

}
