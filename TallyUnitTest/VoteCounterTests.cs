using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Linq;

namespace NetTally.Tests
{
    [TestClass]
    public class VoteCounterTests
    {
        #region Setup
        static IVoteCounter voteCounter;
        static VoteCounter voteCounterRaw;
        static IQuest sampleQuest;

        [ClassInitialize]
        public static void ClassInit(TestContext context)
        {
            voteCounterRaw = new VoteCounter();
            voteCounter = voteCounterRaw;
            sampleQuest = new Quest();
        }

        [TestInitialize]
        public void Initialize()
        {
            voteCounter.Reset();
            voteCounter.PostsList.Clear();
        }
        #endregion

        [TestMethod]
        public void ResetTest()
        {
            voteCounter.Reset();

            Assert.AreEqual(0, voteCounter.GetVotersCollection(VoteType.Vote).Count);
            Assert.AreEqual(0, voteCounter.GetVotesCollection(VoteType.Vote).Count);
            Assert.AreEqual(0, voteCounter.GetVotersCollection(VoteType.Rank).Count);
            Assert.AreEqual(0, voteCounter.GetVotesCollection(VoteType.Rank).Count);

            Assert.AreEqual(0, voteCounter.ReferencePlanNames.Count);
            Assert.AreEqual(0, voteCounter.ReferencePlans.Count);
            Assert.AreEqual(0, voteCounter.ReferenceVoters.Count);
            Assert.AreEqual(0, voteCounter.ReferenceVoterPosts.Count);
            Assert.AreEqual(0, voteCounter.FutureReferences.Count);

            Assert.AreEqual(0, voteCounter.PlanNames.Count);
            Assert.AreEqual("", voteCounter.Title);
        }

        #region Get vote collections
        [TestMethod]
        public void GetVotesCollectionTest1()
        {
            Assert.AreEqual(voteCounterRaw.VotesWithSupporters, voteCounter.GetVotesCollection(VoteType.Vote));
        }

        [TestMethod]
        public void GetVotesCollectionTest2()
        {
            Assert.AreEqual(voteCounterRaw.VotesWithSupporters, voteCounter.GetVotesCollection(VoteType.Plan));
        }

        [TestMethod]
        public void GetVotesCollectionTest3()
        {
            Assert.AreEqual(voteCounterRaw.RankedVotesWithSupporters, voteCounter.GetVotesCollection(VoteType.Rank));
        }

        [TestMethod]
        public void GetVotersCollectionTest1()
        {
            Assert.AreEqual(voteCounterRaw.VoterMessageId, voteCounter.GetVotersCollection(VoteType.Vote));
        }

        [TestMethod]
        public void GetVotersCollectionTest2()
        {
            Assert.AreEqual(voteCounterRaw.VoterMessageId, voteCounter.GetVotersCollection(VoteType.Plan));
        }

        [TestMethod]
        public void GetVotersCollectionTest3()
        {
            Assert.AreEqual(voteCounterRaw.RankedVoterMessageId, voteCounter.GetVotersCollection(VoteType.Rank));
        }
        #endregion

