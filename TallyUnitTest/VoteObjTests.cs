using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
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
            Assert.AreEqual("Kinematics", post.Author);
            Assert.AreEqual("12345", post.ID);
            Assert.AreEqual(12345, post.IDNumber);
            Assert.AreEqual(12, post.Number);
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
            Assert.AreEqual("Kinematics", post.Author);
            Assert.AreEqual("12345", post.ID);
            Assert.AreEqual(12345, post.IDNumber);
            Assert.AreEqual(12, post.Number);
            Assert.IsFalse(post.HasVote);
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
            Assert.AreEqual("Kinematics", post.Author);
            Assert.AreEqual("12345", post.ID);
            Assert.AreEqual(12345, post.IDNumber);
            Assert.AreEqual(12, post.Number);
            Assert.IsFalse(post.HasVote);
        }
    }
}
