using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NetTally.Types.Components;
using NetTally.Types.Enums;
using NetTally.Utility.Comparers;
using NetTally.VoteCounting;
using NetTally.Votes;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;

namespace NetTally.Tests.Votes
{
    [TestClass]
    public class ReferencingVotesTests
    {
        #region Setup
        static IServiceProvider serviceProvider = null!;
        static VoteConstructor voteConstructor = null!;
        static Tally tally = null!;
        static Quest quest = null!;
        static IAgnostic agnostic = null!;

        [ClassInitialize]
        public static void ClassInit(TestContext context)
        {
            serviceProvider = TestStartup.ConfigureServices();

            tally = serviceProvider.GetRequiredService<Tally>();
            voteConstructor = serviceProvider.GetRequiredService<VoteConstructor>();
            agnostic = serviceProvider.GetRequiredService<IAgnostic>();
        }

        [TestInitialize]
        public void Initialize()
        {
            quest = new Quest();
            IVoteCounter voteCounter = serviceProvider.GetRequiredService<IVoteCounter>();
            quest.VoteCounter = voteCounter;

            agnostic.ComparisonPropertyChanged(quest, new PropertyChangedEventArgs(nameof(quest.CaseIsSignificant)));
        }

        [TestCleanup]
        public void TestCleanup()
        {
            quest.CaseIsSignificant = false;
            quest.WhitespaceAndPunctuationIsSignificant = false;
            agnostic.ComparisonPropertyChanged(quest, new PropertyChangedEventArgs(nameof(quest.CaseIsSignificant)));
        }
        #endregion

        #region Define post text
        readonly static string oneLine = @"[X] Run Lola Run!";
        readonly static string oneLineTask = @"[X][Movie] Run Lola Run!";
        readonly static string twoLine = @"[X] Run Lola Run!
[X] National Geographic";
        readonly static string implicitPlan = @"[X][Movie] Plan Run Lola Run!
[X] National Geographic";
        readonly static string explicitPlan = @"[X][Movie] Plan Run Lola Run!
-[X] National Geographic";
        readonly static string twoChunkPlan = @"[X][Movie] Plan Run Lola Run!
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
        static Post GetPostFromKinematics1(string postText)
        {
            Origin origin = new("Kinematics", "123456", 100, new Uri("http://www.example.com/"), "http://www.example.com");

            return new Post(origin, postText);
        }

        static Post GetPostFromAtreya(string postText)
        {
            Origin origin = new("Atreya", "123457", 101, new Uri("http://www.example.com/"), "http://www.example.com");

            return new Post(origin, postText);
        }

        static Post GetPostFromKimberly(string postText)
        {
            Origin origin = new("Kimberly", "123458", 102, new Uri("http://www.example.com/"), "http://www.example.com");

            return new Post(origin, postText);
        }

        static Post GetPostFromKinematics2(string postText)
        {
            Origin origin = new("Kinematics", "123459", 103, new Uri("http://www.example.com/"), "http://www.example.com");

            return new Post(origin, postText);
        }
        #endregion

        [TestMethod]
        public void Simple_Reference()
        {
            quest.PartitionMode = PartitionMode.ByLine;
            quest.DisableProxyVotes = false;

            string voteText1 = oneLine;
            string voteText2 = refKinematics;
            Post post1 = GetPostFromKinematics1(voteText1);
            Post post2 = GetPostFromAtreya(voteText2);

            List<Post> posts = new() { post1, post2 };

            quest.VoteCounter.AddPosts(posts);
            quest.VoteCounter.AddReferenceVoter(post1.Origin);
            quest.VoteCounter.AddReferenceVoter(post2.Origin);

            var results1 = voteConstructor.ProcessPostGetVotes(post1, quest);

            if (results1 != null)
            {
                quest.VoteCounter.AddVotes(results1, post1.Origin);

                var results2 = voteConstructor.ProcessPostGetVotes(post2, quest);

                if (results2 != null)
                {
                    Assert.IsTrue(results1[0].Lines[0] == results2[0].Lines[0]);

                    quest.VoteCounter.AddVotes(results2, post2.Origin);

                    Assert.AreEqual(2, quest.VoteCounter.VoteStorage.GetSupportCountFor(results1[0]));
                }

                Assert.IsFalse(results2 == null);
            }

            Assert.IsFalse(results1 == null);
        }

