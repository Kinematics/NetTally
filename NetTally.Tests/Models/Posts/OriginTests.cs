using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NetTally.Models;

namespace NetTally.Tests.Models.Posts;

#pragma warning disable IDE0059 // Unnecessary assignment of a value

[TestClass]
public class OriginTests
{
    [ClassInitialize]
    public static void ClassInit(TestContext _)
    {
        TestStartup.ConfigureServices();
    }

    private static (string authorName, Author author, string threadUrl, Uri thread,
        string permalinkUrl, Uri permalink, long postIdNumber, PostId postId,
        int threadSeqNumber, PostNumber postNumber, DateTimeOffset timestamp,
        string planName, Author plan)
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
        string planName = "Excalibur";
        Author? plan = Author.Create(planName);
        Assert.IsNotNull(plan);

        return (authorName, author, threadUrl, thread, permalinkUrl, permalink,
            postIdNumber, postId, threadSeqNumber, postNumber, timestamp, planName, plan);
    }

    private static (string authorName, Author author, string planName, Author plan)
        GetDefaultGeneral()
    {
        string authorName = "Kinematics";
        Author? author = Author.Create(authorName);
        Assert.IsNotNull(author);
        string planName = "Excalibur";
        Author? plan = Author.Create(planName);
        Assert.IsNotNull(plan);

        return (authorName, author, planName, plan);
    }

    private static (string threadUrl, Uri thread, string permalinkUrl, Uri permalink,
        long postIdNumber, PostId postId, int threadSeqNumber, PostNumber postNumber)
        GetDefaultSourceDetails()
    {
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

        return (threadUrl, thread, permalinkUrl, permalink,
            postIdNumber, postId, threadSeqNumber, postNumber);
    }


    private static Origin GetNoOrigin()
    {
        return Origin.None;
    }

    private static OriginDetail GetOriginDetail()
    {
        var defaults = GetDefaultSourceDetails();

        var details = OriginDetail.Create(defaults.thread, defaults.permalink, defaults.postId, defaults.postNumber);
        Assert.IsNotNull(details);

        return details;
    }

    private static Origin GetUserOrigin()
    {
        var defaults = GetDefaultGeneral();
        var detail = GetOriginDetail();
        var origin = Origin.CreateUser(defaults.author, detail);
        Assert.IsNotNull(origin);

        return origin;
    }

    private static Origin GetUserNameOrigin()
    {
        var defaults = GetDefaultGeneral();
        var origin = Origin.CreateUser(defaults.author);
        Assert.IsNotNull(origin);

        return origin;
    }

    private static Origin GetPlanOrigin()
    {
        var defaults = GetDefaultGeneral();
        var detail = GetOriginDetail();
        var planOrigin = Origin.CreatePlan(defaults.plan, defaults.author, detail);
        Assert.IsNotNull(planOrigin);

        return planOrigin;
    }

    private static Origin GetPlanNameOrigin()
    {
        var defaults = GetDefaultGeneral();
        var origin = Origin.CreatePlan(defaults.plan);
        Assert.IsNotNull(origin);

        return origin;
    }


    [TestMethod]
    public void Construct_UserOrigin_NullAuthor_Null()
    {
        var defaults = GetDefaultGeneral();
        var detail = GetOriginDetail();

        var origin = Origin.CreateUser(null, detail);
        Assert.IsNull(origin);
    }

    [TestMethod]
    public void Construct_UserOrigin_NullDetail_Null()
    {
        var defaults = GetDefaultGeneral();
        var detail = GetOriginDetail();

        var origin = Origin.CreateUser(defaults.author, null);
        Assert.IsNull(origin);
    }

    [TestMethod]
    public void Construct_UserOrigin_Nulls_Null()
    {
        var defaults = GetDefaultGeneral();
        var detail = GetOriginDetail();

        var origin = Origin.CreateUser(null, null);
        Assert.IsNull(origin);
    }

    [TestMethod]
    public void Construct_UserOrigin_NoAuthor_Null()
    {
        var defaults = GetDefaultGeneral();
        var detail = GetOriginDetail();

        var origin = Origin.CreateUser(Author.None, detail);
        Assert.IsNull(origin);
    }

    [TestMethod]
    public void Construct_UserOrigin_NoDetail_Null()
    {
        var defaults = GetDefaultGeneral();
        var detail = GetOriginDetail();

        var origin = Origin.CreateUser(defaults.author, OriginDetail.None);
        Assert.IsNull(origin);
    }

    [TestMethod]
    public void Construct_UserNameOrigin_NullAuthor_Null()
    {
        var origin = Origin.CreateUser(null);
        Assert.IsNull(origin);
    }

    [TestMethod]
    public void Construct_UserNameOrigin_NoAuthor_Null()
    {
        var origin = Origin.CreateUser(Author.None);
        Assert.IsNull(origin);
    }

    [TestMethod]
    public void Construct_PlanOrigin_NullPlan_Null()
    {
        var defaults = GetDefaultGeneral();
        var detail = GetOriginDetail();

        var origin = Origin.CreatePlan(null, defaults.author, detail);
        Assert.IsNull(origin);
    }

    [TestMethod]
    public void Construct_PlanOrigin_NullAuthor_Null()
    {
        var defaults = GetDefaultGeneral();
        var detail = GetOriginDetail();

        var origin = Origin.CreatePlan(defaults.author, null, detail);
        Assert.IsNull(origin);
    }

    [TestMethod]
    public void Construct_PlanOrigin_NullDetail_Null()
    {
        var defaults = GetDefaultGeneral();
        var detail = GetOriginDetail();

        var origin = Origin.CreatePlan(defaults.author, defaults.plan, null);
        Assert.IsNull(origin);
    }

    [TestMethod]
    public void Construct_PlanOrigin_Nulls_Null()
    {
        var defaults = GetDefaultGeneral();
        var detail = GetOriginDetail();

        var origin = Origin.CreatePlan(null, null, null);
        Assert.IsNull(origin);
    }

    [TestMethod]
    public void Construct_PlanOrigin_NoPlan_Null()
    {
        var defaults = GetDefaultGeneral();
        var detail = GetOriginDetail();

        var origin = Origin.CreatePlan(Author.None, defaults.author, detail);
        Assert.IsNull(origin);
    }

    [TestMethod]
    public void Construct_PlanOrigin_NoAuthor_Null()
    {
        var defaults = GetDefaultGeneral();
        var detail = GetOriginDetail();

        var origin = Origin.CreatePlan(defaults.plan, Author.None, detail);
        Assert.IsNull(origin);
    }

    [TestMethod]
    public void Construct_PlanOrigin_NoDetail_Null()
    {
        var defaults = GetDefaultGeneral();
        var detail = GetOriginDetail();

        var origin = Origin.CreatePlan(defaults.plan, defaults.author, OriginDetail.None);
        Assert.IsNull(origin);
    }

    [TestMethod]
    public void Construct_PlanNameOrigin_NullPlan_Null()
    {
        var origin = Origin.CreatePlan(null);
        Assert.IsNull(origin);
    }

    [TestMethod]
    public void Construct_PlanNameOrigin_NoPlan_Null()
    {
        var origin = Origin.CreatePlan(Author.None);
        Assert.IsNull(origin);
    }

    [TestMethod]
    public void Construct_DefaultUser_NoTimestamp()
    {
        var (authorName, author, threadUrl, thread, permalinkUrl, permalink, postIdNumber,
                postId, threadSeqNumber, postNumber, timestamp, planName, plan) = GetDefaults1();

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
    public void Construct_Plan()
    {
        var (authorName, author, threadUrl, thread, permalinkUrl, permalink, postIdNumber,
                postId, threadSeqNumber, postNumber, timestamp, planName, plan) = GetDefaults1();

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
    public void Construct_Compare_Equal()
    {
        var (authorName, author, threadUrl, thread, permalinkUrl, permalink, postIdNumber,
            postId, threadSeqNumber, postNumber, timestamp, planName, plan) = GetDefaults1();

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
            postId, threadSeqNumber, postNumber, timestamp, planName, plan) = GetDefaults1();

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
            postId, threadSeqNumber, postNumber, timestamp, planName, plan) = GetDefaults1();

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
            postId, threadSeqNumber, postNumber, timestamp, planName, plan) = GetDefaults1();

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
            postId, threadSeqNumber, postNumber, timestamp, planName, plan) = GetDefaults1();

        var details = OriginDetail.Create(thread, permalink, postId, postNumber);
        var origin1 = Origin.CreateUser(author, details);
        Assert.IsNotNull(origin1);

        var origin2 = Origin.CreatePlan(plan, author, details);
        Assert.IsNotNull(origin2);

        Assert.AreNotEqual(origin1, origin2, OriginComparer.Instance);
        Assert.AreEqual(origin1.GetDetails(), origin2.GetDetails(), OriginDetailComparer.Instance);
    }

    [TestMethod]
    public void Compare_Equal_Plans()
    {
        var (authorName, author, threadUrl, thread, permalinkUrl, permalink, postIdNumber,
            postId, threadSeqNumber, postNumber, timestamp, planName, plan) = GetDefaults1();

        var details = OriginDetail.Create(thread, permalink, postId, postNumber);
        var origin1 = Origin.CreateUser(author, details);
        Assert.IsNotNull(origin1);

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
            postId, threadSeqNumber, postNumber, timestamp, planName, plan) = GetDefaults1();

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
            postId, threadSeqNumber, postNumber, timestamp, planName, plan) = GetDefaults1();

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
            postId, threadSeqNumber, postNumber, timestamp, planName, plan) = GetDefaults1();

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
            postId, threadSeqNumber, postNumber, timestamp, planName, plan) = GetDefaults1();

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
            postId, threadSeqNumber, postNumber, timestamp, planName, plan) = GetDefaults1();

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
            postId, threadSeqNumber, postNumber, timestamp, planName, plan) = GetDefaults1();

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
            postId, threadSeqNumber, postNumber, timestamp, planName, plan) = GetDefaults1();

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
            postId, threadSeqNumber, postNumber, timestamp, planName, plan) = GetDefaults1();

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


    [TestMethod]
    public void Behavior_IsPlan_NoOrigin_No()
    {
        var origin = GetNoOrigin();

        Assert.IsFalse(origin.IsPlan);
    }

    [TestMethod]
    public void Behavior_IsPlan_UserOrigin_No()
    {
        var origin = GetUserOrigin();

        Assert.IsFalse(origin.IsPlan);
    }

    [TestMethod]
    public void Behavior_IsPlan_UserNameOrigin_No()
    {
        var origin = GetUserNameOrigin();

        Assert.IsFalse(origin.IsPlan);
    }

    [TestMethod]
    public void Behavior_IsPlan_PlanOrigin_Yes()
    {
        var origin = GetPlanOrigin();

        Assert.IsTrue(origin.IsPlan);
    }

    [TestMethod]
    public void Behavior_IsPlan_PlanNameOrigin_Yes()
    {
        var origin = GetPlanNameOrigin();

        Assert.IsTrue(origin.IsPlan);
    }

    [TestMethod]
    public void Behavior_IsUser_NoOrigin_No()
    {
        var origin = GetNoOrigin();

        Assert.IsFalse(origin.IsUser);
    }

    [TestMethod]
    public void Behavior_IsUser_UserOrigin_Yes()
    {
        var origin = GetUserOrigin();

        Assert.IsTrue(origin.IsUser);
    }

    [TestMethod]
    public void Behavior_IsUser_UserNameOrigin_Yes()
    {
        var origin = GetUserNameOrigin();

        Assert.IsTrue(origin.IsUser);
    }


    [TestMethod]
    public void Behavior_IsUser_PlanOrigin_No()
    {
        var origin = GetPlanOrigin();

        Assert.IsFalse(origin.IsUser);
    }

    [TestMethod]
    public void Behavior_IsUser_PlanNameOrigin_No()
    {
        var origin = GetPlanNameOrigin();

        Assert.IsFalse(origin.IsUser);
    }

    [TestMethod]
    public void Behavior_GetName_NoOrigin_None()
    {
        var origin = GetNoOrigin();

        Assert.AreEqual(Author.None, origin.GetName());
    }

    [TestMethod]
    public void Behavior_GetName_UserOrigin()
    {
        var defaults = GetDefaultGeneral();
        var origin = GetUserOrigin();

        Assert.AreEqual(defaults.author, origin.GetName());
    }

    [TestMethod]
    public void Behavior_GetName_UserNameOrigin()
    {
        var defaults = GetDefaultGeneral();
        var origin = GetUserNameOrigin();

        Assert.AreEqual(defaults.author, origin.GetName());
    }


    [TestMethod]
    public void Behavior_GetName_PlanOrigin()
    {
        var defaults = GetDefaultGeneral();
        var origin = GetPlanOrigin();

        Assert.AreEqual(defaults.plan, origin.GetName());
    }

    [TestMethod]
    public void Behavior_GetName_PlanNameOrigin()
    {
        var defaults = GetDefaultGeneral();
        var origin = GetPlanNameOrigin();

        Assert.AreEqual(defaults.plan, origin.GetName());
    }

    [TestMethod]
    public void Behavior_Author_NoOrigin_None()
    {
        var origin = GetNoOrigin();

        Assert.AreEqual(Author.None, origin.Author);
    }

    [TestMethod]
    public void Behavior_Author_UserOrigin()
    {
        var defaults = GetDefaultGeneral();
        var origin = GetUserOrigin();

        Assert.AreEqual(defaults.author, origin.Author);
    }

    [TestMethod]
    public void Behavior_Author_UserNameOrigin()
    {
        var defaults = GetDefaultGeneral();
        var origin = GetUserNameOrigin();

        Assert.AreEqual(defaults.author, origin.Author);
    }


    [TestMethod]
    public void Behavior_Author_PlanOrigin()
    {
        var defaults = GetDefaultGeneral();
        var origin = GetPlanOrigin();

        Assert.AreEqual(defaults.author, origin.Author);
    }

    [TestMethod]
    public void Behavior_Author_PlanNameOrigin()
    {
        var defaults = GetDefaultGeneral();
        var origin = GetPlanNameOrigin();

        Assert.AreEqual(Author.None, origin.Author);
    }

    [TestMethod]
    public void Behavior_GetDetails_NoOrigin_None()
    {
        var origin = GetNoOrigin();

        Assert.AreEqual(OriginDetail.None, origin.GetDetails());
    }

    [TestMethod]
    public void Behavior_GetDetails_UserOrigin()
    {
        var defaults = GetDefaultGeneral();
        var detail = GetOriginDetail();
        var origin = GetUserOrigin();

        Assert.AreEqual(detail, origin.GetDetails());
    }

    [TestMethod]
    public void Behavior_GetDetails_UserNameOrigin()
    {
        var defaults = GetDefaultGeneral();
        var detail = GetOriginDetail();
        var origin = GetUserNameOrigin();

        Assert.AreEqual(OriginDetail.None, origin.GetDetails());
    }


    [TestMethod]
    public void Behavior_GetDetails_PlanOrigin()
    {
        var defaults = GetDefaultGeneral();
        var detail = GetOriginDetail();
        var origin = GetPlanOrigin();

        Assert.AreEqual(detail, origin.GetDetails());
    }

    [TestMethod]
    public void Behavior_GetDetails_PlanNameOrigin()
    {
        var defaults = GetDefaultGeneral();
        var detail = GetOriginDetail();
        var origin = GetPlanNameOrigin();

        Assert.AreEqual(OriginDetail.None, origin.GetDetails());
    }

    [TestMethod]
    public void Behavior_Thread_NoOrigin_None()
    {
        var details = GetDefaultSourceDetails();
        var origin = GetNoOrigin();

        Assert.IsNull(origin.Thread);
    }

    [TestMethod]
    public void Behavior_Thread_UserOrigin()
    {
        var details = GetDefaultSourceDetails();
        var origin = GetUserOrigin();

        Assert.AreEqual(details.thread, origin.Thread);
    }

    [TestMethod]
    public void Behavior_Thread_UserNameOrigin()
    {
        var details = GetDefaultSourceDetails();
        var origin = GetUserNameOrigin();

        Assert.IsNull(origin.Thread);
    }


    [TestMethod]
    public void Behavior_Thread_PlanOrigin()
    {
        var details = GetDefaultSourceDetails();
        var origin = GetPlanOrigin();

        Assert.AreEqual(details.thread, origin.Thread);
    }

    [TestMethod]
    public void Behavior_Thread_PlanNameOrigin()
    {
        var details = GetDefaultSourceDetails();
        var origin = GetPlanNameOrigin();

        Assert.IsNull(origin.Thread);
    }

    [TestMethod]
    public void Behavior_Permalink_NoOrigin_None()
    {
        var details = GetDefaultSourceDetails();
        var origin = GetNoOrigin();

        Assert.IsNull(origin.Permalink);
    }

    [TestMethod]
    public void Behavior_Permalink_UserOrigin()
    {
        var details = GetDefaultSourceDetails();
        var origin = GetUserOrigin();

        Assert.AreEqual(details.permalink, origin.Permalink);
    }

    [TestMethod]
    public void Behavior_Permalink_UserNameOrigin()
    {
        var details = GetDefaultSourceDetails();
        var origin = GetUserNameOrigin();

        Assert.IsNull(origin.Permalink);
    }


    [TestMethod]
    public void Behavior_Permalink_PlanOrigin()
    {
        var details = GetDefaultSourceDetails();
        var origin = GetPlanOrigin();

        Assert.AreEqual(details.permalink, origin.Permalink);
    }

    [TestMethod]
    public void Behavior_Permalink_PlanNameOrigin()
    {
        var details = GetDefaultSourceDetails();
        var origin = GetPlanNameOrigin();

        Assert.IsNull(origin.Permalink);
    }

    [TestMethod]
    public void Behavior_NoOrigin_Properties()
    {
        var origin = GetNoOrigin();

        //Assert.IsFalse(origin.IsPlan);
        //Assert.IsFalse(origin.IsUser);
        //Assert.AreEqual(Author.None, origin.GetName());
        //Assert.AreEqual(Author.None, origin.Author);
        //Assert.AreEqual(OriginDetail.None, origin.GetDetails());
        //Assert.IsNull(origin.Thread);
        //Assert.IsNull(origin.Permalink);
        //Assert.AreEqual(PostId.None, origin.PostId);
        Assert.AreEqual(PostNumber.None, origin.PostNumber);
    }

    [TestMethod]
    public void Behavior_PostId_NoOrigin_None()
    {
        var origin = GetNoOrigin();

        Assert.AreEqual(PostId.None, origin.PostId);
    }

    [TestMethod]
    public void Behavior_PostId_UserOrigin()
    {
        var details = GetDefaultSourceDetails();
        var origin = GetUserOrigin();

        Assert.AreEqual(details.postId, origin.PostId);
    }

    [TestMethod]
    public void Behavior_PostId_UserNameOrigin()
    {
        var details = GetDefaultSourceDetails();
        var origin = GetUserNameOrigin();

        Assert.AreEqual(PostId.None, origin.PostId);
    }


    [TestMethod]
    public void Behavior_PostId_PlanOrigin()
    {
        var details = GetDefaultSourceDetails();
        var origin = GetPlanOrigin();

        Assert.AreEqual(details.postId, origin.PostId);
    }

    [TestMethod]
    public void Behavior_PostId_PlanNameOrigin()
    {
        var details = GetDefaultSourceDetails();
        var origin = GetPlanNameOrigin();

        Assert.AreEqual(PostId.None, origin.PostId);
    }

    [TestMethod]
    public void Behavior_PostNumber_NoOrigin_None()
    {
        var origin = GetNoOrigin();

        Assert.AreEqual(PostNumber.None, origin.PostNumber);
    }

    [TestMethod]
    public void Behavior_PostNumber_UserOrigin()
    {
        var details = GetDefaultSourceDetails();
        var origin = GetUserOrigin();

        Assert.AreEqual(details.postNumber, origin.PostNumber);
    }

    [TestMethod]
    public void Behavior_PostNumber_UserNameOrigin()
    {
        var details = GetDefaultSourceDetails();
        var origin = GetUserNameOrigin();

        Assert.AreEqual(PostNumber.None, origin.PostNumber);
    }


    [TestMethod]
    public void Behavior_PostNumber_PlanOrigin()
    {
        var details = GetDefaultSourceDetails();
        var origin = GetPlanOrigin();

        Assert.AreEqual(details.postNumber, origin.PostNumber);
    }

    [TestMethod]
    public void Behavior_PostNumber_PlanNameOrigin()
    {
        var details = GetDefaultSourceDetails();
        var origin = GetPlanNameOrigin();

        Assert.AreEqual(PostNumber.None, origin.PostNumber);
    }

    [TestMethod]
    public void Behavior_BBCodeLink_NoOrigin_None()
    {
        var origin = GetNoOrigin();

        Assert.AreEqual(string.Empty, origin.GetBBCodeLink());
    }

    [TestMethod]
    public void Behavior_BBCodeLink_UserOrigin()
    {
        var defaults = GetDefaultGeneral();
        var details = GetDefaultSourceDetails();
        var origin = GetUserOrigin();

        string expected = $"[url=\"{details.permalinkUrl}\"]{defaults.authorName}[/url]";

        Assert.AreEqual(expected, origin.GetBBCodeLink());
    }

    [TestMethod]
    public void Behavior_BBCodeLink_UserNameOrigin()
    {
        var defaults = GetDefaultGeneral();
        var details = GetDefaultSourceDetails();
        var origin = GetUserNameOrigin();

        Assert.AreEqual(string.Empty, origin.GetBBCodeLink());
    }


    [TestMethod]
    public void Behavior_BBCodeLink_PlanOrigin()
    {
        var defaults = GetDefaultGeneral();
        var details = GetDefaultSourceDetails();
        var origin = GetPlanOrigin();

        string expected = $"[url=\"{details.permalinkUrl}\"]{Configure.Strings.PlanNameMarker}{defaults.planName}[/url]";

        Assert.AreEqual(expected, origin.GetBBCodeLink());
    }

    [TestMethod]
    public void Behavior_BBCodeLink_PlanNameOrigin()
    {
        var defaults = GetDefaultGeneral();
        var details = GetDefaultSourceDetails();
        var origin = GetPlanNameOrigin();

        Assert.AreEqual(string.Empty, origin.GetBBCodeLink());
    }

}
#pragma warning restore IDE0059 // Unnecessary assignment of a value
