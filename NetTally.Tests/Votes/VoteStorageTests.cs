using System;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NetTally.VoteCounting;

namespace NetTally.Tests.Votes
{
    [TestClass]
    public class VoteStorageTests
    {
        #region Setup
        static IServiceProvider serviceProvider = null!;
        static IVoteCounter voteCounter = null!;

        [ClassInitialize]
        public static void ClassInit(TestContext context)
        {
            serviceProvider = TestStartup.ConfigureServices();

            voteCounter = serviceProvider.GetRequiredService<IVoteCounter>();
        }

        [TestInitialize]
        public void Initialize()
        {
            voteCounter.Reset();
            voteCounter.ResetPosts();
        }
        #endregion
    }
}
