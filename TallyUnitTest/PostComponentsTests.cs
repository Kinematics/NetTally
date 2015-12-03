using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Linq;
using NetTally;

namespace TallyUnitTest
{
    [TestClass()]
    public class PostComponentsTests
    {
        static string author = "Kinematics";
        static string id = "12345";

        #region Exceptions during construction
        [TestMethod()]
        [ExpectedException(typeof(ArgumentNullException))]
        public void BadConstruction1()
        {
            string msg = @"What do you think they'll be doing now?";

            PostComponents p = new PostComponents(null, id, msg);
        }

        [TestMethod()]
        [ExpectedException(typeof(ArgumentNullException))]
        public void BadConstruction2()
        {
            string msg = @"What do you think they'll be doing now?";

            PostComponents p = new PostComponents(author, null, msg);
        }

        [TestMethod()]
        [ExpectedException(typeof(ArgumentNullException))]
        public void BadConstruction3()
        {
            //string msg = @"What do you think they'll be doing now?";

            PostComponents p = new PostComponents(author, id, null);
        }

        [TestMethod()]
        [ExpectedException(typeof(ArgumentNullException))]
        public void BadConstruction4()
        {
            string msg = @"What do you think they'll be doing now?";

            PostComponents p = new PostComponents("", id, msg);
        }

        [TestMethod()]
        [ExpectedException(typeof(ArgumentNullException))]
        public void BadConstruction5()
        {
            string msg = @"What do you think they'll be doing now?";

            PostComponents p = new PostComponents(author, "", msg);
        }

        [TestMethod()]
        public void BadConstruction6()
        {
            //string msg = @"What do you think they'll be doing now?";

            PostComponents p = new PostComponents(author, id, "");
            Assert.IsFalse(p.IsVote);
        }
        #endregion

        #region Basic non-votes, and checking author and ID

        [TestMethod()]
        public void NoVoteContent()
        {
            string msg = @"What do you think they'll be doing now?";

            PostComponents p = new PostComponents(author, id, msg);

            Assert.AreEqual(author, p.Author);
            Assert.AreEqual(id, p.ID);
            Assert.AreEqual(12345, p.IDValue);
            Assert.IsFalse(p.IsVote);
            Assert.IsNull(p.VoteStrings);
        }

        [TestMethod()]
        public void EmbeddedMarker()
        {
            string msg = @"What do you think they'll be doing now? Use [x] to indicate which you're selecting.";

            PostComponents p = new PostComponents(author, id, msg);

            Assert.AreEqual(author, p.Author);
            Assert.AreEqual(id, p.ID);
            Assert.AreEqual(12345, p.IDValue);
            Assert.IsFalse(p.IsVote);
            Assert.IsNull(p.VoteStrings);
        }

        [TestMethod()]
        public void NonNumericID()
        {
            string id = "A3000F";
            string msg = @"What do you think they'll be doing now?";

            PostComponents p = new PostComponents(author, id, msg);

            Assert.AreEqual(author, p.Author);
            Assert.AreEqual(id, p.ID);
            Assert.AreEqual(0, p.IDValue);
            Assert.IsFalse(p.IsVote);
            Assert.IsNull(p.VoteStrings);
        }

        #endregion

        #region Various vote constructions
        [TestMethod()]
        public void ConstructTallyPost()
        {
            string msg = @"What do you think they'll be doing now? Use [x] to indicate which you're selecting.
Last vote was:
[color=invisible]##### NetTally 1.1.8[/color]
[x] Ferris wheel.";

            PostComponents p = new PostComponents(author, id, msg);

            Assert.IsFalse(p.IsVote);
            Assert.IsNull(p.VoteStrings);
        }

        [TestMethod()]
        public void ConstructorTest1()
        {
            string msg =
@"What do you think they'll be doing now?
[x] Ferris wheel";

            PostComponents p = new PostComponents(author, id, msg);

            Assert.IsTrue(p.IsVote);
            Assert.IsNotNull(p.VoteStrings);
            Assert.AreEqual(1, p.VoteStrings.Count);
            Assert.AreEqual(@"[x] Ferris wheel", p.VoteStrings.First());
        }

        [TestMethod()]
        public void ConstructorTest2()
        {
            string msg =
@"What do you think they'll be doing now?
[x] Ferris wheel
Additional:
[x] Ice cream";

            PostComponents p = new PostComponents(author, id, msg);

            Assert.IsTrue(p.IsVote);
            Assert.IsNotNull(p.VoteStrings);
            Assert.AreEqual(2, p.VoteStrings.Count);
            Assert.AreEqual(@"[x] Ferris wheel", p.VoteStrings[0]);
            Assert.AreEqual(@"[x] Ice cream", p.VoteStrings[1]);
        }

        [TestMethod()]
        public void ConstructorTest3()
        {
            string msg =
@"What do you think they'll be doing now?
[x] Ferris wheel
-[x] At the top";

            PostComponents p = new PostComponents(author, id, msg);

            Assert.IsTrue(p.IsVote);
            Assert.IsNotNull(p.VoteStrings);
            Assert.AreEqual(2, p.VoteStrings.Count);
            Assert.AreEqual(@"[x] Ferris wheel", p.VoteStrings[0]);
            Assert.AreEqual(@"-[x] At the top", p.VoteStrings[1]);
        }

        [TestMethod()]
        public void ConstructorTest4()
        {
            string msg =
@"What do you think they'll be doing now?
[x] Ferris wheel
- [X] At the top";

            PostComponents p = new PostComponents(author, id, msg);

            Assert.IsTrue(p.IsVote);
            Assert.IsNotNull(p.VoteStrings);
            Assert.AreEqual(2, p.VoteStrings.Count);
            Assert.AreEqual(@"[x] Ferris wheel", p.VoteStrings[0]);
            Assert.AreEqual(@"- [X] At the top", p.VoteStrings[1]);
        }

