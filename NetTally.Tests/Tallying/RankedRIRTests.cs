using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NetTally.Types.Components;
using NetTally.Votes;
using System;

namespace NetTally.Tests.Tallying
{
    [TestClass]
    public class RankedRIRTests
    {
        static IServiceProvider serviceProvider = null!;
        static VoteConstructor voteConstructor = null!;
        static IQuest sampleQuest = null!;
        static readonly Origin origin = new Origin("User1", "1", 1, new Uri("http://www.example.com/"), "http://www.example.com");

        [ClassInitialize]
        public static void ClassInit(TestContext context)
        {
            serviceProvider = TestStartup.ConfigureServices();

            voteConstructor = serviceProvider.GetRequiredService<VoteConstructor>();

            sampleQuest = new Quest();
        }

    }
}
