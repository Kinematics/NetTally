using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NetTally.Forums;

namespace NetTally.Tests.Forums
{
    [TestClass]
    public class PostTests
    {
        #region Setup
        static IServiceProvider serviceProvider;
        static readonly Origin origin = new Origin("Kinematics", "123456", 10, new Uri("http://www.example.com/"), "http://www.example.com");

        [ClassInitialize]
        public static void ClassInit(TestContext context)
        {
            serviceProvider = TestStartup.ConfigureServices();
        }

        [TestInitialize]
        public void Initialize()
        {
        }
        #endregion


        #region General failures
        [TestMethod]
        [ExpectedException(typeof(ArgumentNullException))]
        public void Construct_Fail_NullText()
        {
            _ = new Post(Origin.Empty, null);
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentNullException))]
        public void Construct_Fail_NullOrigin()
        {
            _ = new Post(null, "Some text");
        }
        #endregion

        [TestMethod]
        public void Check_That_Class_Does_Not_Modify_Parameters()
        {
            string postText =
@"[x] Line 1
[x] Line 2";

            Post post = new Post(origin, postText);

            Assert.AreEqual(postText, post.Text);
            Assert.AreEqual(origin, post.Origin);
        }

        [TestMethod]
        public void VoteLines_Count_0()
        {
            string postText =
@"A line of discussion, going on about
what we might consider doing.";

            Post post = new Post(origin, postText);

            Assert.AreEqual(0, post.VoteLines.Count);
            Assert.IsFalse(post.HasVote);
        }

        [TestMethod]
        public void VoteLines_Count_Tally_Post()
        {
            string postText =
@"Someone posted a tally:
##### NetTally
[X] A count of votes";

            Post post = new Post(origin, postText);

            Assert.AreEqual(0, post.VoteLines.Count);
            Assert.IsFalse(post.HasVote);
        }

        [TestMethod]
        public void VoteLines_Count_Tally_Invisitext()
        {
            string postText =
@"Someone posted a tally:
『color=Transparent』#『b』####『/b』 NetTally『/color』
[X] A count of votes";

            Post post = new Post(origin, postText);

            Assert.AreEqual(0, post.VoteLines.Count);
            Assert.IsFalse(post.HasVote);
        }

        [TestMethod]
        public void VoteLines_Count_Tally_Invisitext_Bold()
        {
            string postText =
@"Someone posted a tally:
『color=Transparent』##### NetTally『/color』
[X] A count of votes";

            Post post = new Post(origin, postText);

            Assert.AreEqual(0, post.VoteLines.Count);
            Assert.IsFalse(post.HasVote);
        }

        [TestMethod]
        public void Compare_Equal()
        {
            string postText =
@"[x] Line 1
[x] Line 2";

            Origin origin1 = new Origin("Kinematics", "123456", 10, new Uri("http://www.example.com/"), "http://www.example.com");
            Origin origin2 = new Origin("Kinematics", "123456", 10, new Uri("http://www.example.com/"), "http://www.example.com");

            Post post1 = new Post(origin1, postText);
            Post post2 = new Post(origin2, postText);

            Assert.AreEqual(post1, post2);
            Assert.IsTrue(post1 == post2);
        }

        [TestMethod]
        public void Compare_Unequal()
        {
            string postText =
@"[x] Line 1
[x] Line 2";

            Origin origin1 = new Origin("Kinematics", "123456", 10, new Uri("http://www.example.com/"), "http://www.example.com");
            Origin origin2 = new Origin("Kinematics", "123457", 11, new Uri("http://www.example.com/"), "http://www.example.com");

            Post post1 = new Post(origin1, postText);
            Post post2 = new Post(origin2, postText);

            Assert.AreNotEqual(post1, post2);
            Assert.IsTrue(post1 != post2);
            Assert.IsTrue(post2 > post1);
        }

        [TestMethod]
        public void VoteLines_Count_2()
        {
            string postText =
@"[x] Line 1
[x] Line 2";

            Post post = new Post(origin, postText);

            Assert.AreEqual(2, post.VoteLines.Count);
            Assert.IsTrue(post.HasVote);
            Assert.AreEqual("[] Line 1", post.VoteLines[0].ToComparableString());
        }

        [TestMethod]
        public void VoteLines_Count_2_Skip()
        {
            string postText =
@"Tentative vote idea:
[x] Line 1

But might include something else...
[x] Line 2";

            Post post = new Post(origin, postText);

            Assert.AreEqual(2, post.VoteLines.Count);
            Assert.IsTrue(post.HasVote);
            Assert.AreEqual("[] Line 1", post.VoteLines[0].ToComparableString());
        }

        [TestMethod]
        public void VoteLines_Count_Nomination_Fail()
        {
            string postText =
@"Tentative vote idea:
『url=""https://forums.sufficientvelocity.com/members/4076/""』@Kinematics『/url』
『url=""https://forums.sufficientvelocity.com/members/4078/""』@TheInnerHollow『/url』
";

            Post post = new Post(origin, postText);

            Assert.AreEqual(0, post.VoteLines.Count);
            Assert.IsFalse(post.HasVote);
        }

        [TestMethod]
        public void VoteLines_Count_Nomination_Pass()
        {
            string postText =
@"『url=""https://forums.sufficientvelocity.com/members/4076/""』@Kinematics『/url』
『url=""https://forums.sufficientvelocity.com/members/4078/""』@TheInnerHollow『/url』
";

            Post post = new Post(origin, postText);

            Assert.AreEqual(2, post.VoteLines.Count);
            Assert.IsTrue(post.HasVote);
            Assert.AreEqual("[] Kinematics", post.VoteLines[0].ToComparableString());
        }

        [TestMethod]
        public void VoteLines_Complex()
        {
            string postText =
@"[90%] Line 1
-[x] Subunit one
--[x] Special case two
[30%][Exception] Line 2
-[x] Really don't care for this option";

            Post post = new Post(origin, postText);

            Assert.AreEqual(5, post.VoteLines.Count);
            Assert.IsTrue(post.HasVote);
            Assert.AreEqual("[] Line 1", post.VoteLines[0].ToComparableString());
        }

        [TestMethod]
        public void VoteLines_BBCode_Unbalanced()
        {
            string postText =
@"What do you think they'll be doing now?
『b』[x] Ferris wheel
-[x] At the top『/b』";

            Post post = new Post(origin, postText);

            Assert.AreEqual(2, post.VoteLines.Count);
            Assert.IsTrue(post.HasVote);
            Assert.AreEqual("[] Ferris wheel", post.VoteLines[0].ToComparableString());
            Assert.AreEqual("-[] At the top", post.VoteLines[1].ToComparableString());
            Assert.AreEqual("[x] Ferris wheel", post.VoteLines[0].ToString());
            Assert.AreEqual("-[x] At the top", post.VoteLines[1].ToString());
        }

        [TestMethod]
        public void VoteLines_BBCode_Internal_Italics()
        {
            string postText =
@"What do you think they'll be doing now?
[x] Ferris wheel
[X] 『i』Teacups『/i』";

            Post post = new Post(origin, postText);

            Assert.AreEqual(2, post.VoteLines.Count);
            Assert.IsTrue(post.HasVote);
            Assert.AreEqual("[] Ferris wheel", post.VoteLines[0].ToComparableString());
            Assert.AreEqual("[] Teacups", post.VoteLines[1].ToComparableString());
            Assert.AreEqual("[x] Ferris wheel", post.VoteLines[0].ToString());
            Assert.AreEqual("[X] 『i』Teacups『/i』", post.VoteLines[1].ToString());
        }

        [TestMethod]
        public void VoteLines_BBCode_Internal_Color_Named()
        {
            string postText =
@"What do you think they'll be doing now?
[x] Ferris wheel
[x] 『color=orange』Teacups『/color』";

            Post post = new Post(origin, postText);

            Assert.AreEqual(2, post.VoteLines.Count);
            Assert.IsTrue(post.HasVote);
            Assert.AreEqual("[] Ferris wheel", post.VoteLines[0].ToComparableString());
            Assert.AreEqual("[] Teacups", post.VoteLines[1].ToComparableString());
            Assert.AreEqual("[x] Ferris wheel", post.VoteLines[0].ToString());
            Assert.AreEqual("[x] 『color=orange』Teacups『/color』", post.VoteLines[1].ToString());
        }

        [TestMethod]
        public void VoteLines_BBCode_Internal_Color_HTML()
        {
            string postText =
@"What do you think they'll be doing now?
[x] Ferris wheel
[x] 『color=#ff00AA』Teacups『/color』";

            Post post = new Post(origin, postText);

            Assert.AreEqual(2, post.VoteLines.Count);
            Assert.IsTrue(post.HasVote);
            Assert.AreEqual("[] Ferris wheel", post.VoteLines[0].ToComparableString());
            Assert.AreEqual("[] Teacups", post.VoteLines[1].ToComparableString());
            Assert.AreEqual("[x] Ferris wheel", post.VoteLines[0].ToString());
            Assert.AreEqual("[x] 『color=#ff00AA』Teacups『/color』", post.VoteLines[1].ToString());
        }

        [TestMethod]
        public void Intro_Line_Flush_1()
        {
            string postText =
@"What do you think they'll be doing now?
[x] Ferris wheel
-[x] At the top";

            Post post = new Post(origin, postText);

            Assert.AreEqual(2, post.VoteLines.Count);
            Assert.IsTrue(post.HasVote);
            Assert.AreEqual("[] Ferris wheel", post.VoteLines[0].ToComparableString());
            Assert.AreEqual("-[] At the top", post.VoteLines[1].ToComparableString());
            Assert.AreEqual("[x] Ferris wheel", post.VoteLines[0].ToString());
            Assert.AreEqual("-[x] At the top", post.VoteLines[1].ToString());
        }

        [TestMethod]
        public void Intro_Line_Flush_2()
        {
            string postText =
@"What do you think they'll be doing now?
-[x] Ferris wheel
-[x] At the top";

            Post post = new Post(origin, postText);

            Assert.AreEqual(2, post.VoteLines.Count);
            Assert.IsTrue(post.HasVote);
            Assert.AreEqual("[] Ferris wheel", post.VoteLines[0].ToComparableString());
            Assert.AreEqual("-[] At the top", post.VoteLines[1].ToComparableString());
            Assert.AreEqual("[x] Ferris wheel", post.VoteLines[0].ToString());
            Assert.AreEqual("-[x] At the top", post.VoteLines[1].ToString());
        }

        [TestMethod]
        public void Strike_Through_Plan_1()
        {
            string postText =
@"Considering:
❰[X] Plan Triplemancer⦂-[X] Aeromancy (40%)⦂-[X] Hydromancy (30%)⦂-[X] Pyromancy (30%)❱";
            Post post = new Post(origin, postText);

            Assert.IsFalse(post.HasVote);
        }

        [TestMethod]
        public void Strike_Through_Plan_2()
        {
            string postText =
@"Considering:
❰⦂[X] Plan Triplemancer⦂-[X] Aeromancy (40%)⦂-[X] Hydromancy (30%)⦂-[X] Pyromancy (30%)❱";
            Post post = new Post(origin, postText);

            Assert.IsFalse(post.HasVote);

        }

        [TestMethod]
        public void Strike_Through_Content()
        {
            string postText =
@"Considering:
[X] Plan Air, ❰Earth, Water,❱ Fire
-[X] Aeromancy (40%)
-[X] Pyromancy (25%)
-[X] Pyromantic Divination (35%)";
            Post post = new Post(origin, postText);

            Assert.IsTrue(post.HasVote);
            Assert.AreEqual(4, post.VoteLines.Count);
            Assert.AreEqual("[X] Plan Air, 『s』Earth, Water,『/s』 Fire", post.VoteLines[0].ToString());

        }

        [TestMethod]
        public void Strike_Through_Post_Marker()
        {
            string postText =
@"Considering:
[X] ❰Earth, Water,❱ Fire";
            Post post = new Post(origin, postText);

            Assert.IsTrue(post.HasVote);
            Assert.AreEqual(1, post.VoteLines.Count);
            Assert.AreEqual("[X] 『s』Earth, Water,『/s』 Fire", post.VoteLines[0].ToString());

        }
    }
}
