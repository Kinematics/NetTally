using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NetTally.Votes;
using NetTally.Votes.Experiment2;

namespace NTTests.Experiment_2
{
    [TestClass]
    public class VoteContentTests
    {
        #region Non-vote Lines
        [TestMethod]
        public void Analyze_basic_text()
        {
            string text = "Something or other";

            var (isVoteLine, flagIgnore, _) = MessageVoteContent.AnalyzeLine(text);

            Assert.IsFalse(isVoteLine);
            Assert.IsFalse(flagIgnore);
        }

        [TestMethod]
        public void Analyze_bad_prefix()
        {
            string text = "--*[x] Is vote?";

            var (isVoteLine, flagIgnore, _) = MessageVoteContent.AnalyzeLine(text);

            Assert.IsFalse(isVoteLine);
            Assert.IsFalse(flagIgnore);
        }

        [TestMethod]
        public void Analyze__preliminary()
        {
            string text = "--[] Preliminary vote";

            var (isVoteLine, flagIgnore, _) = MessageVoteContent.AnalyzeLine(text);

            Assert.IsFalse(isVoteLine);
            Assert.IsFalse(flagIgnore);
        }

        [TestMethod]
        public void Analyze_joke()
        {
            string text = "[jk] just kidding!";

            var (isVoteLine, flagIgnore, _) = MessageVoteContent.AnalyzeLine(text);

            Assert.IsFalse(isVoteLine);
            Assert.IsFalse(flagIgnore);
        }
        #endregion

        #region Ignore this post
        [TestMethod]
        public void Analyze_non_vote()
        {
            string text = "##### NetTally";

            var (isVoteLine, flagIgnore, _) = MessageVoteContent.AnalyzeLine(text);

            Assert.IsFalse(isVoteLine);
            Assert.IsTrue(flagIgnore);
        }

        [TestMethod]
        public void Analyze_non_vote_short()
        {
            string text = "#### NetTally";

            var (isVoteLine, flagIgnore, _) = MessageVoteContent.AnalyzeLine(text);

            Assert.IsFalse(isVoteLine);
            Assert.IsFalse(flagIgnore);
        }

        [TestMethod]
        public void Analyze_non_vote_color()
        {
            string text = "『color=transparent』##### NetTally『/color』";

            var (isVoteLine, flagIgnore, _) = MessageVoteContent.AnalyzeLine(text);

            Assert.IsFalse(isVoteLine);
            Assert.IsTrue(flagIgnore);
        }

        [TestMethod]
        public void Analyze_non_vote_bold()
        {
            string text = "##『b』##『/b』# NetTally";

            var (isVoteLine, flagIgnore, _) = MessageVoteContent.AnalyzeLine(text);

            Assert.IsFalse(isVoteLine);
            Assert.IsTrue(flagIgnore);
        }

        [TestMethod]
        public void Analyze_non_vote_bold_color()
        {
            string text = "『color=transparent』##『b』##『/b』# NetTally『/color』";

            var (isVoteLine, flagIgnore, _) = MessageVoteContent.AnalyzeLine(text);

            Assert.IsFalse(isVoteLine);
            Assert.IsTrue(flagIgnore);
        }
        #endregion

        #region Valid Votes
        [TestMethod]
        public void Analyze_no_prefix()
        {
            string text = "[x] Is vote?";

            var (isVoteLine, flagIgnore, voteLine) = MessageVoteContent.AnalyzeLine(text);

            Assert.IsTrue(isVoteLine);
            Assert.IsFalse(flagIgnore);
            Assert.AreEqual("", voteLine.Prefix);
            Assert.AreEqual("x", voteLine.Marker);
            Assert.AreEqual("", voteLine.Task);
            Assert.AreEqual("Is vote?", voteLine.Content);
            Assert.AreEqual(MarkerType.Vote, voteLine.MarkerType);
            Assert.AreEqual(0, voteLine.MarkerValue);
        }

