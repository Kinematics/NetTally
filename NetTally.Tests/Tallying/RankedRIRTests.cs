using System;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NetTally.Tally.Components;
using NetTally.Votes;

namespace NetTally.Tests.Tallying
{
    [TestClass]
    public class RankedRIRTests
    {
        static IServiceProvider serviceProvider = null!;
        static Quest sampleQuest = null!;
        static readonly Origin origin = new("User1", "1", 1, new Uri("http://www.example.com/"), "http://www.example.com");

        [ClassInitialize]
        public static void ClassInit(TestContext context)
        {
            serviceProvider = TestStartup.ConfigureServices();

            sampleQuest = new Quest();
        }

    }
}
