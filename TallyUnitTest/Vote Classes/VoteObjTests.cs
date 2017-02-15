using System;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NetTally.Votes;
using NetTally.Votes.Experiment;

namespace NetTally.Tests
{
    [TestClass]
    public class VoteObjTests
    {
        [TestMethod]
        public void EmptyPost()
        {
            Post post = new Post("Kinematics", "12345", 12,
@""
);
            Assert.AreEqual("Kinematics", post.Identity.Name);
            Assert.AreEqual("12345", post.Identity.PostID);
            Assert.AreEqual(12345, post.Identity.PostIDValue);
            Assert.AreEqual(12, post.ThreadPostNumber);
            Assert.IsFalse(post.HasVote);
            Assert.IsNull(post.Vote);
        }

        [TestMethod]
        public void NormalPost()
        {
            Post post = new Post("Kinematics", "12345", 12,
@"Some chatty response about various things that might include a description of [x] how to write a vote.
Make sure it's OK."
);
            Assert.AreEqual("Kinematics", post.Identity.Name);
            Assert.AreEqual("12345", post.Identity.PostID);
            Assert.AreEqual(12345, post.Identity.PostIDValue);
            Assert.AreEqual(12, post.ThreadPostNumber);
            Assert.IsFalse(post.HasVote);
        }

        [TestMethod]
        [ExpectedException(typeof(InvalidOperationException))]
        public void NormalPostExc()
        {
            Post post = new Post("Kinematics", "12345", 12,
@"Some chatty response about various things that might include a description of [x] how to write a vote.
Make sure it's OK."
);
            Assert.AreEqual("Kinematics", post.Identity.Name);
            Assert.AreEqual("12345", post.Identity.PostID);
            Assert.AreEqual(12345, post.Identity.PostIDValue);
            Assert.AreEqual(12, post.ThreadPostNumber);
            Assert.IsFalse(post.HasVote);
            var plans = Plan.GetPlansFromVote(post.Vote);
        }

        [TestMethod]
        public void TallyPost()
        {
            Post post = new Post("Kinematics", "12345", 12,
@"A post that also has some stuff that I don't want to be tallied.
[x] If you vote this way
[x] Or if you vote that way

##### - not a vote

I don't want this to be counted."
);
            Assert.AreEqual("Kinematics", post.Identity.Name);
            Assert.AreEqual("12345", post.Identity.PostID);
            Assert.AreEqual(12345, post.Identity.PostIDValue);
            Assert.AreEqual(12, post.ThreadPostNumber);
            Assert.IsFalse(post.HasVote);
        }

        [TestMethod]
        public void NormalVote()
        {
            Post post = new Post("Kinematics", "12345", 12,
@"A post that has some comments and a vote.
[x] If you vote this way
[x] Or if you vote that way"
);
            Assert.AreEqual("Kinematics", post.Identity.Name);
            Assert.AreEqual("12345", post.Identity.PostID);
            Assert.AreEqual(12345, post.Identity.PostIDValue);
            Assert.AreEqual(12, post.ThreadPostNumber);
            Assert.IsTrue(post.HasVote);
            Assert.AreEqual(2, post.Vote.VoteLines.Count);
            var plans = Plan.GetPlansFromVote(post.Vote);
            Assert.AreEqual(0, plans.Count);
        }

        [TestMethod]
        public void PlanWithContent()
        {
            Post post = new Post("Kinematics", "12345", 12,
@"A post that has some comments and a vote.
[x] Plan getaway
-[x] If you vote this way
-[x] Or if you vote that way"
);
            Assert.AreEqual("Kinematics", post.Identity.Name);
            Assert.AreEqual("12345", post.Identity.PostID);
            Assert.AreEqual(12345, post.Identity.PostIDValue);
            Assert.AreEqual(12, post.ThreadPostNumber);
            Assert.IsTrue(post.HasVote);
            Assert.AreEqual(3, post.Vote.VoteLines.Count);
            var plans = Plan.GetPlansFromVote(post.Vote);
            Assert.AreEqual(1, plans.Count);
            Assert.AreEqual("getaway", plans[0].Identity.Name);
            Assert.AreEqual(3, plans[0].Content.VoteLines.Count);
            Assert.AreEqual(PlanType.Content, plans[0].PlanType);
        }

