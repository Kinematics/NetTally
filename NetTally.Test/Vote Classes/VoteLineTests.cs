using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NetTally.Votes;
using NetTally.Votes.Experiment;
using NetTally.Utility;
using NetTally.Tests.Platform;

namespace NetTally.Tests
{
    [TestClass]
    public class VoteLineTests
    {
        [ClassInitialize]
        public static void ClassInit(TestContext context)
        {
            Agnostic.HashStringsUsing(UnicodeHashFunction.HashFunction);
        }


        [TestMethod]
        public void EmptyVoteLine()
        {
            Assert.AreEqual(string.Empty, VoteLine.Empty.Text);
            Assert.AreEqual(string.Empty, VoteLine.Empty.Prefix);
            Assert.AreEqual(string.Empty, VoteLine.Empty.Marker);
            Assert.AreEqual(MarkerType.None, VoteLine.Empty.MarkerType);
            Assert.AreEqual(0, VoteLine.Empty.MarkerValue);
            Assert.AreEqual(string.Empty, VoteLine.Empty.Task);
            Assert.AreEqual(string.Empty, VoteLine.Empty.Content);
            Assert.AreEqual(string.Empty, VoteLine.Empty.TrimmedContent);
            Assert.AreEqual(string.Empty, VoteLine.Empty.DisplayContent);
            Assert.AreEqual(string.Empty, VoteLine.Empty.ComparableContent);
            Assert.AreEqual(string.Empty, VoteLine.Empty.SimplifiedContent);
            Assert.AreEqual(string.Empty.GetHashCode(), VoteLine.Empty.GetHashCode());
            Assert.AreEqual("[] ", VoteLine.Empty.ToString());
        }

