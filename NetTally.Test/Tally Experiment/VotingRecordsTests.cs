using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NetTally.Tests.Platform;
using NetTally.Votes.Experiment;
using NetTally.Utility;
using NetTally.ViewModels;
using NetTally.Votes;

namespace NetTally.Tests
{
    [TestClass]
    public class VotingRecordsTests
    {
        static Identity defaultIdentity;
        static VotePartition defaultPlanPartition;

        static List<string> notifications = new List<string>();

        static IQuest quest = new Quest();

        #region Setup
        [ClassInitialize]
        public static void ClassInit(TestContext context)
        {
            Agnostic.HashStringsUsing(UnicodeHashFunction.HashFunction);
            ViewModelService.Instance.Build();
            VotingRecords.Instance.PropertyChanged += Instance_PropertyChanged;

            defaultIdentity = new Identity("Name", "1");
            defaultPlanPartition = new VotePartition(new VoteLine("[X] Plan Name\n-[X] Some content"), VoteType.Plan);
        }

        [TestInitialize]
        public void Initialize()
        {
            VotingRecords.Instance.Reset();
            VotingRecords.Instance.ResetUserDefinedTasks();
            quest = new Quest();
            notifications.Clear();
        }

        private static void Instance_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            notifications.Add(e.PropertyName);
        }
        #endregion


        #region Reset
        [TestMethod]
        public async Task Reset1()
        {
            await AddVotes_shim();
            VotingRecords.Instance.Reset();

            Assert.IsFalse(VotingRecords.Instance.Tasks.Any());
        }
        #endregion

        #region Add Posts
        [TestMethod]
        public void AddPosts1()
        {
            var posts = GetPosts1();
            VotingRecords.Instance.UsePostsForTally(posts);

            Assert.AreEqual(posts.Count, VotingRecords.Instance.PostsList.Count);
            Assert.IsTrue(posts.SequenceEqual(VotingRecords.Instance.PostsList));
        }
        #endregion

        #region Query voter identities
        [TestMethod]
        public void QueryVoter1()
        {
            var posts = GetPosts1();
            VotingRecords.Instance.UsePostsForTally(posts);

            Assert.IsTrue(VotingRecords.Instance.HasVoterName("Kinematics"));
            Assert.IsTrue(VotingRecords.Instance.HasVoterName("Aedyn"));
            Assert.IsTrue(VotingRecords.Instance.HasVoterName("Minas"));
            Assert.IsTrue(VotingRecords.Instance.HasVoterName("Ψion"));
            Assert.IsTrue(VotingRecords.Instance.HasVoterName("Yan-de"));
        }

        [TestMethod]
        public void QueryVoter2()
        {
            var posts = GetPosts1();
            VotingRecords.Instance.UsePostsForTally(posts);

            Assert.IsTrue(VotingRecords.Instance.HasVoterName("kinematics"));
            Assert.IsTrue(VotingRecords.Instance.HasVoterName("aedyn"));
            Assert.IsTrue(VotingRecords.Instance.HasVoterName("MINAS"));
            Assert.IsTrue(VotingRecords.Instance.HasVoterName("Ψion"));
            Assert.IsTrue(VotingRecords.Instance.HasVoterName("yande"));
            Assert.IsFalse(VotingRecords.Instance.HasVoterName("kinematic"));
            Assert.IsFalse(VotingRecords.Instance.HasVoterName("aedyn1"));
            Assert.IsFalse(VotingRecords.Instance.HasVoterName("MINASES"));
            Assert.IsFalse(VotingRecords.Instance.HasVoterName("psion"));
        }

        [TestMethod]
        public void QueryVoter3()
        {
            var posts = GetPosts1();
            VotingRecords.Instance.UsePostsForTally(posts);

            var idents = VotingRecords.Instance.GetVoterIdentities("Kinematics");

            Assert.AreEqual(2, idents.Count);

            var ident = idents.First();

            Assert.AreEqual("Kinematics", ident.Name);
        }

        [TestMethod]
        public void QueryVoter4()
        {
            var posts = GetPosts1();
            VotingRecords.Instance.UsePostsForTally(posts);

            var ident = VotingRecords.Instance.GetLastVoterIdentity("Kinematics");

            Assert.AreEqual("Kinematics", ident.Name);
            Assert.AreEqual("12348", ident.PostID);
        }