        [TestMethod]
        public void Simple_Reference_Marker_Percent()
        {
            quest.PartitionMode = PartitionMode.ByLine;
            quest.DisableProxyVotes = false;

            string voteText1 = oneLine;
            string voteText2 = refKinematicsPercent;
            Post post1 = GetPostFromKinematics1(voteText1);
            Post post2 = GetPostFromAtreya(voteText2);

            List<Post> posts = new() { post1, post2 };

            quest.VoteCounter.AddPosts(posts);
            quest.VoteCounter.AddReferenceVoter(post1.Origin);
            quest.VoteCounter.AddReferenceVoter(post2.Origin);

            var results1 = voteConstructor.ProcessPostGetVotes(post1, quest);

            if (results1 != null)
            {
                quest.VoteCounter.AddVotes(results1, post1.Origin);

                var results2 = voteConstructor.ProcessPostGetVotes(post2, quest);

                if (results2 != null)
                {
                    Assert.IsTrue(results1[0].Lines[0] == results2[0].Lines[0]);

                    quest.VoteCounter.AddVotes(results2, post2.Origin);

                    Assert.AreEqual(2, quest.VoteCounter.VoteStorage.GetSupportCountFor(results1[0]));
                }

                Assert.IsFalse(results2 == null);
            }

            Assert.IsFalse(results1 == null);
        }

        [TestMethod]
        public void Simple_Reference_Marker_Approve()
        {
            quest.PartitionMode = PartitionMode.ByLine;
            quest.DisableProxyVotes = false;

            string voteText1 = oneLine;
            string voteText2 = refKinematicsApprove;
            Post post1 = GetPostFromKinematics1(voteText1);
            Post post2 = GetPostFromAtreya(voteText2);

            List<Post> posts = new() { post1, post2 };

            quest.VoteCounter.AddPosts(posts);
            quest.VoteCounter.AddReferenceVoter(post1.Origin);
            quest.VoteCounter.AddReferenceVoter(post2.Origin);

            var results1 = voteConstructor.ProcessPostGetVotes(post1, quest);

            if (results1 != null)
            {
                quest.VoteCounter.AddVotes(results1, post1.Origin);

                var results2 = voteConstructor.ProcessPostGetVotes(post2, quest);

                if (results2 != null)
                {
                    Assert.IsTrue(results1[0].Lines[0] == results2[0].Lines[0]);

                    quest.VoteCounter.AddVotes(results2, post2.Origin);

                    Assert.AreEqual(2, quest.VoteCounter.VoteStorage.GetSupportCountFor(results1[0]));
                }

                Assert.IsFalse(results2 == null);
            }

            Assert.IsFalse(results1 == null);
        }

        [TestMethod]
        public void Simple_Reference_Task()
        {
            quest.PartitionMode = PartitionMode.ByLine;
            quest.DisableProxyVotes = false;

            string voteText1 = oneLineTask;
            string voteText2 = refAtreya;
            Post post1 = GetPostFromAtreya(voteText1);
            Post post2 = GetPostFromKimberly(voteText2);

            List<Post> posts = new() { post1, post2 };

            quest.VoteCounter.AddPosts(posts);
            quest.VoteCounter.AddReferenceVoter(post1.Origin);
            quest.VoteCounter.AddReferenceVoter(post2.Origin);

            var results1 = voteConstructor.ProcessPostGetVotes(post1, quest);

            if (results1 != null)
            {
                quest.VoteCounter.AddVotes(results1, post1.Origin);

                var results2 = voteConstructor.ProcessPostGetVotes(post2, quest);

                if (results2 != null)
                {
                    Assert.IsTrue(results1[0].Lines[0] == results2[0].Lines[0]);

                    quest.VoteCounter.AddVotes(results2, post2.Origin);

                    Assert.AreEqual(2, quest.VoteCounter.VoteStorage.GetSupportCountFor(results1[0]));
                }

                Assert.IsFalse(results2 == null);
            }

            Assert.IsFalse(results1 == null);
        }

        [TestMethod]
        public void Simple_Reference_Task_Marker_Percent()
        {
            quest.PartitionMode = PartitionMode.ByLine;
            quest.DisableProxyVotes = false;

            string voteText1 = oneLineTask;
            string voteText2 = refAtreyaPercent;
            Post post1 = GetPostFromAtreya(voteText1);
            Post post2 = GetPostFromKimberly(voteText2);

            List<Post> posts = new() { post1, post2 };

            quest.VoteCounter.AddPosts(posts);
            quest.VoteCounter.AddReferenceVoter(post1.Origin);
            quest.VoteCounter.AddReferenceVoter(post2.Origin);

            var results1 = voteConstructor.ProcessPostGetVotes(post1, quest);

            if (results1 != null)
            {
                quest.VoteCounter.AddVotes(results1, post1.Origin);

                var results2 = voteConstructor.ProcessPostGetVotes(post2, quest);

                if (results2 != null)
                {
                    Assert.IsTrue(results1[0].Lines[0] == results2[0].Lines[0]);

                    quest.VoteCounter.AddVotes(results2, post2.Origin);

                    Assert.AreEqual(2, quest.VoteCounter.VoteStorage.GetSupportCountFor(results1[0]));
                }

                Assert.IsFalse(results2 == null);
            }

            Assert.IsFalse(results1 == null);
        }

