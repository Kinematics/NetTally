using System;
using System.Linq;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NetTally.Experiment3;

namespace NetTally.Tests.Experiment3
{
    [TestClass]
    public class PostTests
    {
        static IServiceProvider serviceProvider;

        [ClassInitialize]
        public static void ClassInit(TestContext context)
        {
            serviceProvider = TestStartup.ConfigureServices();
        }

#nullable disable
        [TestMethod]
        [ExpectedException(typeof(ArgumentNullException))]
        public void Construct_Fail_NullAuthor()
        {
            Post post = new Post(null, "101", "Some text", 1);
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentNullException))]
        public void Construct_Fail_NullID()
        {
            Post post = new Post("Kinematics", null, "Some text", 1);
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentNullException))]
        public void Construct_Fail_NullText()
        {
            Post post = new Post("Kinematics", "101", null, 1);
        }
#nullable enable

        [TestMethod]
        [ExpectedException(typeof(ArgumentOutOfRangeException))]
        public void Construct_Fail_BadID()
        {
            Post post = new Post("Kinematics", "-101", "Some text", 1);
        }

        [TestMethod]
        public void Construct_BadID_Unknown()
        {
            Post post = new Post("Kinematics", "101xq", "Some text", 1);
            Assert.AreEqual(0, post.IDValue);
        }

        [TestMethod]
        public void Construct_BadID_Overflow()
        {
            Post post = new Post("Kinematics", "4294967296", "Some text", 1);
            Assert.AreEqual(0, post.IDValue);
        }

        [TestMethod]
        public void Check_Unmodified()
        {
            string author = "Kinematics";
            string postId = "123456";
            string postText =
@"[x] Line 1
[x] Line 2";
            int postNumber = 10;

            Post post = new Post(author, postId, postText, postNumber);

            Assert.AreEqual(author, post.Author);
            Assert.AreEqual(postId, post.ID);
            Assert.AreEqual(123456, post.IDValue);
            Assert.AreEqual(postText, post.Text);
            Assert.AreEqual(postNumber, post.Number);
        }

        [TestMethod]
        public void VoteLines_Count_0()
        {
            string author = "Kinematics";
            string postId = "123456";
            string postText =
@"A line of discussion, going on about
what we might consider doing.";
            int postNumber = 10;

            Post post = new Post(author, postId, postText, postNumber);

            Assert.AreEqual(0, post.VoteLines.Count);
            Assert.IsFalse(post.IsVote);
        }

        [TestMethod]
        public void VoteLines_Count_Tally()
        {
            string author = "Kinematics";
            string postId = "123456";
            string postText =
@"Someone posted a tally:
##### NetTally
[X] A count of votes";
            int postNumber = 10;

            Post post = new Post(author, postId, postText, postNumber);

            Assert.AreEqual(0, post.VoteLines.Count);
            Assert.IsFalse(post.IsVote);
        }

        [TestMethod]
        public void VoteLines_Count_Tally_Invisitext()
        {
            string author = "Kinematics";
            string postId = "123456";
            string postText =
@"Someone posted a tally:
『color=Transparent』##### NetTally『/color』
[X] A count of votes";
            int postNumber = 10;

            Post post = new Post(author, postId, postText, postNumber);

            Assert.AreEqual(0, post.VoteLines.Count);
            Assert.IsFalse(post.IsVote);
        }

        [TestMethod]
        public void Compare_Equal()
        {
            string author = "Kinematics";
            string postId = "123456";
            string postText =
@"[x] Line 1
[x] Line 2";
            int postNumber = 10;

            Post post1 = new Post(author, postId, postText, postNumber);
            Post post2 = new Post(author, postId, postText, postNumber);

            Assert.AreEqual(post1, post2);
            Assert.IsTrue(post1 == post2);
        }

        [TestMethod]
        public void Compare_Unequal()
        {
            string author = "Kinematics";
            int idNum = 123456;
            string postText =
@"[x] Line 1
[x] Line 2";
            int postNumber = 10;

            Post post1 = new Post(author, idNum.ToString(), postText, postNumber);
            Post post2 = new Post(author, (idNum+1).ToString(), postText, postNumber);

            Assert.AreNotEqual(post1, post2);
            Assert.IsTrue(post1 != post2);
            Assert.IsTrue(post2 > post1);
        }

        [TestMethod]
        public void VoteLines_Count_2()
        {
            string author = "Kinematics";
            string postId = "123456";
            string postText =
@"[x] Line 1
[x] Line 2";
            int postNumber = 10;

            Post post = new Post(author, postId, postText, postNumber);

            Assert.AreEqual(2, post.VoteLines.Count);
            Assert.IsTrue(post.IsVote);
            Assert.AreEqual("[] Line 1", post.VoteLines.First().ToComparableString());
        }

        [TestMethod]
        public void VoteLines_Count_2_Skip()
        {
            string author = "Kinematics";
            string postId = "123456";
            string postText =
@"Tentative vote idea:
[x] Line 1

But might include something else...
[x] Line 2";
            int postNumber = 10;

            Post post = new Post(author, postId, postText, postNumber);

            Assert.AreEqual(2, post.VoteLines.Count);
            Assert.IsTrue(post.IsVote);
            Assert.AreEqual("[] Line 1", post.VoteLines.First().ToComparableString());
        }

        [TestMethod]
        public void VoteLines_Count_Nomination_Fail()
        {
            string author = "Kinematics";
            string postId = "123456";
            string postText =
@"Tentative vote idea:
『url=""https://forums.sufficientvelocity.com/members/4076/""』@Kinematics『/url』
『url=""https://forums.sufficientvelocity.com/members/4078/""』@TheInnerHollow『/url』
";
            int postNumber = 10;

            Post post = new Post(author, postId, postText, postNumber);

            Assert.AreEqual(0, post.VoteLines.Count);
            Assert.IsFalse(post.IsVote);
        }

        [TestMethod]
        public void VoteLines_Count_Nomination_Pass()
        {
            string author = "Kinematics";
            string postId = "123456";
            string postText =
@"『url=""https://forums.sufficientvelocity.com/members/4076/""』@Kinematics『/url』
『url=""https://forums.sufficientvelocity.com/members/4078/""』@TheInnerHollow『/url』
";
            int postNumber = 10;

            Post post = new Post(author, postId, postText, postNumber);

            Assert.AreEqual(2, post.VoteLines.Count);
            Assert.IsTrue(post.IsVote);
            Assert.AreEqual("[] Kinematics", post.VoteLines.First().ToComparableString());
        }

        [TestMethod]
        public void VoteLines_Complex()
        {
            string author = "Kinematics";
            string postId = "123456";
            string postText =
@"[90%] Line 1
-[x] Subunit one
--[x] Special case two
[30%][Exception] Line 2
-[x] Really don't care for this option";
            int postNumber = 10;

            Post post = new Post(author, postId, postText, postNumber);

            Assert.AreEqual(5, post.VoteLines.Count);
            Assert.IsTrue(post.IsVote);
            Assert.AreEqual("[] Line 1", post.VoteLines.First().ToComparableString());
        }

    }
}
