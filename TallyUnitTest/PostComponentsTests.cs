using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Linq;
using NetTally;

namespace TallyUnitTest
{
    [TestClass()]
    public class PostComponentsTests
    {
        // Test that various constructions result in proper object formation.

        [TestMethod()]
        public void ConstructorTest1()
        {
            string author = "Kinematics";
            string id = "12345";
            string msg = @"What do you think they'll be doing now?";

            PostComponents p = new PostComponents(author, id, msg);

            Assert.AreEqual(author, p.Author);
            Assert.AreEqual(id, p.ID);
            Assert.AreEqual(12345, p.IDValue);
            Assert.IsFalse(p.IsVote);
            Assert.IsNull(p.VoteStrings);
        }

        [TestMethod()]
        public void ConstructorTest2()
        {
            string author = "Kinematics";
            string id = "12345";
            string msg = @"What do you think they'll be doing now? Use [x] to indicate which you're selecting.";

            PostComponents p = new PostComponents(author, id, msg);

            Assert.AreEqual(author, p.Author);
            Assert.AreEqual(id, p.ID);
            Assert.AreEqual(12345, p.IDValue);
            Assert.IsFalse(p.IsVote);
            Assert.IsNull(p.VoteStrings);
        }

        [TestMethod()]
        public void ConstructorTest3()
        {
            string author = "Kinematics";
            string id = "12345";
            string msg = @"What do you think they'll be doing now?
[x] Ferris wheel";

            PostComponents p = new PostComponents(author, id, msg);

            Assert.AreEqual(author, p.Author);
            Assert.AreEqual(id, p.ID);
            Assert.AreEqual(12345, p.IDValue);
            Assert.IsTrue(p.IsVote);
            Assert.IsNotNull(p.VoteStrings);
            Assert.AreEqual(1, p.VoteStrings.Count);
            Assert.AreEqual(@"[x] Ferris wheel", p.VoteStrings.First());
        }

        [TestMethod()]
        public void ConstructorTest4()
        {
            string author = "Kinematics";
            string id = "12345";
            string msg = @"What do you think they'll be doing now? Use [x] to indicate which you're selecting.
Last vote was:
[color=invisible]##### NetTally 1.1.8[/color]
[x] Ferris wheel.";

            PostComponents p = new PostComponents(author, id, msg);

            Assert.AreEqual(author, p.Author);
            Assert.AreEqual(id, p.ID);
            Assert.AreEqual(12345, p.IDValue);
            Assert.IsFalse(p.IsVote);
            Assert.IsNull(p.VoteStrings);
        }

        // Test that various comparisons properly relate two separate objects.

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
    }
}