        [TestMethod]
        public void Analyze_approval_positive()
        {
            string text = "[+] Is vote?";

            var (isVoteLine, flagIgnore, voteLine) = MessageVoteContent.AnalyzeLine(text);

            Assert.IsTrue(isVoteLine);
            Assert.IsFalse(flagIgnore);
            Assert.AreEqual("", voteLine.Prefix);
            Assert.AreEqual("+", voteLine.Marker);
            Assert.AreEqual("", voteLine.Task);
            Assert.AreEqual("Is vote?", voteLine.Content);
            Assert.AreEqual(MarkerType.Approval, voteLine.MarkerType);
            Assert.AreEqual(1, voteLine.MarkerValue);
        }

        [TestMethod]
        public void Analyze_approval_negative()
        {
            string text = "[-] Is vote?";

            var (isVoteLine, flagIgnore, voteLine) = MessageVoteContent.AnalyzeLine(text);

            Assert.IsTrue(isVoteLine);
            Assert.IsFalse(flagIgnore);
            Assert.AreEqual("", voteLine.Prefix);
            Assert.AreEqual("-", voteLine.Marker);
            Assert.AreEqual("", voteLine.Task);
            Assert.AreEqual("Is vote?", voteLine.Content);
            Assert.AreEqual(MarkerType.Approval, voteLine.MarkerType);
            Assert.AreEqual(-1, voteLine.MarkerValue);
        }

        [TestMethod]
        public void Analyze_score()
        {
            string text = "[+9] Is vote?";

            var (isVoteLine, flagIgnore, voteLine) = MessageVoteContent.AnalyzeLine(text);

            Assert.IsTrue(isVoteLine);
            Assert.IsFalse(flagIgnore);
            Assert.AreEqual("", voteLine.Prefix);
            Assert.AreEqual("+9", voteLine.Marker);
            Assert.AreEqual("", voteLine.Task);
            Assert.AreEqual("Is vote?", voteLine.Content);
            Assert.AreEqual(MarkerType.Score, voteLine.MarkerType);
            Assert.AreEqual(9, voteLine.MarkerValue);
        }

        [TestMethod]
        public void Analyze_rank()
        {
            string text = "[#1] Is vote?";

            var (isVoteLine, flagIgnore, voteLine) = MessageVoteContent.AnalyzeLine(text);

            Assert.IsTrue(isVoteLine);
            Assert.IsFalse(flagIgnore);
            Assert.AreEqual("", voteLine.Prefix);
            Assert.AreEqual("#1", voteLine.Marker);
            Assert.AreEqual("", voteLine.Task);
            Assert.AreEqual("Is vote?", voteLine.Content);
            Assert.AreEqual(MarkerType.Rank, voteLine.MarkerType);
            Assert.AreEqual(1, voteLine.MarkerValue);
        }

        [TestMethod]
        public void Analyze_rank_default()
        {
            string text = "[1] Is vote?";

            var (isVoteLine, flagIgnore, voteLine) = MessageVoteContent.AnalyzeLine(text);

            Assert.IsTrue(isVoteLine);
            Assert.IsFalse(flagIgnore);
            Assert.AreEqual("", voteLine.Prefix);
            Assert.AreEqual("1", voteLine.Marker);
            Assert.AreEqual("", voteLine.Task);
            Assert.AreEqual("Is vote?", voteLine.Content);
            Assert.AreEqual(MarkerType.Rank, voteLine.MarkerType);
            Assert.AreEqual(1, voteLine.MarkerValue);
        }

        [TestMethod]
        public void Analyze_task_empty()
        {
            string text = "[x][] Is vote?";

            var (isVoteLine, flagIgnore, voteLine) = MessageVoteContent.AnalyzeLine(text);

            Assert.IsTrue(isVoteLine);
            Assert.IsFalse(flagIgnore);
            Assert.AreEqual("", voteLine.Prefix);
            Assert.AreEqual("x", voteLine.Marker);
            Assert.AreEqual("", voteLine.Task);
            Assert.AreEqual("Is vote?", voteLine.Content);
            Assert.AreEqual(MarkerType.Vote, voteLine.MarkerType);
            Assert.AreEqual(0, voteLine.MarkerValue);
        }