        [TestMethod()]
        public void ConstructorTest5()
        {
            string msg =
@"What do you think they'll be doing now?
[x] Ferris wheel
- - [X] - At the top";

            PostComponents p = new PostComponents(author, id, msg);

            Assert.IsTrue(p.IsVote);
            Assert.IsNotNull(p.VoteStrings);
            Assert.AreEqual(2, p.VoteStrings.Count);
            Assert.AreEqual(@"[x] Ferris wheel", p.VoteStrings[0]);
            Assert.AreEqual(@"- - [X] - At the top", p.VoteStrings[1]);
        }

        [TestMethod()]
        public void ConstructorTest6()
        {
            string msg =
@"What do you think they'll be doing now?
[b][x] Ferris wheel
-[x] At the top[/b]";

            PostComponents p = new PostComponents(author, id, msg);

            Assert.IsTrue(p.IsVote);
            Assert.IsNotNull(p.VoteStrings);
            Assert.AreEqual(2, p.VoteStrings.Count);
            Assert.AreEqual(@"[x] Ferris wheel", p.VoteStrings[0]);
            Assert.AreEqual(@"-[x] At the top", p.VoteStrings[1]);
        }

        [TestMethod()]
        public void ConstructorTest7()
        {
            string msg =
@"What do you think they'll be doing now?
[x] Ferris wheel
[X] [i]Teacups[/i]";

            PostComponents p = new PostComponents(author, id, msg);

            Assert.IsTrue(p.IsVote);
            Assert.IsNotNull(p.VoteStrings);
            Assert.AreEqual(2, p.VoteStrings.Count);
            Assert.AreEqual(@"[x] Ferris wheel", p.VoteStrings[0]);
            Assert.AreEqual(@"[X] [i]Teacups[/i]", p.VoteStrings[1]);
        }

        [TestMethod()]
        public void ConstructorTest8()
        {
            string msg =
@"What do you think they'll be doing now?
[x] Ferris wheel
[x] [color=orange]Teacups[/color]";

            PostComponents p = new PostComponents(author, id, msg);

            Assert.IsTrue(p.IsVote);
            Assert.IsNotNull(p.VoteStrings);
            Assert.AreEqual(2, p.VoteStrings.Count);
            Assert.AreEqual(@"[x] Ferris wheel", p.VoteStrings[0]);
            Assert.AreEqual(@"[x] [color=orange]Teacups[/color]", p.VoteStrings[1]);
        }

        [TestMethod()]
        public void ConstructorTest9()
        {
            string msg =
@"What do you think they'll be doing now?
[x] Ferris wheel
[x] [color=#ff00AA]Teacups[/color]";

            PostComponents p = new PostComponents(author, id, msg);

            Assert.IsTrue(p.IsVote);
            Assert.IsNotNull(p.VoteStrings);
            Assert.AreEqual(2, p.VoteStrings.Count);
            Assert.AreEqual(@"[x] Ferris wheel", p.VoteStrings[0]);
            Assert.AreEqual(@"[x] [color=#ff00AA]Teacups[/color]", p.VoteStrings[1]);
        }
        #endregion

        #region Test that various comparisons properly relate two separate objects.

        [TestMethod()]
        public void CompareTest1()
        {
            string author1 = "Kinematics";
            string id1 = "12345";
            string msg1 = @"What do you think they'll be doing now?
[x] Ferris wheel";

            string author2 = "Kinematics";
            string id2 = "12345";
            string msg2 = @"What do you think they'll be doing now?
[x] Ferris wheel";

            PostComponents p1 = new PostComponents(author1, id1, msg1);
            PostComponents p2 = new PostComponents(author2, id2, msg2);

            Assert.IsTrue(p1.CompareTo(p2) == 0);
        }

        [TestMethod()]
        public void CompareTest2()
        {
            string author1 = "Kinematics";
            string id1 = "12345";
            string msg1 = @"What do you think they'll be doing now?
[x] Ferris wheel";

            string author2 = "Kine";
            string id2 = "12345";
            string msg2 = @"What do you think they'll be doing now?";

            PostComponents p1 = new PostComponents(author1, id1, msg1);
            PostComponents p2 = new PostComponents(author2, id2, msg2);

            Assert.IsTrue(p1.CompareTo(p2) == 0);
        }

        [TestMethod()]
        public void CompareTest3()
        {
            string author1 = "Kinematics";
            string id1 = "12344";
            string msg1 = @"What do you think they'll be doing now?";

            string author2 = "Kinematics";
            string id2 = "12345";
            string msg2 = @"What do you think they'll be doing now?";

            PostComponents p1 = new PostComponents(author1, id1, msg1);
            PostComponents p2 = new PostComponents(author2, id2, msg2);

            Assert.IsTrue(p1.CompareTo(p2) < 0);
        }

        [TestMethod()]
        public void CompareTest4()
        {
            string author1 = "Kinematics";
            string id1 = "12346";
            string msg1 = @"What do you think they'll be doing now?";

            string author2 = "Kinematics";
            string id2 = "12345";
            string msg2 = @"What do you think they'll be doing now?";

            PostComponents p1 = new PostComponents(author1, id1, msg1);
            PostComponents p2 = new PostComponents(author2, id2, msg2);

            Assert.IsTrue(p1.CompareTo(p2) > 0);
        }
        #endregion
    }
}
