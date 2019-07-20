using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NetTally.VoteCounting;
using NetTally.Votes;

namespace NetTally.Tests.Votes
{
    [TestClass]
    public class VoteLineBlockTests
    {
        #region Setup
        static IServiceProvider serviceProvider;
        static IVoteCounter voteCounter;
        static VoteConstructor voteConstructor;
        static Tally tally;
        static IQuest quest;


        [ClassInitialize]
        public static void ClassInit(TestContext context)
        {
            serviceProvider = TestStartup.ConfigureServices();

            voteCounter = serviceProvider.GetRequiredService<IVoteCounter>();
            tally = serviceProvider.GetRequiredService<Tally>();
            voteConstructor = serviceProvider.GetRequiredService<VoteConstructor>();
        }

        [TestInitialize]
        public void Initialize()
        {
            quest = new Quest();

            voteCounter.Reset();
            voteCounter.ClearPosts();
        }
        #endregion Setup

    }
}
