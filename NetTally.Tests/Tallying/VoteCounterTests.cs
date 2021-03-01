using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NetTally.Forums;
using NetTally.Utility.Comparers;
using NetTally.VoteCounting;
using NetTally.Votes;
using NetTally.Types.Enums;
using NetTally.Types.Components;

namespace NetTally.Tests.Tallying
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
        static IAgnostic agnostic;
        static readonly Origin origin1 = new Origin("Brogatar", "123456", 100, new Uri("http://www.example.com/"), "http://www.example.com");
        static readonly Origin origin1a = new Origin("Brogatar", "123476", 110, new Uri("http://www.example.com/"), "http://www.example.com");
        static readonly Origin origin2 = new Origin("Madfish", "123460", 101, new Uri("http://www.example.com/"), "http://www.example.com");
        static readonly Origin origin2a = new Origin("Madfish", "123466", 105, new Uri("http://www.example.com/"), "http://www.example.com");
        static readonly Origin origin3 = new Origin("Kinematics", "123426", 98, new Uri("http://www.example.com/"), "http://www.example.com");


        [ClassInitialize]
        public static void ClassInit(TestContext context)
        {
            serviceProvider = TestStartup.ConfigureServices();

            voteCounter = serviceProvider.GetRequiredService<IVoteCounter>();
            tally = serviceProvider.GetRequiredService<Tally>();
            voteConstructor = serviceProvider.GetRequiredService<VoteConstructor>();
            agnostic = serviceProvider.GetRequiredService<IAgnostic>();
        }

        [TestInitialize]
        public void Initialize()
        {
            quest = new Quest();

            voteCounter.Reset();
            voteCounter.ClearPosts();
        }

        [TestCleanup]
        public void TestCleanup()
        {
            quest.CaseIsSignificant = false;
            quest.WhitespaceAndPunctuationIsSignificant = false;
            agnostic.ComparisonPropertyChanged(quest, new PropertyChangedEventArgs(nameof(quest.CaseIsSignificant)));
        }
        #endregion

        #region Basics
        [TestMethod]
        public async Task Check_Tally_Adds_Normal()
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

            Assert.IsTrue(voteCounter.HasVoter(origin1.Author));
            Assert.IsTrue(voteCounter.HasVoter(origin2.Author));
        }


        [TestMethod]
        public async Task Check_Reset()
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

            voteCounter.Reset();

            allVotes = voteCounter.VoteStorage.GetAllVotes().ToList();

            Assert.AreEqual(0, allVotes.Count);
        }

        public async Task Check_Tally_Adds_Plan()
        {
            string postText1 =
@"[X] Plan Experiment
-[X] This is fine by you.
--[X] At least for today.";
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

            Assert.IsTrue(voteCounter.HasVoter(origin1.Author));
            Assert.IsTrue(voteCounter.HasVoter(origin2.Author));
            Assert.IsTrue(voteCounter.HasPlan("Experiment"));

            var vote1 = voteCounter.VoteStorage.GetVotesBy(origin1);

            Assert.AreEqual(1, vote1.Count);

            var voters1 = voteCounter.GetVotersFor(vote1[0]);

            Assert.AreEqual(2, voters1.Count());
        }
        #endregion Basics

        #region Replacements
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

        [TestMethod]
        public async Task Check_Tally_Adds_Reference()
        {
            string postText1 =
@"[X] Add this to your list of experiments for today.
[X] This is fine by you.
-[X] At least for today.";
            string postText2 =
@"[X] Brogatar";

            Post post1 = new Post(origin1, postText1);
            Post post2 = new Post(origin2, postText2);

            Assert.IsTrue(post1.HasVote);
            Assert.IsTrue(post2.HasVote);

            List<Post> posts = new List<Post>() { post1, post2 };

            quest.PartitionMode = PartitionMode.None;

            await tally.TallyPosts(posts, quest, CancellationToken.None);

            List<VoteLineBlock> allVotes = voteCounter.VoteStorage.GetAllVotes().ToList();

            Assert.AreEqual(1, allVotes.Count);
            Assert.AreEqual(3, allVotes[0].Lines.Count);

            Assert.IsTrue(voteCounter.HasVoter(origin1.Author));
            Assert.IsTrue(voteCounter.HasVoter(origin2.Author));
        }


        [TestMethod]
        public async Task Check_Tally_Replacement_Vote()
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
            Post post3 = new Post(origin1a, postText2);

            Assert.IsTrue(post1.HasVote);
            Assert.IsTrue(post2.HasVote);
            Assert.IsTrue(post3.HasVote);

            List<Post> posts = new List<Post>() { post1, post2, post3 };

            quest.PartitionMode = PartitionMode.None;

            await tally.TallyPosts(posts, quest, CancellationToken.None);

            List<VoteLineBlock> allVotes = voteCounter.VoteStorage.GetAllVotes().ToList();

            Assert.AreEqual(1, allVotes.Count);
            Assert.AreEqual(3, allVotes[0].Lines.Count);
            Assert.AreEqual(2, voteCounter.VoteStorage.GetSupportCountFor(allVotes[0]));

            Assert.IsTrue(voteCounter.HasVoter(origin1.Author));
            Assert.IsTrue(voteCounter.HasVoter(origin2.Author));
        }

        [TestMethod]
        public async Task Check_User_Proxy_Only_Proposed_Plan()
        {
            string postText1 =
@"[X] Proposed Plan: Experiment
-[X] Add this to your list of experiments for today.
-[X] This is fine by you.
--[X] At least for today.";
            string postText2 =
@"[X] Brogatar";

            Post post1 = new Post(origin1, postText1);
            Post post2 = new Post(origin2, postText2);

            Assert.IsTrue(post1.HasVote);
            Assert.IsTrue(post2.HasVote);

            List<Post> posts = new List<Post>() { post1, post2 };

            quest.PartitionMode = PartitionMode.None;

            await tally.TallyPosts(posts, quest, CancellationToken.None);

            List<VoteLineBlock> allVotes = voteCounter.VoteStorage.GetAllVotes().ToList();

            Assert.AreEqual(2, allVotes.Count);

            Assert.AreEqual(0, voteCounter.VoteStorage.GetSupportCountFor(allVotes[0]));
            Assert.AreEqual(1, voteCounter.VoteStorage.GetSupportCountFor(allVotes[1]));

            Assert.IsTrue(voteCounter.HasVoter(origin1.Author));
            Assert.IsTrue(voteCounter.HasVoter(origin2.Author));
            Assert.IsTrue(voteCounter.HasPlan("Experiment"));
        }

        [TestMethod]
        public async Task Check_Original_User_Can_Replace_Plan()
        {
            string postText1 =
@"[X] Plan: Experiment
-[X] Add this to your list of experiments for today.
-[X] This is fine by you.
--[X] At least for today.";
            string postText2 =
@"[X] Plan: Experiment
-[X] Alchemy structure
-[X] Pea soup
--[X] At least for today.";

            quest.PartitionMode = PartitionMode.None;
            quest.AllowUsersToUpdatePlans = true;
            voteCounter.Quest = quest;
            List<Post> posts;

            Post post1 = new Post(origin1, postText1);
            Post post2 = new Post(origin2, postText2);
            Post post3 = new Post(origin3, postText2);
            Post post4 = new Post(origin1a, postText2);

            posts = new List<Post>() { post1, post2 };
            voteCounter.AddPosts(posts);
            var plans = await tally.PreprocessPosts(default);

            Assert.AreEqual(1, plans.Count);
            Assert.AreEqual("Add this to your list of experiments for today.", plans.First().Value.Lines[1].Content);

            voteCounter.Reset();
            posts = new List<Post>() { post1, post2, post3 };
            voteCounter.AddPosts(posts);
            plans = await tally.PreprocessPosts(default);

            Assert.AreEqual(1, plans.Count);
            Assert.AreEqual("Add this to your list of experiments for today.", plans.First().Value.Lines[1].Content);

            voteCounter.Reset();
            posts = new List<Post>() { post1, post2, post3, post4 };
            voteCounter.AddPosts(posts);
            plans = await tally.PreprocessPosts(default);

            Assert.AreEqual(1, plans.Count);
            Assert.AreEqual("Alchemy structure", plans.First().Value.Lines[1].Content);
        }

        #endregion Replacements

        #region Callouts as proxies
        [TestMethod]
        public async Task Check_Callout_Links_With_At_As_Plan()
        {
            string postText1 =
@"[X] Add this to your list of experiments for today.
[X] This is fine by you.
-[X] At least for today.";
            string postText2 =
@"The referenced post did not have the problem described, but another post did.  Basically,
[x] Plan 『url=""https://forums.sufficientvelocity.com/members/4076/""』@Kinematics『/url』 
Wouldn't be applied to my proposed plan because it got turned into a member link (the '@' symbol is dropped on QQ's forums, so that doesn't interfere in this case).";

            Post post1 = new Post(origin3, postText1);
            Post post2 = new Post(origin1, postText2);

            Assert.IsTrue(post1.HasVote);
            Assert.IsTrue(post2.HasVote);

            List<Post> posts = new List<Post>() { post1, post2 };

            quest.PartitionMode = PartitionMode.None;

            await tally.TallyPosts(posts, quest, CancellationToken.None);

            List<VoteLineBlock> allVotes = voteCounter.VoteStorage.GetAllVotes().ToList();

            Assert.AreEqual(1, allVotes.Count);
            Assert.AreEqual(3, allVotes[0].Lines.Count);
            Assert.AreEqual(2, voteCounter.VoteStorage.GetSupportCountFor(allVotes[0]));

            Assert.IsTrue(voteCounter.HasVoter(origin1.Author));
            Assert.IsTrue(voteCounter.HasVoter(origin3.Author));
        }

        [TestMethod]
        public async Task Check_Callout_Links_Without_At_As_Plan()
        {
            string postText1 =
@"[X] Add this to your list of experiments for today.
[X] This is fine by you.
-[X] At least for today.";
            string postText2 =
@"The referenced post did not have the problem described, but another post did.  Basically,
[x] Plan 『url=""https://forums.sufficientvelocity.com/members/4076/""』Kinematics『/url』 
Wouldn't be applied to my proposed plan because it got turned into a member link (the '@' symbol is dropped on QQ's forums, so that doesn't interfere in this case).";

            Post post1 = new Post(origin3, postText1);
            Post post2 = new Post(origin1, postText2);

            Assert.IsTrue(post1.HasVote);
            Assert.IsTrue(post2.HasVote);

            List<Post> posts = new List<Post>() { post1, post2 };

            quest.PartitionMode = PartitionMode.None;

            await tally.TallyPosts(posts, quest, CancellationToken.None);

            List<VoteLineBlock> allVotes = voteCounter.VoteStorage.GetAllVotes().ToList();

            Assert.AreEqual(1, allVotes.Count);
            Assert.AreEqual(3, allVotes[0].Lines.Count);
            Assert.AreEqual(2, voteCounter.VoteStorage.GetSupportCountFor(allVotes[0]));

            Assert.IsTrue(voteCounter.HasVoter(origin1.Author));
            Assert.IsTrue(voteCounter.HasVoter(origin3.Author));
        }

        [TestMethod]
        public async Task Check_Callout_Links_With_At()
        {
            string postText1 =
@"[X] Add this to your list of experiments for today.
[X] This is fine by you.
-[X] At least for today.";
            string postText2 =
@"The referenced post did not have the problem described, but another post did.  Basically, 
[x] 『url=""https://forums.sufficientvelocity.com/members/4076/""』@Kinematics『/url』 
Wouldn't be applied to my proposed plan because it got turned into a member link (the '@' symbol is dropped on QQ's forums, so that doesn't interfere in this case).";

            Post post1 = new Post(origin3, postText1);
            Post post2 = new Post(origin1, postText2);

            Assert.IsTrue(post1.HasVote);
            Assert.IsTrue(post2.HasVote);

            List<Post> posts = new List<Post>() { post1, post2 };

            quest.PartitionMode = PartitionMode.None;

            await tally.TallyPosts(posts, quest, CancellationToken.None);

            List<VoteLineBlock> allVotes = voteCounter.VoteStorage.GetAllVotes().ToList();

            Assert.AreEqual(1, allVotes.Count);
            Assert.AreEqual(3, allVotes[0].Lines.Count);
            Assert.AreEqual(2, voteCounter.VoteStorage.GetSupportCountFor(allVotes[0]));

            Assert.IsTrue(voteCounter.HasVoter(origin1.Author));
            Assert.IsTrue(voteCounter.HasVoter(origin3.Author));
        }

        [TestMethod]
        public async Task Check_Callout_Links_Without_At()
        {
            string postText1 =
@"[X] Add this to your list of experiments for today.
[X] This is fine by you.
-[X] At least for today.";
            string postText2 =
@"The referenced post did not have the problem described, but another post did.  Basically, 
[x] 『url=""https://forums.sufficientvelocity.com/members/4076/""』Kinematics『/url』 
Wouldn't be applied to my proposed plan because it got turned into a member link (the '@' symbol is dropped on QQ's forums, so that doesn't interfere in this case).";

            Post post1 = new Post(origin3, postText1);
            Post post2 = new Post(origin1, postText2);

            Assert.IsTrue(post1.HasVote);
            Assert.IsTrue(post2.HasVote);

            List<Post> posts = new List<Post>() { post1, post2 };

            quest.PartitionMode = PartitionMode.None;

            await tally.TallyPosts(posts, quest, CancellationToken.None);

            List<VoteLineBlock> allVotes = voteCounter.VoteStorage.GetAllVotes().ToList();

            Assert.AreEqual(1, allVotes.Count);
            Assert.AreEqual(3, allVotes[0].Lines.Count);
            Assert.AreEqual(2, voteCounter.VoteStorage.GetSupportCountFor(allVotes[0]));

            Assert.IsTrue(voteCounter.HasVoter(origin1.Author));
            Assert.IsTrue(voteCounter.HasVoter(origin3.Author));
        }
        #endregion Callouts as proxies

        #region Future references
        [TestMethod]
        public async Task Check_Future_Reference_Handling_Normal()
        {
            string postText1 =
@"[X] Brogatar's First post";
            string postText2 =
@"[X] Brogatar";
            string postText3 =
@"[X] Brogatar's Second post";

            Post post1 = new Post(origin1, postText1);
            Post post2 = new Post(origin2, postText2);
            Post post3 = new Post(origin1a, postText3);

            Assert.IsTrue(post1.HasVote);
            Assert.IsTrue(post2.HasVote);
            Assert.IsTrue(post3.HasVote);

            List<Post> posts = new List<Post>() { post1, post2, post3 };

            quest.PartitionMode = PartitionMode.None;

            await tally.TallyPosts(posts, quest, CancellationToken.None);

            List<VoteLineBlock> allVotes = voteCounter.VoteStorage.GetAllVotes().ToList();

            Assert.AreEqual(1, allVotes.Count);

            Assert.AreEqual(2, voteCounter.VoteStorage.GetSupportCountFor(allVotes[0]));
            Assert.AreEqual("[] Brogatar's Second post", allVotes[0].ToString());

            Assert.IsTrue(voteCounter.HasVoter(origin1.Author));
            Assert.IsTrue(voteCounter.HasVoter(origin2.Author));
        }

        [TestMethod]
        public async Task Check_Future_Reference_Handling_Preempted()
        {
            string postText1 =
@"[X] Brogatar's First post";
            string postText2 =
@"[X] Brogatar";
            string postText3 =
@"[X] Changed my mind";
            string postText4 =
@"[X] Brogatar's Second post";

            Post post1 = new Post(origin1, postText1);
            Post post2 = new Post(origin2, postText2);
            Post post3 = new Post(origin2a, postText3);
            Post post4 = new Post(origin1a, postText4);

            Assert.IsTrue(post1.HasVote);
            Assert.IsTrue(post2.HasVote);
            Assert.IsTrue(post3.HasVote);
            Assert.IsTrue(post4.HasVote);

            List<Post> posts = new List<Post>() { post1, post2, post3, post4 };

            quest.PartitionMode = PartitionMode.None;

            await tally.TallyPosts(posts, quest, CancellationToken.None);

            List<VoteLineBlock> allVotes = voteCounter.VoteStorage.GetAllVotes().ToList();

            Assert.AreEqual(2, allVotes.Count);

            Assert.AreEqual(1, voteCounter.VoteStorage.GetSupportCountFor(allVotes[0]));
            Assert.AreEqual(1, voteCounter.VoteStorage.GetSupportCountFor(allVotes[1]));

            Assert.AreEqual("[] Changed my mind", allVotes[0].ToString());
            Assert.AreEqual("[] Brogatar's Second post", allVotes[1].ToString());

            Assert.IsTrue(voteCounter.HasVoter(origin1.Author));
            Assert.IsTrue(voteCounter.HasVoter(origin2.Author));
        }
        #endregion Future references

        #region Test general vote matching
        public async Task Test_Votes_Match(string text1, string text2)
        {
            Assert.IsFalse(string.IsNullOrEmpty(text1));
            Assert.IsFalse(string.IsNullOrEmpty(text2));
            agnostic.ComparisonPropertyChanged(quest, new PropertyChangedEventArgs(nameof(quest.CaseIsSignificant)));

            Post post1 = new Post(origin1, text1);
            Post post2 = new Post(origin2, text2);

            Assert.IsTrue(post1.HasVote);
            Assert.IsTrue(post2.HasVote);

            List<Post> posts = new List<Post>() { post1, post2 };

            quest.PartitionMode = PartitionMode.None;

            await tally.TallyPosts(posts, quest, CancellationToken.None);

            List<VoteLineBlock> allVotes = voteCounter.VoteStorage.GetAllVotes().ToList();

            Assert.AreEqual(1, allVotes.Count);


            Assert.IsTrue(voteCounter.HasVoter(origin1.Author));
            Assert.IsTrue(voteCounter.HasVoter(origin2.Author));

            var vote1 = allVotes[0];
            var voters = voteCounter.VoteStorage.GetVotersFor(vote1).ToList();

            Assert.AreEqual(2, voters.Count);
            Assert.IsTrue(voters.Contains(origin1));
            Assert.IsTrue(voters.Contains(origin2));
        }

        public async Task Test_Votes_Dont_Match(string text1, string text2)
        {
            Assert.IsFalse(string.IsNullOrEmpty(text1));
            Assert.IsFalse(string.IsNullOrEmpty(text2));
            agnostic.ComparisonPropertyChanged(quest, new PropertyChangedEventArgs(nameof(quest.CaseIsSignificant)));

            Post post1 = new Post(origin1, text1);
            Post post2 = new Post(origin2, text2);

            Assert.IsTrue(post1.HasVote);
            Assert.IsTrue(post2.HasVote);

            List<Post> posts = new List<Post>() { post1, post2 };

            quest.PartitionMode = PartitionMode.None;

            await tally.TallyPosts(posts, quest, CancellationToken.None);

            List<VoteLineBlock> allVotes = voteCounter.VoteStorage.GetAllVotes().ToList();

            Assert.AreEqual(2, allVotes.Count);


            Assert.IsTrue(voteCounter.HasVoter(origin1.Author));
            Assert.IsTrue(voteCounter.HasVoter(origin2.Author));

            var vote1 = allVotes[0];
            var voters1 = voteCounter.VoteStorage.GetVotersFor(vote1).ToList();
            var vote2 = allVotes[1];
            var voters2 = voteCounter.VoteStorage.GetVotersFor(vote2).ToList();

            Assert.AreEqual(1, voters1.Count);
            Assert.AreEqual(1, voters2.Count);
        }

        [TestMethod]
        public async Task Check_Match_Same()
        {
            string text1 = "[x] Basic test";
            string text2 = "[x] Basic test";
            quest.CaseIsSignificant = false;
            quest.WhitespaceAndPunctuationIsSignificant = false;

            await Test_Votes_Match(text1, text2);
        }

        [TestMethod]
        public async Task Check_Match_BBCode()
        {
            string text1 = "[x] Basic test";
            string text2 = "[x] Basic 『b』test『/b』";
            quest.CaseIsSignificant = false;
            quest.WhitespaceAndPunctuationIsSignificant = false;

            await Test_Votes_Match(text1, text2);
        }

        [TestMethod]
        public async Task Check_Match_No_Case()
        {
            string text1 = "[x] Basic test";
            string text2 = "[x] Basic TEST";
            quest.CaseIsSignificant = false;
            quest.WhitespaceAndPunctuationIsSignificant = false;

            await Test_Votes_Match(text1, text2);
        }

        [TestMethod]
        public async Task Check_Match_Yes_Case()
        {
            string text1 = "[x] Basic test";
            string text2 = "[x] Basic TEST";
            quest.CaseIsSignificant = true;
            quest.WhitespaceAndPunctuationIsSignificant = false;

            await Test_Votes_Dont_Match(text1, text2);
        }


        [TestMethod]
        public async Task Check_Match_No_Punc()
        {
            string text1 = "[x] Basic test";
            string text2 = "[x] Basic 'test'";
            quest.CaseIsSignificant = false;
            quest.WhitespaceAndPunctuationIsSignificant = false;

            await Test_Votes_Match(text1, text2);
        }

        [TestMethod]
        public async Task Check_Match_Yes_Punc()
        {
            string text1 = "[x] Basic test";
            string text2 = "[x] Basic 'test'";
            quest.CaseIsSignificant = false;
            quest.WhitespaceAndPunctuationIsSignificant = true;

            await Test_Votes_Dont_Match(text1, text2);
        }


        [TestMethod]
        public async Task Check_Match_No_Space()
        {
            string text1 = "[x] Basic test";
            string text2 = "[x] Basic 'Test'";
            quest.CaseIsSignificant = false;
            quest.WhitespaceAndPunctuationIsSignificant = false;

            await Test_Votes_Match(text1, text2);
        }

        [TestMethod]
        public async Task Check_Match_Yes_Space()
        {
            string text1 = "[x] Basic test";
            string text2 = "[x] Basic  Test";
            quest.CaseIsSignificant = false;
            quest.WhitespaceAndPunctuationIsSignificant = true;

            await Test_Votes_Dont_Match(text1, text2);
        }


        [TestMethod]
        public async Task Check_Match_No_Space_And_Case()
        {
            string text1 = "[x] Basic test";
            string text2 = "[x] Basic 'Test'";
            quest.CaseIsSignificant = false;
            quest.WhitespaceAndPunctuationIsSignificant = false;

            await Test_Votes_Match(text1, text2);
        }

        [TestMethod]
        public async Task Check_Match_Yes_Space_And_Case()
        {
            string text1 = "[x] Basic test";
            string text2 = "[x] Basic 'test'";
            quest.CaseIsSignificant = true;
            quest.WhitespaceAndPunctuationIsSignificant = true;

            await Test_Votes_Dont_Match(text1, text2);
        }

        [TestMethod]
        public async Task Check_Match_Yes_Space_And_Case_2()
        {
            string text1 = "[x] Basic test";
            string text2 = "[x] Basic Test";
            quest.CaseIsSignificant = true;
            quest.WhitespaceAndPunctuationIsSignificant = true;

            await Test_Votes_Dont_Match(text1, text2);
        }

        [TestMethod]
        public async Task Check_Match_Apostrophe()
        {
            string text1 = "[x] Basic don't";
            string text2 = "[x] Basic don’t";
            quest.CaseIsSignificant = false;
            quest.WhitespaceAndPunctuationIsSignificant = false;

            await Test_Votes_Match(text1, text2);
        }

        [TestMethod]
        public async Task Check_Match_Quote()
        {
            string text1 = "[x] Basic test";
            string text2 = "[x] Basic “test”";
            quest.CaseIsSignificant = false;
            quest.WhitespaceAndPunctuationIsSignificant = false;

            await Test_Votes_Match(text1, text2);
        }


        [TestMethod]
        public async Task Check_Match_Apostrophe_2()
        {
            string text1 = "[x] Basic don't";
            string text2 = "[x] Basic don’t";
            quest.CaseIsSignificant = false;
            quest.WhitespaceAndPunctuationIsSignificant = true;

            await Test_Votes_Match(text1, text2);
        }

        [TestMethod]
        public async Task Check_Match_Quote_2()
        {
            string text1 = @"[x] Basic ""test""";
            string text2 = "[x] Basic “test”";
            quest.CaseIsSignificant = false;
            quest.WhitespaceAndPunctuationIsSignificant = true;

            await Test_Votes_Match(text1, text2);
        }
        #endregion Test general vote matching
    }
}
