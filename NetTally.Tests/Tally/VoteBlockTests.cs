using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace NetTally.Tests.Votes
{
    [TestClass]
    public class VoteBlockTests
    {
        static IServiceProvider serviceProvider;

        [ClassInitialize]
        public static void ClassInit(TestContext context)
        {
            serviceProvider = TestStartup.ConfigureServices();
        }

    }
}