        [TestMethod]
        public void Simple_Reference_Task_Marker_Approve()
        {
            quest.PartitionMode = PartitionMode.ByLine;
            quest.DisableProxyVotes = false;

            string voteText1 = oneLineTask;
            string voteText2 = refAtreyaApprove;
            Post post1 = GetPostFromAtreya(voteText1);
            Post post2 = GetPostFromKimberly(voteText2);

            List<Post> posts = new() { post1, post2 };

            quest.VoteCounter.AddPosts(posts);
            quest.VoteCounter.AddReferenceVoter(post1.Origin);
            quest.VoteCounter.AddReferenceVoter(post2.Origin);

            var results1 = voteConstructor.ProcessPostGetVotes(post1, quest);

            if (results1 != null)
            {
                quest.VoteCounter.AddVotes(results1, post1.Origin);

                var results2 = voteConstructor.ProcessPostGetVotes(post2, quest);

                if (results2 != null)
                {
                    Assert.IsTrue(results1[0].Lines[0] == results2[0].Lines[0]);

                    quest.VoteCounter.AddVotes(results2, post2.Origin);

                    Assert.AreEqual(2, quest.VoteCounter.VoteStorage.GetSupportCountFor(results1[0]));
                }

                Assert.IsFalse(results2 == null);
            }

            Assert.IsFalse(results1 == null);
        }

        [TestMethod]
        public void Fail_Self_Reference()
        {
            quest.PartitionMode = PartitionMode.ByLine;
            quest.DisableProxyVotes = false;

            string voteText1 = twoLine;
            string voteText2 = refKimberly;
            Post post1 = GetPostFromAtreya(voteText1);
            Post post2 = GetPostFromKimberly(voteText2);

            List<Post> posts = new() { post1, post2 };

            quest.VoteCounter.AddPosts(posts);
            quest.VoteCounter.AddReferenceVoter(post1.Origin);
            quest.VoteCounter.AddReferenceVoter(post2.Origin);

            var results1 = voteConstructor.ProcessPostGetVotes(post1, quest);

            if (results1 != null)
            {
                quest.VoteCounter.AddVotes(results1, post1.Origin);

                var results2 = voteConstructor.ProcessPostGetVotes(post2, quest);

                if (results2 != null)
                {
                    Assert.IsTrue(results1[0].Lines[0] == results2[0].Lines[0]);

                    quest.VoteCounter.AddVotes(results2, post2.Origin);

                    Assert.AreEqual(1, quest.VoteCounter.VoteStorage.GetSupportCountFor(results1[0]));
                }

                Assert.IsTrue(results2 == null);
            }

            Assert.IsFalse(results1 == null);
        }

        [TestMethod]
        public void Fail_No_Proxy()
        {
            quest.PartitionMode = PartitionMode.ByLine;
            quest.DisableProxyVotes = true;

            string voteText1 = twoLine;
            string voteText2 = refKimberly;
            Post post1 = GetPostFromKimberly(voteText1);
            Post post2 = GetPostFromKinematics1(voteText2);

            List<Post> posts = new() { post1, post2 };

            quest.VoteCounter.AddPosts(posts);
            quest.VoteCounter.AddReferenceVoter(post1.Origin);
            quest.VoteCounter.AddReferenceVoter(post2.Origin);

            var results1 = voteConstructor.ProcessPostGetVotes(post1, quest);

            if (results1 != null)
            {
                quest.VoteCounter.AddVotes(results1, post1.Origin);

                var results2 = voteConstructor.ProcessPostGetVotes(post2, quest);

                if (results2 != null)
                {
                    quest.VoteCounter.AddVotes(results2, post2.Origin);

                    Assert.IsFalse(results1[0].Lines[0] == results2[0].Lines[0]);

                    Assert.AreEqual(1, quest.VoteCounter.VoteStorage.GetSupportCountFor(results1[0]));
                }

                Assert.IsFalse(results2 == null);
            }

            Assert.IsFalse(results1 == null);
        }

