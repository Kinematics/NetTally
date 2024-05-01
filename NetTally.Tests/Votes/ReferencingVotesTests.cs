using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NetTally.Enums;
using NetTally.Tally.Components;
using NetTally.VoteCounting;
using NetTally.Votes;

namespace NetTally.Tests.Votes
{
    [TestClass]
    public class ReferencingVotesTests
    {
        #region Setup
        static IServiceProvider serviceProvider = null!;
        static Quest quest = null!;

        [ClassInitialize]
        public static void ClassInit(TestContext _)
        {
            serviceProvider = TestStartup.ConfigureServices();
        }

        [TestInitialize]
        public void Initialize()
        {
            quest = new Quest
            {
                VoteCounter = serviceProvider.GetRequiredService<IVoteCounter>()
            };
        }

        [TestCleanup]
        public void TestCleanup()
        {
            quest.CaseIsSignificant = false;
            quest.WhitespaceAndPunctuationIsSignificant = false;
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
        readonly static string proposeBiking =
@"[X] Proposed plan: Mountain biking
-[x] Camelback Mountain
-[x] Grand Canyon";
        readonly static string scoreBiking = @"[75%] Plan Mountain biking";

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



        private static void Verify_VotesBothSupport(Post post1, Post post2)
        {
            List<Post> posts = [post1, post2];

            quest.VoteCounter.AddPosts(posts);
            quest.VoteCounter.AddReferenceVoter(post1.Origin);
            quest.VoteCounter.AddReferenceVoter(post2.Origin);

            var processed = VoteConstructor.TryProcessPostGetVotes(post1, quest, out var votes1);

            Assert.IsTrue(processed);

            quest.VoteCounter.AddVotes(votes1, post1.Origin);

            processed = VoteConstructor.TryProcessPostGetVotes(post2, quest, out var votes2);

            Assert.IsTrue(processed);

            quest.VoteCounter.AddVotes(votes2, post2.Origin);

            Assert.IsTrue(votes1.Count > 0);
            Assert.IsTrue(votes2.Count > 0);
            Assert.IsTrue(votes1[0].Lines.Count > 0);
            Assert.IsTrue(votes2[0].Lines.Count > 0);

            Assert.AreEqual(2, quest.VoteCounter.VoteStorage.GetSupportCountFor(votes1[0]));
        }

        private static VoteLine? GetFirstLine(VoteLineBlock block)
        {
            return block.Lines.FirstOrDefault();
        }


        [TestMethod]
        public void Simple_Reference()
        {
            quest.PartitionMode = PartitionMode.ByLine;
            quest.DisableProxyVotes = false;

            Post post1 = GetPostFromKinematics1(oneLine);
            Post post2 = GetPostFromAtreya(refKinematics);

            Verify_VotesBothSupport(post1, post2);
        }

        [TestMethod]
        public void Simple_Reference_Marker_Percent()
        {
            quest.PartitionMode = PartitionMode.ByLine;
            quest.DisableProxyVotes = false;

            Post post1 = GetPostFromKinematics1(oneLine);
            Post post2 = GetPostFromAtreya(refKinematicsPercent);

            Verify_VotesBothSupport(post1, post2);
        }

        [TestMethod]
        public void Simple_Reference_Marker_Approve()
        {
            quest.PartitionMode = PartitionMode.ByLine;
            quest.DisableProxyVotes = false;

            Post post1 = GetPostFromKinematics1(oneLine);
            Post post2 = GetPostFromAtreya(refKinematicsApprove);

            Verify_VotesBothSupport(post1, post2);
        }

        [TestMethod]
        public void Simple_Reference_Task()
        {
            quest.PartitionMode = PartitionMode.ByLine;
            quest.DisableProxyVotes = false;

            Post post1 = GetPostFromAtreya(oneLineTask);
            Post post2 = GetPostFromKimberly(refAtreya);

            Verify_VotesBothSupport(post1, post2);
        }

        [TestMethod]
        public void Simple_Reference_Task_Marker_Percent()
        {
            quest.PartitionMode = PartitionMode.ByLine;
            quest.DisableProxyVotes = false;

            Post post1 = GetPostFromAtreya(oneLineTask);
            Post post2 = GetPostFromKimberly(refAtreyaPercent);

            Verify_VotesBothSupport(post1, post2);
        }

        [TestMethod]
        public void Simple_Reference_Task_Marker_Approve()
        {
            quest.PartitionMode = PartitionMode.ByLine;
            quest.DisableProxyVotes = false;

            Post post1 = GetPostFromAtreya(oneLineTask);
            Post post2 = GetPostFromKimberly(refAtreyaApprove);

            Verify_VotesBothSupport(post1, post2);
        }

        [TestMethod]
        public void Reference_Self_NotAllowed()
        {
            quest.PartitionMode = PartitionMode.ByLine;
            quest.DisableProxyVotes = false;

            Post post1 = GetPostFromAtreya(twoLine);
            Post post2 = GetPostFromKimberly(refKimberly);

            List<Post> posts = [post1, post2];

            quest.VoteCounter.AddPosts(posts);
            quest.VoteCounter.AddReferenceVoter(post1.Origin);
            quest.VoteCounter.AddReferenceVoter(post2.Origin);

            var processed = VoteConstructor.TryProcessPostGetVotes(post1, quest, out var votes1);

            Assert.IsTrue(processed);

            quest.VoteCounter.AddVotes(votes1, post1.Origin);

            processed = VoteConstructor.TryProcessPostGetVotes(post2, quest, out var votes2);

            Assert.IsFalse(processed);

            Assert.AreEqual(0, votes2.Count);

            quest.VoteCounter.AddVotes(votes2, post2.Origin);

            Assert.AreEqual(1, quest.VoteCounter.VoteStorage.GetSupportCountFor(votes1[0]));
        }

        [TestMethod]
        public void Reference_Proxy_Disabled()
        {
            quest.PartitionMode = PartitionMode.ByLine;
            quest.DisableProxyVotes = true;

            Post post1 = GetPostFromKimberly(twoLine);
            Post post2 = GetPostFromKinematics1(refKimberly);

            List<Post> posts = [post1, post2];

            quest.VoteCounter.AddPosts(posts);
            quest.VoteCounter.AddReferenceVoter(post1.Origin);
            quest.VoteCounter.AddReferenceVoter(post2.Origin);

            var processed = VoteConstructor.TryProcessPostGetVotes(post1, quest, out var votes1);

            Assert.IsTrue(processed);

            quest.VoteCounter.AddVotes(votes1, post1.Origin);

            processed = VoteConstructor.TryProcessPostGetVotes(post2, quest, out var votes2);

            Assert.IsTrue(processed);

            Assert.AreNotEqual(votes1.Count, votes2.Count);

            quest.VoteCounter.AddVotes(votes2, post2.Origin);

            Assert.AreEqual(1, quest.VoteCounter.VoteStorage.GetSupportCountFor(votes1[0]));
        }

        [TestMethod]
        public void Reference_DoesNotExist()
        {
            quest.PartitionMode = PartitionMode.ByLine;
            quest.DisableProxyVotes = false;

            Post post1 = GetPostFromAtreya(twoLine);
            Post post2 = GetPostFromKinematics1(refKimberlyPercent);

            List<Post> posts = [post1, post2];

            quest.VoteCounter.AddPosts(posts);
            quest.VoteCounter.AddReferenceVoter(post1.Origin);
            quest.VoteCounter.AddReferenceVoter(post2.Origin);

            var processed = VoteConstructor.TryProcessPostGetVotes(post1, quest, out var votes1);

            Assert.IsTrue(processed);

            quest.VoteCounter.AddVotes(votes1, post1.Origin);

            processed = VoteConstructor.TryProcessPostGetVotes(post2, quest, out var votes2);

            Assert.IsTrue(processed);

            Assert.AreNotEqual(votes1.Count, votes2.Count);

            quest.VoteCounter.AddVotes(votes2, post2.Origin);

            Assert.AreEqual(1, quest.VoteCounter.VoteStorage.GetSupportCountFor(votes1[0]));
        }

        [TestMethod]
        public void Implicit_Plan_Name_Ref()
        {
            quest.PartitionMode = PartitionMode.ByBlock;
            quest.DisableProxyVotes = false;

            Post post1 = GetPostFromKimberly(implicitPlan);
            Post post2 = GetPostFromAtreya(refKimberlyApprove);

            List<Post> posts = [post1, post2];

            quest.VoteCounter.AddPosts(posts);

            Tallyer.PreprocessPosts(quest);

            var processed = VoteConstructor.TryProcessPostGetVotes(post1, quest, out var votes1);

            Assert.IsTrue(processed);

            quest.VoteCounter.AddVotes(votes1, post1.Origin);

            processed = VoteConstructor.TryProcessPostGetVotes(post2, quest, out var votes2);

            Assert.IsTrue(processed);

            quest.VoteCounter.AddVotes(votes2, post2.Origin);

            Assert.AreEqual(votes1.Count, votes2.Count);
            Assert.AreEqual(GetFirstLine(votes1[0]), GetFirstLine(votes2[0]));

            Assert.AreEqual(2, quest.VoteCounter.VoteStorage.GetSupportCountFor(votes1[0]));
        }


        [TestMethod]
        public void Explicit_Plan_Ref()
        {
            quest.PartitionMode = PartitionMode.ByBlock;
            quest.DisableProxyVotes = false;

            Post post1 = GetPostFromKimberly(explicitPlan);
            Post post2 = GetPostFromAtreya(oneLine);

            List<Post> posts = [post1, post2];

            quest.VoteCounter.AddPosts(posts);
            Tallyer.PreprocessPosts(quest);

            var processed = VoteConstructor.TryProcessPostGetVotes(post1, quest, out var votes1);

            Assert.IsTrue(processed);

            quest.VoteCounter.AddVotes(votes1, post1.Origin);

            processed = VoteConstructor.TryProcessPostGetVotes(post2, quest, out var votes2);

            Assert.IsTrue(processed);

            quest.VoteCounter.AddVotes(votes2, post2.Origin);

            Assert.IsTrue(votes1.Count > 0);
            Assert.IsTrue(votes2.Count > 0);
            Assert.IsTrue(votes1[0].Lines.Count > 0);
            Assert.IsTrue(votes2[0].Lines.Count > 0);

            Assert.AreEqual(2, quest.VoteCounter.VoteStorage.GetSupportCountFor(votes1[0]));
        }

        [TestMethod]
        public void Explicit_Plan_TwoChunk_Ref()
        {
            quest.PartitionMode = PartitionMode.ByBlock;
            quest.DisableProxyVotes = false;

            Post post1 = GetPostFromKimberly(twoChunkPlan);
            Post post2 = GetPostFromKinematics2(oneLine);

            List<Post> posts = [post1, post2];

            quest.VoteCounter.AddPosts(posts);

            Tallyer.PreprocessPosts(quest);

            var processed = VoteConstructor.TryProcessPostGetVotes(post1, quest, out var votes1);

            Assert.IsTrue(processed);

            quest.VoteCounter.AddVotes(votes1, post1.Origin);

            processed = VoteConstructor.TryProcessPostGetVotes(post2, quest, out var votes2);

            Assert.IsTrue(processed);

            quest.VoteCounter.AddVotes(votes2, post2.Origin);

            Assert.IsTrue(votes1.Count > 0);
            Assert.IsTrue(votes2.Count > 0);
            Assert.IsTrue(votes1[0].Lines.Count > 0);
            Assert.IsTrue(votes2[0].Lines.Count > 0);

            Assert.AreEqual(2, quest.VoteCounter.VoteStorage.GetSupportCountFor(votes1[0]));
        }

        [TestMethod]
        public void Implicit_Plan_Ref()
        {
            quest.PartitionMode = PartitionMode.None;
            quest.DisableProxyVotes = false;

            Post post1 = GetPostFromKimberly(implicitPlan);
            Post post2 = GetPostFromAtreya(oneLine);

            List<Post> posts = [post1, post2];

            quest.VoteCounter.AddPosts(posts);

            Tallyer.PreprocessPosts(quest);

            var processed = VoteConstructor.TryProcessPostGetVotes(post1, quest, out var votes1);

            Assert.IsTrue(processed);

            quest.VoteCounter.AddVotes(votes1, post1.Origin);

            processed = VoteConstructor.TryProcessPostGetVotes(post2, quest, out var votes2);

            Assert.IsTrue(processed);

            quest.VoteCounter.AddVotes(votes2, post2.Origin);

            Assert.IsTrue(votes1.Count > 0);
            Assert.IsTrue(votes2.Count > 0);
            Assert.IsTrue(votes1[0].Lines.Count > 0);
            Assert.IsTrue(votes2[0].Lines.Count > 0);

            Assert.AreEqual(2, quest.VoteCounter.VoteStorage.GetSupportCountFor(votes1[0]));
        }

        [TestMethod]
        public void Implicit_Plan_Block_Ref()
        {
            quest.PartitionMode = PartitionMode.ByBlock;
            quest.DisableProxyVotes = false;

            Post post1 = GetPostFromKimberly(implicitPlan);
            Post post2 = GetPostFromAtreya(oneLine);

            List<Post> posts = [post1, post2];

            quest.VoteCounter.AddPosts(posts);

            Tallyer.PreprocessPosts(quest);

            var processed = VoteConstructor.TryProcessPostGetVotes(post1, quest, out var votes1);

            Assert.IsTrue(processed);

            quest.VoteCounter.AddVotes(votes1, post1.Origin);

            processed = VoteConstructor.TryProcessPostGetVotes(post2, quest, out var votes2);

            Assert.IsTrue(processed);

            quest.VoteCounter.AddVotes(votes2, post2.Origin);

            Assert.IsTrue(votes1.Count > 0);
            Assert.IsTrue(votes2.Count > 0);
            Assert.IsTrue(votes1[0].Lines.Count > 0);
            Assert.IsTrue(votes2[0].Lines.Count > 0);

            Assert.AreEqual(2, quest.VoteCounter.VoteStorage.GetSupportCountFor(votes1[0]));
        }


        [TestMethod]
        public void Cross_Marker_Reference_Plan()
        {
            quest.PartitionMode = PartitionMode.ByBlock;
            quest.DisableProxyVotes = false;

            Post post1 = GetPostFromKinematics1(proposeBiking);
            Post post2 = GetPostFromKinematics2(scoreBiking);

            List<Post> posts = [post1, post2];

            quest.VoteCounter.AddPosts(posts);

            Tallyer.PreprocessPosts(quest);

            var processed = VoteConstructor.TryProcessPostGetVotes(post1, quest, out var votes1);

            Assert.IsTrue(processed);

            quest.VoteCounter.AddVotes(votes1, post1.Origin);

            processed = VoteConstructor.TryProcessPostGetVotes(post2, quest, out var votes2);

            Assert.IsTrue(processed);

            quest.VoteCounter.AddVotes(votes2, post2.Origin);


            Assert.AreEqual(0, votes1.Count);
            Assert.AreEqual(1, votes2.Count);

            Assert.AreEqual(1, quest.VoteCounter.VoteStorage.GetSupportCountFor(votes2[0]));
            Assert.AreEqual(2, quest.VoteCounter.VoteStorage.GetSupportersFor(votes2[0])?.Count ?? 0);
            Assert.AreEqual(1, quest.VoteCounter.VoteStorage.GetVotesBy(post2.Origin).Count);

            var allVotes = quest.VoteCounter.GetAllVotes();
            Assert.AreEqual(1, allVotes.Count());

            Assert.AreEqual(MarkerType.Score, allVotes.First().Category);
        }
    }
}
