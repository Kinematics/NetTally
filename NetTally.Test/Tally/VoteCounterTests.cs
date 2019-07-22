using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NetTally.Forums;
using NetTally.VoteCounting;
using NetTally.Votes;

namespace NetTally.Tests.Votes
{
    [TestClass]
    public class VoteCounterTests
    {
        #region Setup
        static IServiceProvider serviceProvider;
        static IVoteCounter voteCounter;
        static VoteConstructor voteConstructor;
        static Tally tally;
        static IQuest quest;
        static readonly Origin origin1 = new Origin("Brogatar", "123456", 100, new Uri("http://www.example.com/"), "http://www.example.com");
        static readonly Origin origin2 = new Origin("Madfish", "123466", 101, new Uri("http://www.example.com/"), "http://www.example.com");


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
        public async Task Reprocess_Doesnt_Stack_Lines()
        {
            string postText1 =
@"[X] Add this to your list of experiments for today.
[X] This is fine by you.
-[X] At least for today.";
            string postText2 =
@"[X] Save it for another day.
[X] This is fine by you.
-[X] At least for today.";

            Post post1 = new Post(origin1, postText1);
            Post post2 = new Post(origin2, postText2);

            Assert.IsTrue(post1.HasVote);
            Assert.IsTrue(post2.HasVote);

            List<Post> posts = new List<Post>() { post1, post2 };

            quest.PartitionMode = PartitionMode.None;

            await tally.TallyPosts(posts, quest, CancellationToken.None);

            List<VoteLineBlock> allVotes = voteCounter.VoteStorage.GetAllVotes().ToList();

            Assert.AreEqual(2, allVotes.Count);
            Assert.AreEqual(3, allVotes[0].Lines.Count);
            Assert.AreEqual(3, allVotes[1].Lines.Count);

            quest.PartitionMode = PartitionMode.ByLine;

            await tally.TallyPosts(CancellationToken.None);

            allVotes = voteCounter.VoteStorage.GetAllVotes().ToList();

            Assert.AreEqual(4, allVotes.Count);
            Assert.AreEqual(1, allVotes[0].Lines.Count);
            Assert.AreEqual(1, allVotes[1].Lines.Count);
            Assert.AreEqual(1, allVotes[2].Lines.Count);
            Assert.AreEqual(1, allVotes[3].Lines.Count);

            quest.PartitionMode = PartitionMode.None;

            await tally.TallyPosts(CancellationToken.None);

            allVotes = voteCounter.VoteStorage.GetAllVotes().ToList();

            Assert.AreEqual(2, allVotes.Count);
            Assert.AreEqual(3, allVotes[0].Lines.Count);
            Assert.AreEqual(3, allVotes[1].Lines.Count);
        }
    }
}
