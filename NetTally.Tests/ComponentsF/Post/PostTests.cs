using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NetTally.Tally.ComponentsF.Post;

namespace NetTally.Tests.ComponentsF.Post;
[TestClass]
public class PostTests
{
    [ClassInitialize]
    public static void ClassInit(TestContext _)
    {
        TestStartup.ConfigureServices();
    }

    [TestMethod]
    public void Construct_Null_Error()
    {
        var post = Tally.ComponentsF.Post.Post.Create(null!, null!);
        Assert.IsNull(post);
    }
}
