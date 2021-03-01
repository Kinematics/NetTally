using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NetTally.Forums;
using NetTally.Votes;
using NetTally.VoteCounting.RankVotes;
using NetTally.Types.Components;

namespace NetTally.Tests.Tallying
{
    [TestClass]
    public class RankedRIRTests
    {
        static IServiceProvider serviceProvider;
        static VoteConstructor voteConstructor;
        static IQuest sampleQuest;
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
