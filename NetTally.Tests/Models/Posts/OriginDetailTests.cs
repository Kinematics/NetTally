using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NetTally.Models;

namespace NetTally.Tests.Models.Posts;

#pragma warning disable IDE0059 // Unnecessary assignment of a value

[TestClass]
public class OriginDetailTests
{
    [ClassInitialize]
    public static void ClassInit(TestContext _)
    {
        TestStartup.ConfigureServices();
    }

    private static (string threadUrl, string permalinkUrl, long postIdNumber, long threadSeqNumber)
        GetDefaults2()
    {
        string threadUrl = "https://forums.sufficientvelocity.com/threads/renascence-a-homura-quest.10402/";
        string permalinkUrl = "https://forums.sufficientvelocity.com/threads/renascence-a-homura-quest.10402/post-2236809";
        long postIdNumber = 2236809;
        int threadSeqNumber = 2490;

        return (threadUrl, permalinkUrl, postIdNumber, threadSeqNumber);
    }

    private static (string threadUrl, string permalinkUrl, long postIdNumber, long threadSeqNumber)
        GetDefaults3()
    {
        string threadUrl = "https://forums.sufficientvelocity.com/threads/renascence-a-homura-quest.10402/";
        string permalinkUrl = "https://forums.sufficientvelocity.com/threads/renascence-a-homura-quest.10402/post-2237909";
        long postIdNumber = 2237909;
        int threadSeqNumber = 2589;

        return (threadUrl, permalinkUrl, postIdNumber, threadSeqNumber);
    }

    private static (Uri thread, Uri permalink, PostId postId, PostNumber postNumber)
        ConvertToTypes((string threadUrl, string permalinkUrl, long postIdNumber, long threadSeqNumber) values)
    {
        var (threadUrl, permalinkUrl, postIdNumber, threadSeqNumber) = values;

        Uri thread = new(threadUrl);
        Uri permalink = new(permalinkUrl);
        PostId postId = new(postIdNumber);
        PostNumber postNumber = new(threadSeqNumber);

        return (thread, permalink, postId, postNumber);
    }

    [TestMethod]
    [TestCategory("Creation")]
    public void Construct_Empty_Null()
    {
        Uri? thread = null;
        Uri? permalink = null;
        PostId? postId = null;
        PostNumber? postNumber = null;

        var detail = OriginDetail.Create(thread, permalink, postId, postNumber);
        Assert.IsNull(detail);
    }

    [TestMethod]
    [TestCategory("Creation")]
    public void Construct_NoThread_Null()
    {
        var (threadUrl, permalinkUrl, postIdNumber, threadSeqNumber) = GetDefaults2();

        Uri? thread = null;
        Uri? permalink = new(permalinkUrl);
        PostId? postId = new(postIdNumber);
        PostNumber? postNumber = new(threadSeqNumber);

        var detail = OriginDetail.Create(thread, permalink, postId, postNumber);
        Assert.IsNull(detail);
    }

    [TestMethod]
    [TestCategory("Creation")]
    public void Construct_NoPermalink_Null()
    {
        var (threadUrl, permalinkUrl, postIdNumber, threadSeqNumber) = GetDefaults2();

        Uri? thread = new(threadUrl);
        Uri? permalink = null;
        PostId? postId = new(postIdNumber);
        PostNumber? postNumber = new(threadSeqNumber);

        var detail = OriginDetail.Create(thread, permalink, postId, postNumber);
        Assert.IsNull(detail);
    }

    [TestMethod]
    [TestCategory("Creation")]
    public void Construct_NoPostId_Null()
    {
        var (threadUrl, permalinkUrl, postIdNumber, threadSeqNumber) = GetDefaults2();

        Uri? thread = new(threadUrl);
        Uri? permalink = new(permalinkUrl);
        PostId? postId = null;
        PostNumber? postNumber = new(threadSeqNumber);

        var detail = OriginDetail.Create(thread, permalink, postId, postNumber);
        Assert.IsNull(detail);
    }

    [TestMethod]
    [TestCategory("Creation")]
    public void Construct_NoPostNumber_Null()
    {
        var (threadUrl, permalinkUrl, postIdNumber, threadSeqNumber) = GetDefaults2();

        Uri? thread = new(threadUrl);
        Uri? permalink = new(permalinkUrl);
        PostId? postId = new(postIdNumber);
        PostNumber? postNumber = null;

        var detail = OriginDetail.Create(thread, permalink, postId, postNumber);
        Assert.IsNull(detail);
    }

    [TestMethod]
    [TestCategory("Creation")]
    public void Construct_Defaults_Normal()
    {
        var (threadUrl, permalinkUrl, postIdNumber, threadSeqNumber) = GetDefaults2();

        Uri? thread = new(threadUrl);
        Uri? permalink = new(permalinkUrl);
        PostId? postId = new(postIdNumber);
        PostNumber? postNumber = new(threadSeqNumber);

        var detail = OriginDetail.Create(thread, permalink, postId, postNumber);
        Assert.IsNotNull(detail);
    }

