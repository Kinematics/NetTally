using System;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NetTally.Tally.Components;
using NetTally.VoteCounting;

namespace NetTally.Tests.Tallying
{
    [TestClass]
    public class RankedRIRTests
    {
        static Quest sampleQuest = null!;
        //static readonly Origin origin = new("User1", "1", 1, new Uri("http://www.example.com/"), "http://www.example.com");

        [ClassInitialize]
        public static void ClassInit(TestContext _)
        {
            var serviceProvider = TestStartup.ConfigureServices();

            sampleQuest = new Quest()
            {
                VoteCounter = serviceProvider.GetRequiredService<VoteCounter>()
            };
        }
    }
}
