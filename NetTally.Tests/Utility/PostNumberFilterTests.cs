using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace NetTally.Tests.Utility;
[TestClass]
public class PostNumberFilterTests
{
    static Quest quest = null!;

    [ClassInitialize]
    public static void ClassInit(TestContext _)
    {
        var serviceProvider = TestStartup.ConfigureServices();
        quest = TestStartup.GetExampleQuest(serviceProvider);
    }

    [TestMethod]
    public void NoFilter_AllowsAll()
    {
        quest.CustomPostFilters = "";

        Assert.IsTrue(quest.PostsFilter.Allows(1));
        Assert.IsTrue(quest.PostsFilter.Allows(123456));
        Assert.IsTrue(quest.PostsFilter.Allows(-500));
        Assert.IsFalse(quest.PostsFilter.Blocks(1));
        Assert.IsFalse(quest.PostsFilter.Blocks(123456));
        Assert.IsFalse(quest.PostsFilter.Blocks(-500));
    }

    [TestMethod]
    public void NoFilter_BlocksAll()
    {
        quest.CustomPostFilters = "!";

        Assert.IsTrue(quest.PostsFilter.Blocks(1));
        Assert.IsTrue(quest.PostsFilter.Blocks(123456));
        Assert.IsTrue(quest.PostsFilter.Blocks(-500));
    }

    [TestMethod]
    public void Filter_BasicBlocks_100()
    {
        quest.CustomPostFilters = "100";

        Assert.IsTrue(quest.PostsFilter.Allows(1));
        Assert.IsTrue(quest.PostsFilter.Blocks(100));
    }

    [TestMethod]
    public void Filter_MultipleBlocks_100()
    {
        quest.CustomPostFilters = "100,110 , 150";

        Assert.IsTrue(quest.PostsFilter.Allows(1));
        Assert.IsTrue(quest.PostsFilter.Allows(10));
        Assert.IsTrue(quest.PostsFilter.Blocks(100));
        Assert.IsTrue(quest.PostsFilter.Blocks(110));
        Assert.IsTrue(quest.PostsFilter.Blocks(150));
    }

    [TestMethod]
    public void Filter_MultipleBlocks_Spacing()
    {
        quest.CustomPostFilters = "  100,110 , 150 ";

        Assert.IsTrue(quest.PostsFilter.Allows(1));
        Assert.IsTrue(quest.PostsFilter.Allows(10));
        Assert.IsTrue(quest.PostsFilter.Blocks(100));
        Assert.IsTrue(quest.PostsFilter.Blocks(110));
        Assert.IsTrue(quest.PostsFilter.Blocks(150));
    }

    [TestMethod]
    public void Filter_MultipleBlocks_Spelling()
    {
        quest.CustomPostFilters = "  100,1l0 , 150 ";

        Assert.IsTrue(quest.PostsFilter.Allows(1));
        Assert.IsTrue(quest.PostsFilter.Allows(10));
        Assert.IsTrue(quest.PostsFilter.Blocks(100));
        Assert.IsFalse(quest.PostsFilter.Blocks(110));
        Assert.IsTrue(quest.PostsFilter.Blocks(150));
    }

    [TestMethod]
    public void Filter_Range_1()
    {
        quest.CustomPostFilters = "  100-110 , 150 ";

        Assert.IsTrue(quest.PostsFilter.Allows(1));
        Assert.IsTrue(quest.PostsFilter.Allows(10));
        Assert.IsTrue(quest.PostsFilter.Blocks(100));
        Assert.IsTrue(quest.PostsFilter.Blocks(101));
        Assert.IsTrue(quest.PostsFilter.Blocks(102));
        Assert.IsTrue(quest.PostsFilter.Blocks(103));
        Assert.IsTrue(quest.PostsFilter.Blocks(104));
        Assert.IsTrue(quest.PostsFilter.Blocks(110));
        Assert.IsTrue(quest.PostsFilter.Blocks(150));
    }

    [TestMethod]
    public void Filter_Range_2()
    {
        quest.CustomPostFilters = "  100 - 110 , 150 ";

        Assert.IsTrue(quest.PostsFilter.Allows(1));
        Assert.IsTrue(quest.PostsFilter.Allows(10));
        Assert.IsTrue(quest.PostsFilter.Blocks(100));
        Assert.IsTrue(quest.PostsFilter.Blocks(101));
        Assert.IsTrue(quest.PostsFilter.Blocks(102));
        Assert.IsTrue(quest.PostsFilter.Blocks(103));
        Assert.IsTrue(quest.PostsFilter.Blocks(104));
        Assert.IsTrue(quest.PostsFilter.Blocks(110));
        Assert.IsTrue(quest.PostsFilter.Blocks(150));
    }

    [TestMethod]
    public void Filter_Range_Unrecognized()
    {
        quest.CustomPostFilters = "  100 - 110 , 150:160 ";

        Assert.IsTrue(quest.PostsFilter.Allows(1));
        Assert.IsTrue(quest.PostsFilter.Allows(10));
        Assert.IsTrue(quest.PostsFilter.Blocks(100));
        Assert.IsTrue(quest.PostsFilter.Blocks(101));
        Assert.IsTrue(quest.PostsFilter.Blocks(102));
        Assert.IsTrue(quest.PostsFilter.Blocks(103));
        Assert.IsTrue(quest.PostsFilter.Blocks(104));
        Assert.IsTrue(quest.PostsFilter.Blocks(110));
        Assert.IsFalse(quest.PostsFilter.Blocks(150));
    }
}
