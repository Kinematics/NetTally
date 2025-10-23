using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NetTally.Models;

namespace NetTally.Tests.Components.Posts;

#pragma warning disable IDE0059 // Unnecessary assignment of a value

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
        int threadSeqNumber, PostNumber postNumber, DateTimeOffset timestamp)
        GetDefaults1()
    {
        string authorName = "Kinematics";
        Author? author = Author.Create(authorName);
        Assert.IsNotNull(author);
        string threadUrl = "https://forums.sufficientvelocity.com/threads/renascence-a-homura-quest.10402/";
        Uri thread = new(threadUrl);
        string permalinkUrl = "https://forums.sufficientvelocity.com/threads/renascence-a-homura-quest.10402/post-2236809";
        Uri permalink = new(permalinkUrl);
        long postIdNumber = 2236809;
        PostId? postId = PostId.Create(postIdNumber);
        Assert.IsNotNull(postId);
        int threadSeqNumber = 2490;
        PostNumber? postNumber = PostNumber.Create(threadSeqNumber);
        Assert.IsNotNull(postNumber);
        DateTimeOffset timestamp = DateTimeOffset.Now;

        return (authorName, author, threadUrl, thread, permalinkUrl, permalink,
            postIdNumber, postId, threadSeqNumber, postNumber, timestamp);
    }

    [TestMethod]
    public void Construct_DefaultUser_NoTimestamp()
    {
        var (authorName, author, threadUrl, thread, permalinkUrl, permalink, postIdNumber,
                postId, threadSeqNumber, postNumber, timestamp) = GetDefaults1();

        var details = OriginDetail.Create(thread, permalink, postId, postNumber);
        var origin = Origin.CreateUser(author, details);

        Assert.IsNotNull(origin);
        Assert.IsTrue(origin is UserOrigin);
        Assert.AreEqual(author, origin.GetName());
        Assert.AreEqual(thread, origin.Thread);
        Assert.AreEqual(permalink, origin.Permalink);
        Assert.AreEqual(postId, origin.PostId);
        Assert.AreEqual(postNumber, origin.PostNumber);
    }

    [TestMethod]
    public void Construct_User_NoAuthor_Null()
    {
        var (authorName, author, threadUrl, thread, permalinkUrl, permalink, postIdNumber,
                postId, threadSeqNumber, postNumber, timestamp) = GetDefaults1();

        var details = OriginDetail.Create(thread, permalink, postId, postNumber);
        var origin = Origin.CreateUser(Author.None, details);
        Assert.IsNull(origin);
    }

    [TestMethod]
    public void Construct_Plan()
    {
        var (authorName, author, threadUrl, thread, permalinkUrl, permalink, postIdNumber,
                postId, threadSeqNumber, postNumber, timestamp) = GetDefaults1();

        string planName = "Assault";
        Author? plan = Author.Create(planName);
        Assert.IsNotNull(plan);

        var details = OriginDetail.Create(thread, permalink, postId, postNumber);
        var userOrigin = Origin.CreateUser(author, details);
        Assert.IsNotNull(userOrigin);

        var planOrigin = Origin.CreatePlan(plan, author, details);
        Assert.IsNotNull(planOrigin);
        Assert.IsTrue(planOrigin is PlanOrigin);

        PlanOrigin? asPlanOrigin = planOrigin as PlanOrigin;
        Assert.IsNotNull(asPlanOrigin);

        Assert.AreEqual(planName, asPlanOrigin.PlanName.DisplayName);
    }

    [TestMethod]
    public void Construct_UserNameOnly_Default()
    {
        var (authorName, author, threadUrl, thread, permalinkUrl, permalink, postIdNumber,
                postId, threadSeqNumber, postNumber, timestamp) = GetDefaults1();

        var origin = Origin.CreateUser(author);
        Assert.IsNotNull(origin);
        Assert.IsTrue(origin is UserOrigin);
    }

    [TestMethod]
    public void Construct_UserNameOnly_NoAuthor_Null()
    {
        var origin = Origin.CreateUser(Author.None);
        Assert.IsNull(origin);
    }

    [TestMethod]
    public void Construct_Compare_Equal()
    {
        var (authorName, author, threadUrl, thread, permalinkUrl, permalink, postIdNumber,
            postId, threadSeqNumber, postNumber, timestamp) = GetDefaults1();

        var details = OriginDetail.Create(thread, permalink, postId, postNumber);
        var origin1 = Origin.CreateUser(author, details);
        var origin2 = Origin.CreateUser(author, details);
        Assert.IsNotNull(origin1);
        Assert.IsNotNull(origin2);

        Assert.AreEqual(origin1, origin2);
        Assert.AreEqual(origin1, origin2, OriginComparer.Instance);
    }

    [TestMethod]
    public void Compare_PostsDiffer_NotEqual()
    {
        var (authorName, author, threadUrl, thread, permalinkUrl, permalink, postIdNumber,
            postId, threadSeqNumber, postNumber, timestamp) = GetDefaults1();

        PostId? postId2 = PostId.Create(postIdNumber + 1);

        var details1 = OriginDetail.Create(thread, permalink, postId, postNumber);
        var details2 = OriginDetail.Create(thread, permalink, postId2, postNumber);
        var origin1 = Origin.CreateUser(author, details1);
        var origin2 = Origin.CreateUser(author, details2);

        Assert.IsNotNull(origin1);
        Assert.IsNotNull(origin2);

        Assert.AreNotEqual(origin1, origin2, OriginComparer.Instance);
    }

    [TestMethod]
    public void Construct_Compare_CapsNames_Equal()
    {
        var (authorName, author, threadUrl, thread, permalinkUrl, permalink, postIdNumber,
            postId, threadSeqNumber, postNumber, timestamp) = GetDefaults1();

        Author? author2 = Author.Create(authorName.ToUpper());
        Assert.IsNotNull(author2);

        var details = OriginDetail.Create(thread, permalink, postId, postNumber);
        var origin1 = Origin.CreateUser(author, details);
        var origin2 = Origin.CreateUser(author2, details);
        Assert.IsNotNull(origin1);
        Assert.IsNotNull(origin2);

        Assert.AreEqual(origin1, origin2, OriginComparer.Instance);
    }

    [TestMethod]
    public void Construct_Compare_DiffNames_NotEqual()
    {
        var (authorName, author, threadUrl, thread, permalinkUrl, permalink, postIdNumber,
            postId, threadSeqNumber, postNumber, timestamp) = GetDefaults1();

        Author? author2 = Author.Create("Louie");
        Assert.IsNotNull(author2);

        var details = OriginDetail.Create(thread, permalink, postId, postNumber);
        var origin1 = Origin.CreateUser(author, details);
        var origin2 = Origin.CreateUser(author2, details);
        Assert.IsNotNull(origin1);
        Assert.IsNotNull(origin2);

        Assert.AreNotEqual(origin1, origin2, OriginComparer.Instance);
    }

    [TestMethod]
    public void Compare_User_with_Plan()
    {
        var (authorName, author, threadUrl, thread, permalinkUrl, permalink, postIdNumber,
            postId, threadSeqNumber, postNumber, timestamp) = GetDefaults1();

        var details = OriginDetail.Create(thread, permalink, postId, postNumber);
        var origin1 = Origin.CreateUser(author, details);
        Assert.IsNotNull(origin1);

        Author? plan = Author.Create("Nightlife");
        var origin2 = Origin.CreatePlan(plan, author, details);
        Assert.IsNotNull(origin2);

        Assert.AreNotEqual(origin1, origin2, OriginComparer.Instance);
        Assert.AreEqual(origin1.GetDetails(), origin2.GetDetails(), OriginDetailComparer.Instance);
    }

    [TestMethod]
    public void Compare_Equal_Plans()
    {
        var (authorName, author, threadUrl, thread, permalinkUrl, permalink, postIdNumber,
            postId, threadSeqNumber, postNumber, timestamp) = GetDefaults1();

        var details = OriginDetail.Create(thread, permalink, postId, postNumber);
        var origin1 = Origin.CreateUser(author, details);
        Assert.IsNotNull(origin1);

        Author? plan = Author.Create("Nightlife");
        var origin2 = Origin.CreatePlan(plan, author, origin1.GetDetails());
        Assert.IsNotNull(origin2);
        var origin3 = Origin.CreatePlan(plan, author, origin1.GetDetails());
        Assert.IsNotNull(origin3);

        Assert.AreEqual(origin2, origin3, OriginComparer.Instance);
    }

    [TestMethod]
    public void Compare_NotEqual_Plans()
    {
        var (authorName, author, threadUrl, thread, permalinkUrl, permalink, postIdNumber,
            postId, threadSeqNumber, postNumber, timestamp) = GetDefaults1();

        var details = OriginDetail.Create(thread, permalink, postId, postNumber);
        var origin1 = Origin.CreateUser(author, details);
        Assert.IsNotNull(origin1);

        Author? plan1 = Author.Create("Nightlife");
        Author? plan2 = Author.Create("Beach Trip");
        var origin2 = Origin.CreatePlan(plan1, author, origin1.GetDetails());
        Assert.IsNotNull(origin2);
        var origin3 = Origin.CreatePlan(plan2, author, origin1.GetDetails());
        Assert.IsNotNull(origin3);

        Assert.AreNotEqual(origin2, origin3, OriginComparer.Instance);
    }

    [TestMethod]
    public void Compare_Short_Origin_Same()
    {
        var (authorName, author, threadUrl, thread, permalinkUrl, permalink, postIdNumber,
            postId, threadSeqNumber, postNumber, timestamp) = GetDefaults1();

        var details = OriginDetail.Create(thread, permalink, postId, postNumber);
        var origin1 = Origin.CreateUser(author, details);
        Assert.IsNotNull(origin1);

        var origin2 = Origin.CreateUser(author);
        Assert.IsNotNull(origin2);

        Assert.AreEqual(origin1, origin2, OriginComparer.Instance);
    }

    [TestMethod]
    public void Compare_Short_Origin_Same_Caps()
    {
        var (authorName, author, threadUrl, thread, permalinkUrl, permalink, postIdNumber,
            postId, threadSeqNumber, postNumber, timestamp) = GetDefaults1();

        var details = OriginDetail.Create(thread, permalink, postId, postNumber);
        var origin1 = Origin.CreateUser(author, details);
        Assert.IsNotNull(origin1);

        var author2 = Author.Create(authorName.ToUpper());
        var origin2 = Origin.CreateUser(author2);
        Assert.IsNotNull(origin2);

        Assert.AreEqual(origin1, origin2, OriginComparer.Instance);
    }

    [TestMethod]
    public void Compare_Short_Origin_Diff_Name()
    {
        var (authorName, author, threadUrl, thread, permalinkUrl, permalink, postIdNumber,
            postId, threadSeqNumber, postNumber, timestamp) = GetDefaults1();

        var details = OriginDetail.Create(thread, permalink, postId, postNumber);
        var origin1 = Origin.CreateUser(author, details);
        Assert.IsNotNull(origin1);

        var author2 = Author.Create(authorName + "Z");
        var origin2 = Origin.CreateUser(author2);
        Assert.IsNotNull(origin2);

        Assert.AreNotEqual(origin1, origin2, OriginComparer.Instance);
    }

    [TestMethod]
    public void Compare_Short_Origin_Plan_Name()
    {
        var (authorName, author, threadUrl, thread, permalinkUrl, permalink, postIdNumber,
            postId, threadSeqNumber, postNumber, timestamp) = GetDefaults1();

        var details = OriginDetail.Create(thread, permalink, postId, postNumber);
        var origin1 = Origin.CreateUser(author, details);
        Assert.IsNotNull(origin1);

        var origin2 = Origin.CreatePlan(author);
        Assert.IsNotNull(origin2);

        Assert.AreNotEqual(origin1, origin2, OriginComparer.Instance);
    }

    [TestMethod]
    public void Compare_Aquired_Plan_Short_Plan()
    {
        var (authorName, author, threadUrl, thread, permalinkUrl, permalink, postIdNumber,
            postId, threadSeqNumber, postNumber, timestamp) = GetDefaults1();

        var details = OriginDetail.Create(thread, permalink, postId, postNumber);
        var origin1 = Origin.CreateUser(author, details);
        Assert.IsNotNull(origin1);

        Author? plan1 = Author.Create("Nightlife");
        var origin2 = Origin.CreatePlan(plan1, author, origin1.GetDetails());
        Assert.IsNotNull(origin2);

        var origin3 = Origin.CreatePlan(plan1);
        Assert.IsNotNull(origin3);

        Assert.AreEqual(origin2, origin3, OriginComparer.Instance);
    }

    [TestMethod]
    public void Compare_Aquired_Plan_Punc()
    {
        var (authorName, author, threadUrl, thread, permalinkUrl, permalink, postIdNumber,
            postId, threadSeqNumber, postNumber, timestamp) = GetDefaults1();

        var details = OriginDetail.Create(thread, permalink, postId, postNumber);
        var origin1 = Origin.CreateUser(author, details);
        Assert.IsNotNull(origin1);

        Author? plan1 = Author.Create("Nightlife");
        var origin2 = Origin.CreatePlan(plan1, author, origin1.GetDetails());
        Assert.IsNotNull(origin2);

        Author? plan2 = Author.Create("Nightlife~!");
        var origin3 = Origin.CreatePlan(plan2);
        Assert.IsNotNull(origin3);

        Assert.AreNotEqual(origin2, origin3, OriginComparer.Instance);
    }

    [TestMethod]
    public void Compare_Diff_Source()
    {
        var (authorName, author, threadUrl, thread, permalinkUrl, permalink, postIdNumber,
            postId, threadSeqNumber, postNumber, timestamp) = GetDefaults1();

        var details1 = OriginDetail.Create(thread, permalink, postId, postNumber);
        var origin1 = Origin.CreateUser(author, details1);
        Assert.IsNotNull(origin1);

        var threadUrl2 = "https://forums.spacebattles.com/threads/renascence-a-homura-quest.10402/";
        var thread2 = new Uri(threadUrl2);
        var permalinkUrl2 = "https://forums.spacebattles.com/threads/renascence-a-homura-quest.10402/post-2236809";
        var permalink2 = new Uri(permalinkUrl2);

        var details2 = OriginDetail.Create(thread2, permalink2, postId, postNumber);
        var origin2 = Origin.CreateUser(author, details2);
        Assert.IsNotNull(origin2);

        Assert.AreNotEqual(origin1, origin2, OriginComparer.Instance);
    }

}
#pragma warning restore IDE0059 // Unnecessary assignment of a value