        [TestMethod]
        public void LabeledPlan()
        {
            Post post = new Post("Kinematics", "12345", 12,
@"A post that has some comments and a vote.
[x] Plan getaway
[x] If you vote this way
[x] Or if you vote that way"
);
            Assert.AreEqual("Kinematics", post.Identity.Name);
            Assert.AreEqual("12345", post.Identity.PostID);
            Assert.AreEqual(12345, post.Identity.PostIDValue);
            Assert.AreEqual(12, post.ThreadPostNumber);
            Assert.IsTrue(post.HasVote);
            Assert.AreEqual(3, post.Vote.VoteLines.Count);
            var plans = Plan.GetPlansFromVote(post.Vote);
            Assert.AreEqual(1, plans.Count);
            Assert.AreEqual("getaway", plans[0].Identity.Name);
            Assert.AreEqual(3, plans[0].Content.VoteLines.Count);
            Assert.AreEqual(PlanType.Label, plans[0].PlanType);
        }

        [TestMethod]
        public void ContentPlanAndMore()
        {
            Post post = new Post("Kinematics", "12345", 12,
@"A post that has some comments and a vote.
[x] Plan getaway
-[x] If you vote this way
-[x] Or if you vote that way
[x] And some other stuff"
);
            Assert.AreEqual("Kinematics", post.Identity.Name);
            Assert.AreEqual("12345", post.Identity.PostID);
            Assert.AreEqual(12345, post.Identity.PostIDValue);
            Assert.AreEqual(12, post.ThreadPostNumber);
            Assert.IsTrue(post.HasVote);
            Assert.AreEqual(4, post.Vote.VoteLines.Count);

            var plans = Plan.GetPlansFromVote(post.Vote);
            Assert.AreEqual(1, plans.Count);

            Assert.AreEqual("getaway", plans[0].Identity.Name);
            Assert.AreEqual(3, plans[0].Content.VoteLines.Count);
            Assert.AreEqual(PlanType.Content, plans[0].PlanType);
        }

        [TestMethod]
        public void MultiPlan()
        {
            Post post = new Post("Kinematics", "12345", 12,
@"A post that has some comments and a vote.
[x] Plan getaway
-[x] If you vote this way
-[x] Or if you vote that way
[x] Plan Hideaway
-[x] Under the bed
[x] And some other stuff"
);
            Assert.AreEqual("Kinematics", post.Identity.Name);
            Assert.AreEqual("12345", post.Identity.PostID);
            Assert.AreEqual(12345, post.Identity.PostIDValue);
            Assert.AreEqual(12, post.ThreadPostNumber);
            Assert.IsTrue(post.HasVote);
            Assert.AreEqual(6, post.Vote.VoteLines.Count);

            var plans = Plan.GetPlansFromVote(post.Vote);
            Assert.AreEqual(2, plans.Count);

            Assert.AreEqual("getaway", plans[0].Identity.Name);
            Assert.AreEqual(3, plans[0].Content.VoteLines.Count);
            Assert.AreEqual(PlanType.Content, plans[0].PlanType);

            Assert.AreEqual("Hideaway", plans[1].Identity.Name);
            Assert.AreEqual(2, plans[1].Content.VoteLines.Count);
            Assert.AreEqual(PlanType.Content, plans[1].PlanType);
        }