    [TestMethod]
    [TestCategory("Creation")]
    public void Construct_Defaults_Values()
    {
        var (threadUrl, permalinkUrl, postIdNumber, threadSeqNumber) = GetDefaults2();

        Uri? thread = new(threadUrl);
        Uri? permalink = new(permalinkUrl);
        PostId? postId = new(postIdNumber);
        PostNumber? postNumber = new(threadSeqNumber);

        var detail = OriginDetail.Create(thread, permalink, postId, postNumber);
        Assert.IsNotNull(detail);
        var detailThread = detail.GetThread();
        Assert.IsNotNull(detailThread);
        Assert.AreEqual(threadUrl, detailThread.AbsoluteUri);
        var detailPermalink = detail.GetPermalink();
        Assert.IsNotNull(detailPermalink);
        Assert.AreEqual(permalinkUrl, detailPermalink.AbsoluteUri);
        var detailPostId = detail.GetPostId();
        Assert.AreEqual(postIdNumber, detailPostId.Value);
        var detailNumber = detail.GetPostNumber();
        Assert.AreEqual(threadSeqNumber, detailNumber.Value);
    }

    [TestMethod]
    [TestCategory("Creation")]
    public void Construct_None_Values()
    {
        var detail = OriginDetail.None;
        Assert.IsNotNull(detail);

        var detailThread = detail.GetThread();
        Assert.IsNull(detailThread);
        var detailPermalink = detail.GetPermalink();
        Assert.IsNull(detailPermalink);
        var detailPostId = detail.GetPostId();
        Assert.AreEqual(PostId.None, detailPostId);
        var detailNumber = detail.GetPostNumber();
        Assert.AreEqual(PostNumber.None, detailNumber);
    }


    [TestMethod]
    [TestCategory("Comparison")]
    public void Compare_Invalid()
    {
        var (thread, permalink, postId, postNumber) = ConvertToTypes(GetDefaults2());

        var detail1 = OriginDetail.Create(thread, permalink, postId, postNumber);
        Assert.IsNotNull(detail1);
        var detail2 = OriginDetail.None;

        Assert.AreEqual(0, OriginDetailComparer.Instance.Compare(null, null));
        Assert.AreEqual(1, OriginDetailComparer.Instance.Compare(null, detail1));
        Assert.AreEqual(1, OriginDetailComparer.Instance.Compare(null, detail2));
        Assert.AreEqual(-1, OriginDetailComparer.Instance.Compare(detail1, null));
        Assert.AreEqual(-1, OriginDetailComparer.Instance.Compare(detail2, null));
        Assert.AreEqual(0, OriginDetailComparer.Instance.Compare(detail2, detail2));
        Assert.AreEqual(-1, OriginDetailComparer.Instance.Compare(detail1, detail2));
        Assert.AreEqual(1, OriginDetailComparer.Instance.Compare(detail2, detail1));
    }

    [TestMethod]
    [TestCategory("Comparison")]
    public void Equals_Invalid()
    {
        var (thread, permalink, postId, postNumber) = ConvertToTypes(GetDefaults2());

        var detail1 = OriginDetail.Create(thread, permalink, postId, postNumber);
        Assert.IsNotNull(detail1);
        var detail2 = OriginDetail.None;

        Assert.AreEqual(null, null, OriginDetailComparer.Instance);
        Assert.AreEqual(detail2, detail2, OriginDetailComparer.Instance);
        Assert.AreEqual(detail1, detail1, OriginDetailComparer.Instance);
        Assert.AreNotEqual(null, detail2, OriginDetailComparer.Instance);
        Assert.AreNotEqual(detail2, null, OriginDetailComparer.Instance);
        Assert.AreNotEqual(null, detail1, OriginDetailComparer.Instance);
        Assert.AreNotEqual(detail1, null, OriginDetailComparer.Instance);
        Assert.AreNotEqual(detail1, detail2, OriginDetailComparer.Instance);
        Assert.AreNotEqual(detail2, detail1, OriginDetailComparer.Instance);
    }

    [TestMethod]
    [TestCategory("Comparison")]
    public void Compare_Same_Equal()
    {
        var defaults1 = ConvertToTypes(GetDefaults2());
        var defaults2 = ConvertToTypes(GetDefaults2());

        var detail1 = OriginDetail.Create(defaults1.thread, defaults1.permalink, defaults1.postId, defaults1.postNumber);
        var detail2 = OriginDetail.Create(defaults2.thread, defaults2.permalink, defaults2.postId, defaults2.postNumber);
        Assert.IsNotNull(detail1);
        Assert.IsNotNull(detail2);

        Assert.AreEqual(detail1, detail2, OriginDetailComparer.Instance);
    }

    [TestMethod]
    [TestCategory("Comparison")]
    public void Compare_Different_NotEqual()
    {
        var defaults1 = ConvertToTypes(GetDefaults2());
        var defaults2 = ConvertToTypes(GetDefaults3());

        var detail1 = OriginDetail.Create(defaults1.thread, defaults1.permalink, defaults1.postId, defaults1.postNumber);
        var detail2 = OriginDetail.Create(defaults2.thread, defaults2.permalink, defaults2.postId, defaults2.postNumber);
        Assert.IsNotNull(detail1);
        Assert.IsNotNull(detail2);

        Assert.AreNotEqual(detail1, detail2, OriginDetailComparer.Instance);
    }
}
#pragma warning restore IDE0059 // Unnecessary assignment of a value