        #endregion

        #region Add Plans
        [TestMethod]
        public void AddPlan_1()
        {
            Plan plan = new Plan("Name", defaultIdentity, defaultPlanPartition, PlanType.Content);
            var plans = new List<Plan> { plan };
            Dictionary<string, List<Plan>> planDict = new Dictionary<string, List<Plan>> { ["Name"] = plans };
            VotingRecords.Instance.AddPlans(planDict);
            Assert.IsTrue(VotingRecords.Instance.HasPlanName("Name"));
            Assert.IsFalse(VotingRecords.Instance.HasPlanName("Names"));
            Assert.AreEqual("Name", VotingRecords.Instance.GetPlanName("Name"));
            Assert.AreEqual(null, VotingRecords.Instance.GetPlanName("Names"));
        }

        [TestMethod]
        public void AddPlan_2()
        {
            Plan plan = new Plan("Name", defaultIdentity, defaultPlanPartition, PlanType.Content);
            var plans = new List<Plan> { plan };
            Dictionary<string, List<Plan>> planDict = new Dictionary<string, List<Plan>> { ["Name"] = plans };
            VotingRecords.Instance.AddPlans(planDict);
            Assert.IsTrue(VotingRecords.Instance.HasPlanName("name"));
            Assert.IsFalse(VotingRecords.Instance.HasPlanName("names"));
            Assert.AreEqual("Name", VotingRecords.Instance.GetPlanName("name"));
            Assert.AreEqual(null, VotingRecords.Instance.GetPlanName("names"));
        }

        [TestMethod]
        public void AddPlan_3()
        {
            Plan plan = new Plan("Name", defaultIdentity, defaultPlanPartition, PlanType.Content);
            var plans = new List<Plan> { plan };
            Dictionary<string, List<Plan>> planDict = new Dictionary<string, List<Plan>> { ["Name"] = plans };
            VotingRecords.Instance.AddPlans(planDict);
            Assert.IsTrue(VotingRecords.Instance.HasPlanName("NAME"));
            Assert.IsFalse(VotingRecords.Instance.HasPlanName("NAMES"));
            Assert.AreEqual("Name", VotingRecords.Instance.GetPlanName("NAME"));
            Assert.AreEqual(null, VotingRecords.Instance.GetPlanName("NAMES"));
        }

        [TestMethod]
        public void AddPlan_4()
        {
            Plan plan = new Plan("Name", defaultIdentity, defaultPlanPartition, PlanType.Content);
            var plans = new List<Plan> { plan };
            Dictionary<string, List<Plan>> planDict = new Dictionary<string, List<Plan>> { ["Name"] = plans };
            VotingRecords.Instance.AddPlans(planDict);
            Assert.IsTrue(VotingRecords.Instance.HasPlanName("n-ame"));
            Assert.IsFalse(VotingRecords.Instance.HasPlanName("n-ames"));
            Assert.AreEqual("Name", VotingRecords.Instance.GetPlanName("n-ame"));
            Assert.AreEqual(null, VotingRecords.Instance.GetPlanName("n-ames"));
        }

        [TestMethod]
        public void AddPlan_5()
        {
            Plan plan = new Plan("N'ame", defaultIdentity, defaultPlanPartition, PlanType.Content);
            var plans = new List<Plan> { plan };
            Dictionary<string, List<Plan>> planDict = new Dictionary<string, List<Plan>> { ["N'ame"] = plans };
            VotingRecords.Instance.AddPlans(planDict);
            Assert.IsTrue(VotingRecords.Instance.HasPlanName("Name"));
            Assert.IsFalse(VotingRecords.Instance.HasPlanName("Names"));
            Assert.AreEqual("N'ame", VotingRecords.Instance.GetPlanName("Name"));
            Assert.AreEqual(null, VotingRecords.Instance.GetPlanName("Names"));
        }