        [TestMethod]
        public void MultiPlan2()
        {
            Post post = new Post("Kinematics", "12345", 12,
@"A post that has some comments and a vote.
[x] Base plan Bii
-[x] Stuffs

[x] Plan getaway
-[x] If you vote this way
-[x] Or if you vote that way
[x] Plan Hideaway
-[x] Under the bed
[x] And some other stuff
[x] plan Bii"
);
            Assert.AreEqual("Kinematics", post.Identity.Name);
            Assert.AreEqual("12345", post.Identity.PostID);
            Assert.AreEqual(12345, post.Identity.PostIDValue);
            Assert.AreEqual(12, post.ThreadPostNumber);
            Assert.IsTrue(post.HasVote);
            Assert.AreEqual(9, post.Vote.VoteLines.Count);
            var plans = Plan.GetPlansFromVote(post.Vote);
            Assert.AreEqual(3, plans.Count);

            Assert.AreEqual("Bii", plans[0].Identity.Name);
            Assert.AreEqual(2, plans[0].Content.VoteLines.Count);
            Assert.AreEqual(PlanType.Base, plans[0].PlanType);

            Assert.AreEqual("getaway", plans[1].Identity.Name);
            Assert.AreEqual(3, plans[1].Content.VoteLines.Count);
            Assert.AreEqual(PlanType.Content, plans[1].PlanType);

            Assert.AreEqual("Hideaway", plans[2].Identity.Name);
            Assert.AreEqual(2, plans[2].Content.VoteLines.Count);
            Assert.AreEqual(PlanType.Content, plans[2].PlanType);
        }

        [TestMethod]
        public void ApprovalPlan()
        {
            Post post = new Post("Kinematics", "12345", 12,
@"A post that has some comments and a vote.
[+] Plan getaway
-[+] If you vote this way
-[+] Or if you vote that way
[+] And some other stuff"
);
            Assert.AreEqual("Kinematics", post.Identity.Name);
            Assert.AreEqual("12345", post.Identity.PostID);
            Assert.AreEqual(12345, post.Identity.PostIDValue);
            Assert.AreEqual(12, post.ThreadPostNumber);
            Assert.IsTrue(post.HasVote);
            Assert.AreEqual(4, post.Vote.VoteLines.Count);

            var plans = Plan.GetPlansFromVote(post.Vote);
            Assert.AreEqual(1, plans.Count);

            Assert.AreEqual("getaway", plans[0].Identity.Name);
            Assert.AreEqual(3, plans[0].Content.VoteLines.Count);
            Assert.AreEqual(PlanType.Content, plans[0].PlanType);
        }

        [TestMethod]
        public void ContentPlanAndRankings()
        {
            Post post = new Post("Kinematics", "12345", 12,
@"A post that has some comments and a vote.
[x] Plan getaway
-[x] If you vote this way
-[x] Or if you vote that way

[1] Heavy Armor
[2] Robes
[3] Maid Uniform"
);
            Assert.AreEqual("Kinematics", post.Identity.Name);
            Assert.AreEqual("12345", post.Identity.PostID);
            Assert.AreEqual(12345, post.Identity.PostIDValue);
            Assert.AreEqual(12, post.ThreadPostNumber);
            Assert.IsTrue(post.HasVote);
            Assert.AreEqual(6, post.Vote.VoteLines.Count);

            var plans = Plan.GetPlansFromVote(post.Vote);
            Assert.AreEqual(1, plans.Count);

            Assert.AreEqual("getaway", plans[0].Identity.Name);
            Assert.AreEqual(3, plans[0].Content.VoteLines.Count);
            Assert.AreEqual(PlanType.Content, plans[0].PlanType);
        }

        [TestMethod]
        public void ValueMarkedPlans()
        {
            Post post = new Post("Kinematics", "12345", 12,
@"A post that has some comments and a vote.
[2] Plan getaway
-[*] If you vote this way
-[*] Or if you vote that way

[1] Plan Hideaway
-[*] Under the bed"
);
            Assert.AreEqual("Kinematics", post.Identity.Name);
            Assert.AreEqual("12345", post.Identity.PostID);
            Assert.AreEqual(12345, post.Identity.PostIDValue);
            Assert.AreEqual(12, post.ThreadPostNumber);
            Assert.IsTrue(post.HasVote);
            Assert.AreEqual(5, post.Vote.VoteLines.Count);

            var plans = Plan.GetPlansFromVote(post.Vote);
            Assert.AreEqual(2, plans.Count);

            Assert.AreEqual("getaway", plans[0].Identity.Name);
            Assert.AreEqual(3, plans[0].Content.VoteLines.Count);
            Assert.AreEqual(PlanType.Content, plans[0].PlanType);

            Assert.AreEqual("Hideaway", plans[1].Identity.Name);
            Assert.AreEqual(2, plans[1].Content.VoteLines.Count);
            Assert.AreEqual(PlanType.Content, plans[1].PlanType);

        }