        [TestMethod]
        public void Analyze_task_simple()
        {
            string text = "[x] [simple task] Is vote?";

            var (isVoteLine, flagIgnore, voteLine) = MessageVoteContent.AnalyzeLine(text);

            Assert.IsTrue(isVoteLine);
            Assert.IsFalse(flagIgnore);
            Assert.AreEqual("", voteLine.Prefix);
            Assert.AreEqual("x", voteLine.Marker);
            Assert.AreEqual("simple task", voteLine.Task);
            Assert.AreEqual("Is vote?", voteLine.Content);
            Assert.AreEqual(MarkerType.Vote, voteLine.MarkerType);
            Assert.AreEqual(0, voteLine.MarkerValue);
        }

        [TestMethod]
        public void Analyze_task_simple_bold()
        {
            string text = "[x] [『b』simple『/b』 task] Is vote?";

            var (isVoteLine, flagIgnore, voteLine) = MessageVoteContent.AnalyzeLine(text);

            Assert.IsTrue(isVoteLine);
            Assert.IsFalse(flagIgnore);
            Assert.AreEqual("", voteLine.Prefix);
            Assert.AreEqual("x", voteLine.Marker);
            Assert.AreEqual("simple task", voteLine.Task);
            Assert.AreEqual("Is vote?", voteLine.Content);
            Assert.AreEqual(MarkerType.Vote, voteLine.MarkerType);
            Assert.AreEqual(0, voteLine.MarkerValue);
        }


        [TestMethod]
        public void Analyze_bold_content()
        {
            string text = "[x] 『b』Is vote?『/b』";

            var (isVoteLine, flagIgnore, voteLine) = MessageVoteContent.AnalyzeLine(text);

            Assert.IsTrue(isVoteLine);
            Assert.IsFalse(flagIgnore);
            Assert.AreEqual("", voteLine.Prefix);
            Assert.AreEqual("x", voteLine.Marker);
            Assert.AreEqual("", voteLine.Task);
            Assert.AreEqual("『b』Is vote?『/b』", voteLine.Content);
            Assert.AreEqual(MarkerType.Vote, voteLine.MarkerType);
            Assert.AreEqual(0, voteLine.MarkerValue);
        }

        [TestMethod]
        public void Analyze_bold_task()
        {
            string text = "[x] 『b』[simple task] Is vote?『/b』";

            var (isVoteLine, flagIgnore, voteLine) = MessageVoteContent.AnalyzeLine(text);

            Assert.IsTrue(isVoteLine);
            Assert.IsFalse(flagIgnore);
            Assert.AreEqual("", voteLine.Prefix);
            Assert.AreEqual("x", voteLine.Marker);
            Assert.AreEqual("simple task", voteLine.Task);
            Assert.AreEqual("『b』Is vote?『/b』", voteLine.Content);
            Assert.AreEqual(MarkerType.Vote, voteLine.MarkerType);
            Assert.AreEqual(0, voteLine.MarkerValue);
        }

        [TestMethod]
        public void Analyze_bold_inner_task()
        {
            string text = "[x] 『b』[『b』simple『/b』 task] Is vote?『/b』";

            var (isVoteLine, flagIgnore, voteLine) = MessageVoteContent.AnalyzeLine(text);

            Assert.IsTrue(isVoteLine);
            Assert.IsFalse(flagIgnore);
            Assert.AreEqual("", voteLine.Prefix);
            Assert.AreEqual("x", voteLine.Marker);
            Assert.AreEqual("simple task", voteLine.Task);
            Assert.AreEqual("『b』Is vote?『/b』", voteLine.Content);
            Assert.AreEqual(MarkerType.Vote, voteLine.MarkerType);
            Assert.AreEqual(0, voteLine.MarkerValue);
        }
        #endregion
    }
}