        [TestMethod]
        public void Non_Self_Reference()
        {
            quest.PartitionMode = PartitionMode.ByLine;
            quest.DisableProxyVotes = false;

            string voteText1 = twoLine;
            string voteText2 = refKimberlyPercent;
            Post post1 = GetPostFromAtreya(voteText1);
            Post post2 = GetPostFromKinematics1(voteText2);

            List<Post> posts = new() { post1, post2 };

            quest.VoteCounter.AddPosts(posts);
            quest.VoteCounter.AddReferenceVoter(post1.Origin);
            quest.VoteCounter.AddReferenceVoter(post2.Origin);

            var results1 = voteConstructor.ProcessPostGetVotes(post1, quest);

            if (results1 != null)
            {
                quest.VoteCounter.AddVotes(results1, post1.Origin);

                var results2 = voteConstructor.ProcessPostGetVotes(post2, quest);

                if (results2 != null)
                {
                    Assert.IsFalse(results1[0].Lines[0] == results2[0].Lines[0]);

                    quest.VoteCounter.AddVotes(results2, post2.Origin);

                    Assert.AreEqual(1, quest.VoteCounter.VoteStorage.GetSupportCountFor(results1[0]));
                }

                Assert.IsFalse(results2 == null);
            }

            Assert.IsFalse(results1 == null);
        }

        [TestMethod]
        public async Task Implicit_Plan_Name_RefAsync()
        {
            quest.PartitionMode = PartitionMode.ByBlock;
            quest.DisableProxyVotes = false;

            string voteText1 = implicitPlan;
            string voteText2 = refKimberlyApprove;
            Post post1 = GetPostFromKimberly(voteText1);
            Post post2 = GetPostFromAtreya(voteText2);

            List<Post> posts = new() { post1, post2 };

            quest.VoteCounter.AddPosts(posts);

            await tally.PreprocessPosts(quest, default);

            var results1 = voteConstructor.ProcessPostGetVotes(post1, quest);

            if (results1 != null)
            {
                quest.VoteCounter.AddVotes(results1, post1.Origin);

                var results2 = voteConstructor.ProcessPostGetVotes(post2, quest);

                if (results2 != null)
                {
                    Assert.IsTrue(results1[0].Lines[0] == results2[0].Lines[0]);

                    quest.VoteCounter.AddVotes(results2, post2.Origin);

                    Assert.AreEqual(2, quest.VoteCounter.VoteStorage.GetSupportCountFor(results1[0]));
                }

                Assert.IsFalse(results2 == null);
            }

            Assert.IsFalse(results1 == null);
        }

        [TestMethod]
        public async Task Explicit_Plan_RefAsync()
        {
            quest.PartitionMode = PartitionMode.ByBlock;
            quest.DisableProxyVotes = false;

            string voteText1 = explicitPlan;
            string voteText2 = oneLine;
            Post post1 = GetPostFromKimberly(voteText1);
            Post post2 = GetPostFromAtreya(voteText2);

            List<Post> posts = new() { post1, post2 };

            quest.VoteCounter.AddPosts(posts);

            await tally.PreprocessPosts(quest, default);

            var results1 = voteConstructor.ProcessPostGetVotes(post1, quest);

            if (results1 != null)
            {
                quest.VoteCounter.AddVotes(results1, post1.Origin);

                var results2 = voteConstructor.ProcessPostGetVotes(post2, quest);

                if (results2 != null)
                {
                    Assert.IsTrue(results1[0] == results2[0]);

                    quest.VoteCounter.AddVotes(results2, post2.Origin);

                    Assert.AreEqual(2, quest.VoteCounter.VoteStorage.GetSupportCountFor(results1[0]));
                }

                Assert.IsFalse(results2 == null);
            }

            Assert.IsFalse(results1 == null);
        }

        [TestMethod]
        public async Task Explicit_Plan_TwoChunk_RefAsync()
        {
            quest.PartitionMode = PartitionMode.ByBlock;
            quest.DisableProxyVotes = false;

            string voteText1 = twoChunkPlan;
            string voteText2 = oneLine;
            Post post1 = GetPostFromKimberly(voteText1);
            Post post2 = GetPostFromKinematics2(voteText2);

            List<Post> posts = new() { post1, post2 };

            quest.VoteCounter.AddPosts(posts);

            await tally.PreprocessPosts(quest, default);

            var results1 = voteConstructor.ProcessPostGetVotes(post1, quest);

            if (results1 != null)
            {
                quest.VoteCounter.AddVotes(results1, post1.Origin);

                var results2 = voteConstructor.ProcessPostGetVotes(post2, quest);

                if (results2 != null)
                {
                    quest.VoteCounter.AddVotes(results2, post2.Origin);

                    Assert.IsTrue(results1[0] == results2[0]);
                    Assert.AreEqual(2, results1.Count);
                    Assert.AreEqual(1, results2.Count);

                    Assert.AreEqual(2, quest.VoteCounter.VoteStorage.GetSupportCountFor(results1[0]));
                }

                Assert.IsFalse(results2 == null);
            }

            Assert.IsFalse(results1 == null);
        }