        #region Test for expected construction failures
        [TestMethod]
        [ExpectedException(typeof(ArgumentNullException))]
        public void Construct_Fail_1()
        {
            VoteLine vote = new VoteLine("");
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentException))]
        public void Construct_Fail_2()
        {
            VoteLine vote = new VoteLine("A line of text.");
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentException))]
        public void Construct_Fail_3()
        {
            VoteLine vote = new VoteLine("{x] Mismatch brackets");
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentException))]
        public void Construct_Fail_4()
        {
            VoteLine vote = new VoteLine("~~[x] Incorrect prefix");
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentException))]
        public void Construct_Fail_5()
        {
            VoteLine vote = new VoteLine("--[-x] Invalid marker");
        }
        #endregion

        #region Basic construction
        [TestMethod]
        public void Construct1()
        {
            VoteLine vote = new VoteLine("[x] A simple vote line");

            Assert.AreEqual("[x] A simple vote line", vote.Text);
            Assert.AreEqual(string.Empty, vote.Prefix);
            Assert.AreEqual("x", vote.Marker);
            Assert.AreEqual(MarkerType.Vote, vote.MarkerType);
            Assert.AreEqual(0, vote.MarkerValue);
            Assert.AreEqual(string.Empty, vote.Task);
            Assert.AreEqual("A simple vote line", vote.Content);
            Assert.AreEqual("A simple vote line", vote.TrimmedContent);
            Assert.AreEqual("A simple vote line", vote.DisplayContent);
            Assert.AreEqual("A simple vote line", vote.ComparableContent);
            Assert.AreEqual("Asimplevoteline", vote.SimplifiedContent);
            Assert.AreEqual("[x] A simple vote line", vote.ToString());
        }

        [TestMethod]
        public void Construct2()
        {
            VoteLine vote = new VoteLine("[x][How?] A simple vote line with task");

            Assert.AreEqual("[x][How?] A simple vote line with task", vote.Text);
            Assert.AreEqual(string.Empty, vote.Prefix);
            Assert.AreEqual("x", vote.Marker);
            Assert.AreEqual(MarkerType.Vote, vote.MarkerType);
            Assert.AreEqual(0, vote.MarkerValue);
            Assert.AreEqual("How?", vote.Task);
            Assert.AreEqual("A simple vote line with task", vote.Content);
            Assert.AreEqual("A simple vote line with task", vote.TrimmedContent);
            Assert.AreEqual("A simple vote line with task", vote.DisplayContent);
            Assert.AreEqual("A simple vote line with task", vote.ComparableContent);
            Assert.AreEqual("Asimplevotelinewithtask", vote.SimplifiedContent);
            Assert.AreEqual("[x][How?] A simple vote line with task", vote.ToString());
        }

        [TestMethod]
        public void Construct3()
        {
            VoteLine vote = new VoteLine("- - [x][How?] A simple vote line with task and prefix");

            Assert.AreEqual("- - [x][How?] A simple vote line with task and prefix", vote.Text);
            Assert.AreEqual("--", vote.Prefix);
            Assert.AreEqual("x", vote.Marker);
            Assert.AreEqual(MarkerType.Vote, vote.MarkerType);
            Assert.AreEqual(0, vote.MarkerValue);
            Assert.AreEqual("How?", vote.Task);
            Assert.AreEqual("A simple vote line with task and prefix", vote.Content);
            Assert.AreEqual("A simple vote line with task and prefix", vote.TrimmedContent);
            Assert.AreEqual("A simple vote line with task and prefix", vote.DisplayContent);
            Assert.AreEqual("A simple vote line with task and prefix", vote.ComparableContent);
            Assert.AreEqual("Asimplevotelinewithtaskandprefix", vote.SimplifiedContent);
            Assert.AreEqual("--[x][How?] A simple vote line with task and prefix", vote.ToString());
        }

        [TestMethod]
        public void Construct4()
        {
            // Extra space between the marker and task
            VoteLine vote = new VoteLine("- - [x] [How?] A simple vote line with task and prefix");

            Assert.AreEqual("- - [x][How?] A simple vote line with task and prefix", vote.Text);
            Assert.AreEqual("--", vote.Prefix);
            Assert.AreEqual("x", vote.Marker);
            Assert.AreEqual(MarkerType.Vote, vote.MarkerType);
            Assert.AreEqual(0, vote.MarkerValue);
            Assert.AreEqual("How?", vote.Task);
            Assert.AreEqual("A simple vote line with task and prefix", vote.Content);
            Assert.AreEqual("A simple vote line with task and prefix", vote.TrimmedContent);
            Assert.AreEqual("A simple vote line with task and prefix", vote.DisplayContent);
            Assert.AreEqual("A simple vote line with task and prefix", vote.ComparableContent);
            Assert.AreEqual("Asimplevotelinewithtaskandprefix", vote.SimplifiedContent);
            Assert.AreEqual("--[x][How?] A simple vote line with task and prefix", vote.ToString());
        }

        [TestMethod]
        public void Construct5()
        {
            VoteLine vote = new VoteLine("[x] Æsir résumé");

            Assert.AreEqual("[x] Æsir résumé", vote.Text);
            Assert.AreEqual(string.Empty, vote.Prefix);
            Assert.AreEqual("x", vote.Marker);
            Assert.AreEqual(MarkerType.Vote, vote.MarkerType);
            Assert.AreEqual(0, vote.MarkerValue);
            Assert.AreEqual(string.Empty, vote.Task);
            Assert.AreEqual("Æsir résumé", vote.Content);
            Assert.AreEqual("Æsir résumé", vote.TrimmedContent);
            Assert.AreEqual("Æsir résumé", vote.DisplayContent);
            Assert.AreEqual("Æsir résumé", vote.ComparableContent);
            Assert.AreEqual("AEsirresume", vote.SimplifiedContent);
            Assert.AreEqual("[x] Æsir résumé", vote.ToString());
        }

        [TestMethod]
        public void Construct6()
        {
            VoteLine vote = new VoteLine("[x] Æsir 『b』résumé『/b』");

            Assert.AreEqual("[x] Æsir 『b』résumé『/b』", vote.Text);
            Assert.AreEqual(string.Empty, vote.Prefix);
            Assert.AreEqual("x", vote.Marker);
            Assert.AreEqual(MarkerType.Vote, vote.MarkerType);
            Assert.AreEqual(0, vote.MarkerValue);
            Assert.AreEqual(string.Empty, vote.Task);
            Assert.AreEqual("Æsir 『b』résumé『/b』", vote.Content);
            Assert.AreEqual("Æsir 『b』résumé『/b』", vote.TrimmedContent);
            Assert.AreEqual("Æsir [b]résumé[/b]", vote.DisplayContent);
            Assert.AreEqual("Æsir résumé", vote.ComparableContent);
            Assert.AreEqual("AEsirresume", vote.SimplifiedContent);
            Assert.AreEqual("[x] Æsir [b]résumé[/b]", vote.ToString());
        }
        #endregion

        #region Basic comparisons
        [TestMethod]
        public void Compare1_Spacing()
        {
            // Spacing variation
            VoteLine vote1 = new VoteLine("- - [x] [How?] A simple vote line with task and prefix");
            VoteLine vote2 = new VoteLine("--[x][How?] a simple vote line with task and prefix");

            Assert.AreEqual(vote1, vote2);
        }

        [TestMethod]
        public void Compare2_Markers()
        {
            // Marker and prefix variation
            VoteLine vote1 = new VoteLine("- - [X] [How?] A simple vote line with task and prefix");
            VoteLine vote2 = new VoteLine("-[✔][How?] a simple vote line with task and prefix");

            Assert.AreEqual(vote1, vote2);
        }

        [TestMethod]
        public void Compare3_Characters()
        {
            // Diacritical variation
            VoteLine vote1 = new VoteLine("-[X][How?] Æsir résumé");
            VoteLine vote2 = new VoteLine("-[✔][How?] aesir resume");

            Assert.AreEqual(vote1, vote2);
        }

        [TestMethod]
        public void Compare4_Tasks()
        {
            // Task variation
            VoteLine vote1 = new VoteLine("-[X][How?] Æsir résumé");
            VoteLine vote2 = new VoteLine("-[✔] aesir resume");

            Assert.AreEqual(vote1, vote2);
        }

        #endregion
    }
}