        #region Add Vote param checks
        [TestMethod]
        [ExpectedException(typeof(ArgumentNullException))]
        public void AddVoteParamsTest1()
        {
            voteCounter.AddVotes(null, null, null, VoteType.Vote);
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentNullException))]
        public void AddVoteParamsTest2()
        {
            voteCounter.AddVotes(new List<string>(), null, null, VoteType.Vote);
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentNullException))]
        public void AddVoteParamsTest3()
        {
            voteCounter.AddVotes(new List<string>(), "me", null, VoteType.Vote);
        }

        [TestMethod]
        public void AddVoteParamsTest4()
        {
            voteCounter.AddVotes(new List<string>(), "me", "1", VoteType.Vote);
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentNullException))]
        public void AddVoteParamsTest5()
        {
            voteCounter.AddVotes(new List<string>(), "", "1", VoteType.Vote);
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentNullException))]
        public void AddVoteParamsTest6()
        {
            voteCounter.AddVotes(new List<string>(), "me", "", VoteType.Vote);
        }
        #endregion

        #region Add Votes
        [TestMethod]
        public void AddVoteTypeVoteTest()
        {
            string voteLine = "[x] First test";
            string voter = "me";
            string postId = "1";
            List<string> vote = new List<string> { voteLine };
            VoteType voteType = VoteType.Vote;

            voteCounter.AddVotes(vote, voter, postId, voteType);

            var votes = voteCounter.GetVotesCollection(voteType);
            var voters = voteCounter.GetVotersCollection(voteType);

            Assert.IsTrue(votes.Keys.Contains(voteLine));
            Assert.IsTrue(votes[voteLine].Contains(voter));

            Assert.IsTrue(voters.ContainsKey(voter));
            Assert.AreEqual(postId, voters[voter]);
        }

        [TestMethod]
        public void AddPlanTypeVoteTest()
        {
            string voteLine = "[x] First test";
            string voter = "me";
            string planname = "◈PlanPlan";
            string postId = "1";
            List<string> vote = new List<string> { voteLine };
            VoteType voteType = VoteType.Plan;

            voteCounter.AddVotes(vote, planname, postId, voteType);
            voteCounter.AddVotes(vote, voter, postId, VoteType.Vote);

            Assert.IsTrue(voteCounter.GetVotesCollection(voteType).Keys.Contains(voteLine));
            Assert.IsTrue(voteCounter.GetVotesCollection(voteType)[voteLine].Contains(voter));
            Assert.IsTrue(voteCounter.GetVotesCollection(voteType)[voteLine].Contains(planname));

            Assert.IsTrue(voteCounter.GetVotersCollection(voteType).ContainsKey(voter));
            Assert.IsTrue(voteCounter.GetVotersCollection(voteType).ContainsKey(planname));
            Assert.AreEqual(postId, voteCounter.GetVotersCollection(voteType)[voter]);
            Assert.AreEqual(postId, voteCounter.GetVotersCollection(voteType)[planname]);

            Assert.IsTrue(voteCounter.HasPlan("PlanPlan"));
        }

        [TestMethod]
        public void AddRankTypeVoteTest()
        {
            string voteLine = "[1] First test";
            string voter = "me";
            string postId = "1";
            List<string> vote = new List<string> { voteLine };
            VoteType voteType = VoteType.Rank;

            voteCounter.AddVotes(vote, voter, postId, voteType);

            Assert.IsTrue(voteCounter.GetVotesCollection(voteType).Keys.Contains(voteLine));
            Assert.IsTrue(voteCounter.GetVotesCollection(voteType)[voteLine].Contains(voter));

            Assert.IsTrue(voteCounter.GetVotersCollection(voteType).ContainsKey(voter));
            Assert.AreEqual(postId, voteCounter.GetVotersCollection(voteType)[voter]);
        }

        [TestMethod]
        public void AddVoteMultiTest1()
        {
            string voteLine = "[x] First test";
            string voter1 = "me";
            string postId1 = "1";
            string voter2 = "you";
            string postId2 = "2";
            List<string> vote = new List<string> { voteLine };
            VoteType voteType = VoteType.Vote;

            voteCounter.AddVotes(vote, voter1, postId1, voteType);
            voteCounter.AddVotes(vote, voter2, postId2, voteType);

            Assert.IsTrue(voteCounter.GetVotesCollection(voteType).Keys.Contains(voteLine));
            Assert.IsTrue(voteCounter.GetVotesCollection(voteType)[voteLine].Contains(voter1));
            Assert.IsTrue(voteCounter.GetVotesCollection(voteType)[voteLine].Contains(voter2));

            Assert.IsTrue(voteCounter.GetVotersCollection(voteType).ContainsKey(voter1));
            Assert.IsTrue(voteCounter.GetVotersCollection(voteType).ContainsKey(voter2));
            Assert.AreEqual(postId1, voteCounter.GetVotersCollection(voteType)[voter1]);
            Assert.AreEqual(postId2, voteCounter.GetVotersCollection(voteType)[voter2]);
        }

        [TestMethod]
        public void AddVoteMultiTest2()
        {
            string voteLine1 = "[x] First test";
            string voteLine2 = "[x] [b]First[/b] test";
            string voter1 = "me";
            string postId1 = "1";
            string voter2 = "you";
            string postId2 = "2";
            List<string> vote1 = new List<string> { voteLine1 };
            List<string> vote2 = new List<string> { voteLine2 };
            VoteType voteType = VoteType.Vote;

            voteCounter.AddVotes(vote1, voter1, postId1, voteType);
            voteCounter.AddVotes(vote2, voter2, postId2, voteType);

            Assert.IsTrue(voteCounter.GetVotesCollection(voteType).Keys.Contains(voteLine1));
            Assert.IsFalse(voteCounter.GetVotesCollection(voteType).Keys.Contains(voteLine2));
            Assert.AreEqual(1, voteCounter.GetVotesCollection(voteType).Count);
            Assert.IsTrue(voteCounter.GetVotesCollection(voteType)[voteLine1].Contains(voter1));
            Assert.IsTrue(voteCounter.GetVotesCollection(voteType)[voteLine1].Contains(voter2));

            Assert.IsTrue(voteCounter.GetVotersCollection(voteType).ContainsKey(voter1));
            Assert.IsTrue(voteCounter.GetVotersCollection(voteType).ContainsKey(voter2));
            Assert.AreEqual(postId1, voteCounter.GetVotersCollection(voteType)[voter1]);
            Assert.AreEqual(postId2, voteCounter.GetVotersCollection(voteType)[voter2]);
        }

        [TestMethod]
        public void AddVoteReplacementTest1()
        {
            string voteLine1 = "[x] First test";
            string voteLine2 = "[x] Second test";
            string voter1 = "me";
            string postId1 = "1";
            string postId2 = "2";
            List<string> vote1 = new List<string> { voteLine1 };
            List<string> vote2 = new List<string> { voteLine2 };
            VoteType voteType = VoteType.Vote;

            voteCounter.AddVotes(vote1, voter1, postId1, voteType);
            voteCounter.AddVotes(vote2, voter1, postId2, voteType);

            Assert.IsFalse(voteCounter.GetVotesCollection(voteType).Keys.Contains(voteLine1));
            Assert.IsTrue(voteCounter.GetVotesCollection(voteType).Keys.Contains(voteLine2));
            Assert.IsTrue(voteCounter.GetVotesCollection(voteType)[voteLine2].Contains(voter1));

            Assert.IsTrue(voteCounter.GetVotersCollection(voteType).ContainsKey(voter1));
            Assert.AreEqual(postId2, voteCounter.GetVotersCollection(voteType)[voter1]);
        }

        #endregion

        #region Matches
        private void TestMatch(string line1, string line2)
        {
            string voter1 = "me";
            string postId1 = "1";
            string voter2 = "you";
            string postId2 = "2";
            List<string> vote1 = new List<string> { line1 };
            List<string> vote2 = new List<string> { line2 };
            VoteType voteType = VoteType.Vote;

            voteCounter.AddVotes(vote1, voter1, postId1, voteType);
            voteCounter.AddVotes(vote2, voter2, postId2, voteType);

            Assert.IsTrue(voteCounter.GetVotesCollection(voteType).Keys.Contains(line1));
            //Assert.IsFalse(voteCounter.GetVotesCollection(voteType).Keys.Contains(line2));
            Assert.AreEqual(1, voteCounter.GetVotesCollection(voteType).Count);
            Assert.IsTrue(voteCounter.GetVotesCollection(voteType)[line1].Contains(voter1));
            Assert.IsTrue(voteCounter.GetVotesCollection(voteType)[line1].Contains(voter2));

            Assert.IsTrue(voteCounter.GetVotersCollection(voteType).ContainsKey(voter1));
            Assert.IsTrue(voteCounter.GetVotersCollection(voteType).ContainsKey(voter2));
            Assert.AreEqual(postId1, voteCounter.GetVotersCollection(voteType)[voter1]);
            Assert.AreEqual(postId2, voteCounter.GetVotersCollection(voteType)[voter2]);
        }

        private void TestMismatch(string line1, string line2)
        {
            string voter1 = "me";
            string postId1 = "1";
            string voter2 = "you";
            string postId2 = "2";
            List<string> vote1 = new List<string> { line1 };
            List<string> vote2 = new List<string> { line2 };
            VoteType voteType = VoteType.Vote;

            voteCounter.AddVotes(vote1, voter1, postId1, voteType);
            voteCounter.AddVotes(vote2, voter2, postId2, voteType);

            Assert.IsTrue(voteCounter.GetVotesCollection(voteType).Keys.Contains(line1));
            Assert.IsTrue(voteCounter.GetVotesCollection(voteType).Keys.Contains(line2));
            Assert.AreEqual(2, voteCounter.GetVotesCollection(voteType).Count);
            Assert.IsTrue(voteCounter.GetVotesCollection(voteType)[line1].Contains(voter1));
            Assert.IsTrue(voteCounter.GetVotesCollection(voteType)[line2].Contains(voter2));

            Assert.IsTrue(voteCounter.GetVotersCollection(voteType).ContainsKey(voter1));
            Assert.IsTrue(voteCounter.GetVotersCollection(voteType).ContainsKey(voter2));
            Assert.AreEqual(postId1, voteCounter.GetVotersCollection(voteType)[voter1]);
            Assert.AreEqual(postId2, voteCounter.GetVotersCollection(voteType)[voter2]);
        }

        [TestMethod]
        public void TestMatches1()
        {
            string voteLine1 = "[x] First test";
            string voteLine2 = "[x] First test";

            TestMatch(voteLine1, voteLine2);
        }

        [TestMethod]
        public void TestMatches2()
        {
            string voteLine1 = "[x] First test";
            string voteLine2 = "[x] [b]First[/b] test";

            TestMatch(voteLine1, voteLine2);
        }

        [TestMethod]
        public void TestMatches3()
        {
            string voteLine1 = "[x] First test";
            string voteLine2 = "[x] first TEST";

            TestMatch(voteLine1, voteLine2);
        }

        [TestMethod]
        public void TestMatches4()
        {
            string voteLine1 = "[x] First test";
            string voteLine2 = "[x] First  test";

            TestMatch(voteLine1, voteLine2);
        }

        [TestMethod]
        public void TestMatches5()
        {
            string voteLine1 = "[x] First test";
            string voteLine2 = "-[x] First test";

            TestMatch(voteLine1, voteLine2);
        }

        [TestMethod]
        public void TestMatches6()
        {
            string voteLine1 = "[x] First test";
            string voteLine2 = "[b][x] First test[/b]";

            TestMatch(voteLine1, voteLine2);
        }

        [TestMethod]
        public void TestMatches7()
        {
            string voteLine1 = "[x] First test";
            string voteLine2 = "[x] First test.";

            TestMatch(voteLine1, voteLine2);
        }

        [TestMethod]
        public void TestMatches8()
        {
            string voteLine1 = "[x] First test";
            string voteLine2 = "[x] First t'est.";

            TestMatch(voteLine1, voteLine2);
        }

        [TestMethod]
        public void TestMatches9()
        {
            string voteLine1 = "[x] “First Test”";
            string voteLine2 = "[x] \"First Test\"";

            TestMatch(voteLine1, voteLine2);
        }

        [TestMethod]
        public void TestMatches10()
        {
            string voteLine1 = "[x] Don't go";
            string voteLine2 = "[x] Donʼt go";

            TestMatch(voteLine1, voteLine2);
        }

        [TestMethod]
        public void TestMatches11()
        {
            string voteLine1 = "[x] Don't go";
            string voteLine2 = "[x] Don’t go";

            TestMatch(voteLine1, voteLine2);
        }

        [TestMethod]
        public void TestMatches12()
        {
            string voteLine1 = "[x] Don't go";
            string voteLine2 = "[x] Don`t go";

            TestMatch(voteLine1, voteLine2);
        }

        [TestMethod]
        public void TestMatches13()
        {
            string voteLine1 = "[x] First test";
            string voteLine2 = "[x] First &test";

            TestMatch(voteLine1, voteLine2);
        }



        [TestMethod]
        public void TestMismatches1()
        {
            string voteLine1 = "[x] First test";
            string voteLine2 = "[x] Second test";

            TestMismatch(voteLine1, voteLine2);
        }


        #endregion

        [TestMethod]
        public void FindVotesForVoterTest1()
        {
            string voteLine1 = "[x] Vote for stuff 1";
            string voteLine2 = "[x] Vote for stuff 2";
            string voter1 = "me";
            string voter2 = "you";
            string postId1 = "1";
            string postId2 = "2";
            List<string> vote1 = new List<string> { voteLine1 };
            List<string> vote2 = new List<string> { voteLine2 };
            VoteType voteType = VoteType.Vote;

            voteCounter.AddVotes(vote1, voter1, postId1, voteType);
            voteCounter.AddVotes(vote2, voter2, postId2, voteType);

            var votes = voteCounter.GetVotesFromReference("[x] me", "Him");
            Assert.AreEqual(1, votes.Count);
            Assert.IsTrue(votes.Contains(voteLine1));
        }

        [TestMethod]
        public void FindVotesForVoterTest2()
        {
            string voteLine1 = "[x] Vote for stuff 1";
            string voteLine2 = "[x] Vote for stuff 2";
            string voter1 = "me";
            string voter2 = "you";
            string postId1 = "1";
            string postId2 = "2";
            List<string> vote1 = new List<string> { voteLine1 };
            List<string> vote2 = new List<string> { voteLine1, voteLine2 };
            VoteType voteType = VoteType.Vote;

            voteCounter.AddVotes(vote1, voter1, postId1, voteType);
            voteCounter.AddVotes(vote2, voter2, postId2, voteType);

            var votes = voteCounter.GetVotesFromReference("[x] you", "Him");
            Assert.AreEqual(2, votes.Count);
            Assert.IsTrue(votes.Contains(voteLine1));
            Assert.IsTrue(votes.Contains(voteLine2));
        }

        [TestMethod]
        public void TallyVotesTest()
        {
            //TODO
        }

        [TestMethod]
        public void NameReferenceTest()
        {
            // Check for non-case sensitivity in referencing other voters.
            PostComponents p1 = new PostComponents("Beyogi", "12345", "[x] Vote for something");
            PostComponents p2 = new PostComponents("Mini", "12345", "[x] beyogi");
            voteCounter.PostsList.Add(p1);
            voteCounter.PostsList.Add(p2);
            voteCounter.TallyPosts(sampleQuest);

            Assert.AreEqual(2, voteCounter.GetVotersCollection(VoteType.Vote).Count);
            Assert.AreEqual(1, voteCounter.GetVotesCollection(VoteType.Vote).Count);
            Assert.IsTrue(voteCounter.HasVote("[x] Vote for something\r\n", VoteType.Vote));
        }

        //[TestMethod]
        public void TimingMethod()
        {
            const int postCount = 100;
            string[] voters = new string[postCount];
            string[] postIDs = new string[postCount];

            for (int i = 0; i < postCount; i++)
            {
                voters[i] = $"user{i+1}";
                postIDs[i] = $"{i+1}";
            }

            var posts1 = from n in Enumerable.Range(0, postCount)
                        select new PostComponents(voters[n], postIDs[n], "[X] A vote for something or other that may go on for a little while before deciding.");

            List<PostComponents> postList = new List<PostComponents>();

            const int loopCount = 100;

            using (new RegionProfiler("Test Loops addrange"))
            {
                for (int i = 0; i < loopCount; i++)
                {
                    var posts = from n in Enumerable.Range(0, postCount)
                                select new PostComponents(voters[n], postIDs[n], "[X] A vote for something or other that may go on for a little while before deciding.");
                    postList.Clear();
                    postList.AddRange(posts);
                }
            }

            postList.Clear();

            using (new RegionProfiler("Test Loops tolist"))
            {
                for (int i = 0; i < loopCount; i++)
                {
                    var posts = from n in Enumerable.Range(0, postCount)
                                select new PostComponents(voters[n], postIDs[n], "[X] A vote for something or other that may go on for a little while before deciding.");
                    postList = posts.ToList();
                }
            }
        }

    }
}