        [TestMethod]
        public async Task Implicit_Plan_RefAsync()
        {
            quest.PartitionMode = PartitionMode.None;
            quest.DisableProxyVotes = false;

            string voteText1 = implicitPlan;
            string voteText2 = oneLine;
            Post post1 = GetPostFromKimberly(voteText1);
            Post post2 = GetPostFromAtreya(voteText2);

            List<Post> posts = new() { post1, post2 };

            quest.VoteCounter.AddPosts(posts);

            await tally.PreprocessPosts(quest, default);

            var results1 = voteConstructor.ProcessPostGetVotes(post1, quest);

            if (results1 != null)
            {
                quest.VoteCounter.AddVotes(results1, post1.Origin);

                var results2 = voteConstructor.ProcessPostGetVotes(post2, quest);

                if (results2 != null)
                {
                    Assert.IsTrue(results1[0] == results2[0]);

                    quest.VoteCounter.AddVotes(results2, post2.Origin);

                    Assert.AreEqual(2, quest.VoteCounter.VoteStorage.GetSupportCountFor(results1[0]));
                }

                Assert.IsFalse(results2 == null);
            }

            Assert.IsFalse(results1 == null);
        }

        [TestMethod]
        public async Task Implicit_Plan_Block_RefAsync()
        {
            quest.PartitionMode = PartitionMode.ByBlock;
            quest.DisableProxyVotes = false;

            string voteText1 = implicitPlan;
            string voteText2 = oneLine;
            Post post1 = GetPostFromKimberly(voteText1);
            Post post2 = GetPostFromAtreya(voteText2);

            List<Post> posts = new() { post1, post2 };

            quest.VoteCounter.AddPosts(posts);

            await tally.PreprocessPosts(quest, default);

            var results1 = voteConstructor.ProcessPostGetVotes(post1, quest);

            if (results1 != null)
            {
                quest.VoteCounter.AddVotes(results1, post1.Origin);

                var results2 = voteConstructor.ProcessPostGetVotes(post2, quest);

                if (results2 != null)
                {
                    Assert.IsTrue(results1[0] == results2[0]);

                    quest.VoteCounter.AddVotes(results2, post2.Origin);

                    Assert.AreEqual(2, quest.VoteCounter.VoteStorage.GetSupportCountFor(results1[0]));
                }

                Assert.IsFalse(results2 == null);
            }

            Assert.IsFalse(results1 == null);
        }


        [TestMethod]
        public async Task Cross_Marker_Reference_PlanAsync()
        {
            quest.PartitionMode = PartitionMode.ByBlock;
            quest.DisableProxyVotes = false;

            string voteText1 =
@"[X] Proposed plan: Mountain biking
-[x] Camelback Mountain
-[x] Grand Canyon";

            string voteText2 = @"[75%] Plan Mountain biking";

            Post post1 = GetPostFromKinematics1(voteText1);
            Post post2 = GetPostFromKinematics2(voteText2);

            List<Post> posts = new() { post1, post2 };

            quest.VoteCounter.AddPosts(posts);

            await tally.PreprocessPosts(quest, default);

            var results1 = voteConstructor.ProcessPostGetVotes(post1, quest);

            Assert.IsFalse(results1 == null);

            if (results1 == null)
                return;

            Assert.AreEqual(0, results1.Count);

            var results2 = voteConstructor.ProcessPostGetVotes(post2, quest);

            if (results2 != null)
            {
                Assert.AreEqual(1, results2.Count);

                quest.VoteCounter.AddVotes(results2, post2.Origin);
                Assert.AreEqual(1, quest.VoteCounter.VoteStorage.GetSupportCountFor(results2[0]));
                Assert.AreEqual(2, quest.VoteCounter.VoteStorage.GetSupportersFor(results2[0])?.Count ?? 0);
                Assert.AreEqual(1, quest.VoteCounter.VoteStorage.GetVotesBy(post2.Origin).Count);

                var allVotes = quest.VoteCounter.GetAllVotes();
                Assert.AreEqual(1, allVotes.Count());

                Assert.AreEqual(MarkerType.Score, allVotes.First().Category);
            }

            Assert.IsFalse(results2 == null);
        }
    }
}
