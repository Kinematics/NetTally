using System;
using System.Linq;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NetTally.Experiment3;
using NetTally.VoteCounting;
using NetTally.Votes;

namespace NetTally.Tests.Experiment3
{
    [TestClass]
    public class PartitioningTests
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
        #endregion


        [TestMethod]
        public void SingleLine_NoPartitioning()
        {
            string postText =
@"[X] Run, Lola, Run!";

            Post post = new Post("Kinematics", "123456", postText, 66);
            voteCounter.Quest = quest;
            quest.PartitionMode = PartitionMode.None;

            var result = voteConstructor.ProcessPostGetVotes(post, quest);

            Assert.AreNotEqual(null, result);
            Assert.AreEqual(1, result!.Count);
            Assert.AreEqual("[] Run, Lola, Run!", result[0].ToComparableString());
        }


    }
}
