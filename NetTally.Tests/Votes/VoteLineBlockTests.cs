using System;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NetTally.VoteCounting;

namespace NetTally.Tests.Votes
{
    [TestClass]
    public class VoteLineBlockTests
    {
        #region Setup
        static IServiceProvider serviceProvider = null!;
        static IVoteCounter voteCounter = null!;
        static Quest quest = null!;

        [ClassInitialize]
        public static void ClassInit(TestContext _)
        {
            serviceProvider = TestStartup.ConfigureServices();

            voteCounter = serviceProvider.GetRequiredService<IVoteCounter>();
        }

        [TestInitialize]
        public void Initialize()
        {
            quest = new Quest();

            voteCounter.Reset();
            voteCounter.ResetPosts();
        }
        #endregion Setup

    }
}