        [TestMethod]
        public void RankedPlans()
        {
            Post post = new Post("Kinematics", "12345", 12,
@"A post that has some comments and a vote.
[#2] Plan getaway
-[*] If you vote this way
-[*] Or if you vote that way

[#1] Plan Hideaway
-[*] Under the bed"
);
            Assert.AreEqual("Kinematics", post.Identity.Name);
            Assert.AreEqual("12345", post.Identity.PostID);
            Assert.AreEqual(12345, post.Identity.PostIDValue);
            Assert.AreEqual(12, post.ThreadPostNumber);
            Assert.IsTrue(post.HasVote);
            Assert.AreEqual(5, post.Vote.VoteLines.Count);

            var plans = Plan.GetPlansFromVote(post.Vote);
            Assert.AreEqual(2, plans.Count);

            Assert.AreEqual("getaway", plans[0].Identity.Name);
            Assert.AreEqual(3, plans[0].Content.VoteLines.Count);
            Assert.AreEqual(PlanType.Content, plans[0].PlanType);

            Assert.AreEqual("Hideaway", plans[1].Identity.Name);
            Assert.AreEqual(2, plans[1].Content.VoteLines.Count);
            Assert.AreEqual(PlanType.Content, plans[1].PlanType);
        }

        [TestMethod]
        public void ScoredPlans()
        {
            Post post = new Post("Kinematics", "12345", 12,
@"A post that has some comments and a vote.
[+7] Plan getaway
-[*] If you vote this way
-[*] Or if you vote that way

[+8] Plan Hideaway
-[*] Under the bed
-[+6] Bring Teddy"
);
            Assert.AreEqual("Kinematics", post.Identity.Name);
            Assert.AreEqual("12345", post.Identity.PostID);
            Assert.AreEqual(12345, post.Identity.PostIDValue);
            Assert.AreEqual(12, post.ThreadPostNumber);
            Assert.IsTrue(post.HasVote);
            Assert.AreEqual(6, post.Vote.VoteLines.Count);

            var plans = Plan.GetPlansFromVote(post.Vote);
            Assert.AreEqual(2, plans.Count);

            Assert.AreEqual("getaway", plans[0].Identity.Name);
            Assert.AreEqual(3, plans[0].Content.VoteLines.Count);
            Assert.AreEqual(PlanType.Content, plans[0].PlanType);

            Assert.AreEqual("Hideaway", plans[1].Identity.Name);
            Assert.AreEqual(3, plans[1].Content.VoteLines.Count);
            Assert.AreEqual(PlanType.Content, plans[1].PlanType);
        }

        [TestMethod]
        public void MixedPlans_Components()
        {
            Post post = new Post("Kinematics", "12345", 12,
@"A post that has some comments and a vote.
[+7] Plan getaway
-[*] If you vote this way
-[*] Or if you vote that way

[+8][Where] Plan Hideaway
-[*] Under the bed
-[+6] Bring Teddy

[#3] Plan Runaway
-[x] Through the woods
-[x] Bring Jane"

);
            Assert.AreEqual("Kinematics", post.Identity.Name);
            Assert.AreEqual("12345", post.Identity.PostID);
            Assert.AreEqual(12345, post.Identity.PostIDValue);
            Assert.AreEqual(12, post.ThreadPostNumber);
            Assert.IsTrue(post.HasVote);
            Assert.AreEqual(9, post.Vote.VoteLines.Count);

            var plans = Plan.GetPlansFromVote(post.Vote);
            Assert.AreEqual(3, plans.Count);

            var componentsByBlock = post.Vote.GetComponents(PartitionMode.ByBlock);
            var componentsByLine = post.Vote.GetComponents(PartitionMode.ByLine);

            Assert.AreEqual(3, componentsByBlock.Count);
            Assert.AreEqual(7, componentsByLine.Count);
            Assert.AreEqual("Where", componentsByLine[4][0].Task);
            Assert.AreEqual(8, componentsByLine[4][0].MarkerValue);
        }

    }
}
