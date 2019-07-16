using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace NetTally.Tests.Votes
{
    [TestClass]
    public class VoteLineBlockTests
    {
        static IServiceProvider serviceProvider;

        [ClassInitialize]
        public static void ClassInit(TestContext context)
        {
            serviceProvider = TestStartup.ConfigureServices();
        }

    }
}