        [TestMethod]
        public void AddPlan_6()
        {
            Plan plan = new Plan("N'ame", defaultIdentity, defaultPlanPartition, PlanType.Content);
            var plans = new List<Plan> { plan };
            Dictionary<string, List<Plan>> planDict = new Dictionary<string, List<Plan>> { ["N'ame"] = plans };
            VotingRecords.Instance.AddPlans(planDict);
            Assert.IsTrue(VotingRecords.Instance.HasPlanName("name"));
            Assert.IsFalse(VotingRecords.Instance.HasPlanName("names"));
            Assert.AreEqual("N'ame", VotingRecords.Instance.GetPlanName("NAME"));
            Assert.AreEqual(null, VotingRecords.Instance.GetPlanName("names"));
        }
        #endregion

        #region Query plans
        #endregion

        #region Tasks
        [TestMethod]
        public void UserTask1()
        {
            VotingRecords.Instance.AddUserTask("Testing");

            Assert.IsTrue(VotingRecords.Instance.Tasks.Contains("Testing"));

            VotingRecords.Instance.ResetUserDefinedTasks();

            Assert.IsFalse(VotingRecords.Instance.Tasks.Contains("Testing"));
        }
        #endregion

        #region Add votes
        public async Task AddVotes_shim()
        {
            var posts = GetPosts1();
            VotingRecords.Instance.UsePostsForTally(posts);

            await VotingCounter.Instance.CountVotesInPosts(quest, GetPosts1(), CancellationToken.None);
        }

        [TestMethod]
        [Ignore]
        public async Task RunVotes1()
        {
            await AddVotes_shim();

            var votes = VotingRecords.Instance.GetVoteEntries(VoteType.Vote);

            Assert.AreEqual(5, votes.Count);

            Assert.IsTrue(VotingRecords.Instance.Tasks.Contains("Do it?"));
            Assert.IsTrue(VotingRecords.Instance.Tasks.Contains("Where"));

            Assert.IsFalse(VotingRecords.Instance.HasUndoItems);
        }

        [TestMethod]
        [Ignore]
        public async Task RunVotes2()
        {
            quest.PartitionMode = PartitionMode.ByLine;

            await AddVotes_shim();

            var votes = VotingRecords.Instance.GetVoteEntries(VoteType.Vote);

            Assert.AreEqual(7, votes.Count);

            var f = votes.First(v => v.Key.Task == "Where");
            Assert.AreEqual("Yan-de", f.Value.First().Name);

            Assert.IsTrue(VotingRecords.Instance.Tasks.Contains("Do it?"));
            Assert.IsTrue(VotingRecords.Instance.Tasks.Contains("Where"));

            Assert.IsFalse(VotingRecords.Instance.HasUndoItems);
        }
        #endregion

        #region Get votes for identity
        [TestMethod]
        [Ignore]
        public async Task GetIdentityVotes1()
        {
            quest.PartitionMode = PartitionMode.ByLine;

            await AddVotes_shim();

            var ident = VotingRecords.Instance.GetLastVoterIdentity("Kinematics");

            var vs = VotingRecords.Instance.GetPartitionsForIdentity(ident);

            Assert.AreEqual(2, vs.Count);
        }
        #endregion

        #region Manage votes
        #endregion

        #region Undo
        #endregion


        #region Posts
        private List<Post> GetPosts1()
        {
            List<Post> posts = new List<Post>();

            Post post;

            post = new Post("Kinematics", "12345", 3,
@"Intro
[x] Test 1
[x] Test 2");

            posts.Add(post);

            post = new Post("Aedyn", "12346", 4,
@"Intro
[x] Test 1
[x] Test 2");

            posts.Add(post);

            post = new Post("Minas", "12347", 5,
@"Intro
[x] Maybe
[x] Test 2");

            posts.Add(post);

            post = new Post("Ψion", "12348", 6,
@"Intro
[x][Do it?] Maybe
[x] Test 2");

            posts.Add(post);

            post = new Post("Yan-de", "12347", 7,
@"Intro
[x][Where] Somehwere
[x] Test 3");

            posts.Add(post);

            post = new Post("Kinematics", "12348", 8,
@"Changed my mind
[x] Test 3
[x] Test 4");

            posts.Add(post);

            return posts;
        }
        #endregion
    }
}
