using System;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NetTally.Forums;
using NetTally.VoteCounting;
using NetTally.Votes;

namespace NetTally.Tests.Votes
{
    [TestClass]
    public class ReferencingVotesTests
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

        #region Define post text
        readonly static string oneLine = @"[X] Run Lola Run!";
        readonly static string oneLineTask = @"[X][Movie] Run Lola Run!";
        readonly static string twoLine = @"[X] Run Lola Run!
[X] National Geographic";
        readonly static string twoLineTask = @"[X][Movie] Run Lola Run!
[X] National Geographic";
        readonly static string childLine = @"[X][Movie] Run Lola Run!
-[X] National Geographic";
        readonly static string twoChunk = @"[X][Movie] Run Lola Run!
-[X] National Geographic
[X] Gunbuster";

        readonly static string refKinematics = @"[X] Kinematics";
        readonly static string refAtreya = @"[X] Atreya";
        readonly static string refKimberly = @"[X] Kimberly";
        readonly static string refKinematicsPercent = @"[88%] Kinematics";
        readonly static string refAtreyaPercent = @"[77%] Atreya";
        readonly static string refKimberlyPercent = @"[66%] Kimberly";
        readonly static string refKinematicsApprove = @"[+] Kinematics";
        readonly static string refAtreyaApprove = @"[+] Atreya";
        readonly static string refKimberlyApprove = @"[-] Kimberly";
        #endregion

        #region Generate user posts
        Post GetPostFromUser1(string postText)
        {
            Origin origin = new Origin("Kinematics", "123456", 100, new Uri("http://www.example.com/"), "http://www.example.com");

            return new Post(origin, postText);
        }

        Post GetPostFromUser2(string postText)
        {
            Origin origin = new Origin("Atreya", "123457", 101, new Uri("http://www.example.com/"), "http://www.example.com");

            return new Post(origin, postText);
        }

        Post GetPostFromUser3(string postText)
        {
            Origin origin = new Origin("Kimberly", "123458", 102, new Uri("http://www.example.com/"), "http://www.example.com");

            return new Post(origin, postText);
        }

        Post GetPostFromUser4(string postText)
        {
            Origin origin = new Origin("Kinematics", "123459", 103, new Uri("http://www.example.com/"), "http://www.example.com");

            return new Post(origin, postText);
        }
        #endregion

        [TestMethod]
        public void Simple_Reference()
        {
            string voteText1 = oneLine;
            string voteText2 = refKinematics;
            Post post1 = GetPostFromUser1(voteText1);
            Post post2 = GetPostFromUser2(voteText2);

            quest.PartitionMode = PartitionMode.ByLine;
            quest.DisableProxyVotes = false;

            voteCounter.AddReferenceVoter(post1.Origin);
            voteCounter.AddReferenceVoter(post2.Origin);

            var results1 = voteConstructor.ProcessPostGetVotes(post1, quest);

            if (results1 != null)
                voteCounter.AddVotes(results1, post1.Origin);

            var results2 = voteConstructor.ProcessPostGetVotes(post2, quest);

            if (results2 != null)
            {
                Assert.IsTrue(results2[0].Lines.Count == 1);
                Assert.AreEqual(voteText1, results2[0].Lines[0].ToString());

                voteCounter.AddVotes(results2, post2.Origin);
            }

            Assert.IsFalse(results2 == null);
        }


    }
}
