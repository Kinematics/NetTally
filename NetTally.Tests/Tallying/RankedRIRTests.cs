using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NetTally.Tally.Components;

namespace NetTally.Tests.Tallying
{
    [TestClass]
    public class RankedRIRTests
    {
        static IServiceProvider serviceProvider = null!;
        static Quest sampleQuest = null!;
        static readonly Origin origin = new("User1", "1", 1, new Uri("http://www.example.com/"), "http://www.example.com");

        [ClassInitialize]
        public static void ClassInit(TestContext _)
        {
            serviceProvider = TestStartup.ConfigureServices();

            sampleQuest = new Quest();
        }

    }
}
