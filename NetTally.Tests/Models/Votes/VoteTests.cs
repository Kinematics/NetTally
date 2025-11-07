using Microsoft.VisualStudio.TestTools.UnitTesting;
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

    [TestMethod]
    public void Create_Null_Null()
    {
        var vote = Vote.Create(null);
        Assert.IsNull(vote);
    }
}
