using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NetTally.Votes.Experiment2;
//using NetTally.Votes;

namespace NTTests.Experiment_2
{
    [TestClass]
    public class VoteLineTests
    {
#nullable disable
        [TestMethod]
        public void Construct_null_prefix()
        {
            VoteLine _ = new VoteLine(null, "x", "", "content", NetTally.Votes.MarkerType.Vote, 0);
        }

        [Ignore]
        [TestMethod]
        [ExpectedException(typeof(ArgumentNullException))]
        public void Construct_null_marker()
        {
            VoteLine _ = new VoteLine("", null, "", "content", NetTally.Votes.MarkerType.Vote, 0);
        }

        [Ignore]
        [TestMethod]
        [ExpectedException(typeof(ArgumentNullException))]
        public void Construct_empty_marker()
        {
            VoteLine _ = new VoteLine("", "", "", "content", NetTally.Votes.MarkerType.Vote, 0);
        }

        [TestMethod]
        public void Construct_null_task()
        {
            VoteLine _ = new VoteLine("", "x", null, "content", NetTally.Votes.MarkerType.Vote, 0);
        }

        [TestMethod]
        public void Construct_null_content()
        {
            VoteLine _ = new VoteLine("", "x", "", null, NetTally.Votes.MarkerType.Vote, 0);
        }
#nullable enable

        [TestMethod]
        public void Trimming()
        {
            VoteLine line = new VoteLine("", " x", " a task ", "  some content ", NetTally.Votes.MarkerType.Vote, 0);

            Assert.AreEqual("", line.Prefix);
            Assert.AreEqual("x", line.Marker);
            Assert.AreEqual("a task", line.Task);
            Assert.AreEqual("some content", line.Content);
            Assert.AreEqual(NetTally.Votes.MarkerType.Vote, line.MarkerType);
            Assert.AreEqual(0, line.MarkerValue);
        }

    }
}
