using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NetTally.Votes.Experiment2;
using NetTally.Votes;

namespace TallyUnitTest.Tally_Experiment_2
{
    [TestClass]
    public class VoteLineTests
    {
        [TestMethod]
        public void Construct_null_prefix()
        {
            VoteLine line = new VoteLine(null, "x", "", "content", MarkerType.Vote, 0);
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentNullException))]
        public void Construct_null_marker()
        {
            VoteLine line = new VoteLine("", null, "", "content", MarkerType.Vote, 0);
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentNullException))]
        public void Construct_empty_marker()
        {
            VoteLine line = new VoteLine("", "", "", "content", MarkerType.Vote, 0);
        }

        [TestMethod]
        public void Construct_null_task()
        {
            VoteLine line = new VoteLine("", "x", null, "content", MarkerType.Vote, 0);
        }

        [TestMethod]
        public void Construct_null_content()
        {
            VoteLine line = new VoteLine("", "x", "", null, MarkerType.Vote, 0);
        }

        [TestMethod]
        public void Trimming()
        {
            VoteLine line = new VoteLine("", " x", " a task ", "  some content ", MarkerType.Vote, 0);

            Assert.AreEqual("", line.Prefix);
            Assert.AreEqual("x", line.Marker);
            Assert.AreEqual("a task", line.Task);
            Assert.AreEqual("some content", line.Content);
            Assert.AreEqual(MarkerType.Vote, line.MarkerType);
            Assert.AreEqual(0, line.MarkerValue);
        }

    }
}
