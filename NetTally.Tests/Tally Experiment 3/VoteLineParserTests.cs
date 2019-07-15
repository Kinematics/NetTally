using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NetTally.Votes;

namespace NetTally.Tests.Votes
{
    [TestClass]
    public class VoteLineParserTests
    {
        static IServiceProvider serviceProvider;

        [ClassInitialize]
        public static void ClassInit(TestContext context)
        {
            serviceProvider = TestStartup.ConfigureServices();
        }

        #region Non-votes
        [TestMethod]
        public void NonVote_1()
        {
            string line = "A basic vote line";
            VoteLine? voteLine = VoteLineParser.ParseLine(line);

            Assert.AreEqual(null, voteLine);
        }

        [TestMethod]
        public void NonVote_2()
        {
            string line = "A discussion.  Make sure to [X] your vote.";
            VoteLine? voteLine = VoteLineParser.ParseLine(line);

            Assert.AreEqual(null, voteLine);
        }

        [TestMethod]
        public void NonVote_3()
        {
            string line = "[JK] A joke vote";
            VoteLine? voteLine = VoteLineParser.ParseLine(line);

            Assert.AreEqual(null, voteLine);
        }

        [TestMethod]
        public void NonVote_4()
        {
            string line = "--- Waaaait a minute...";
            VoteLine? voteLine = VoteLineParser.ParseLine(line);

            Assert.AreEqual(null, voteLine);
        }

        [TestMethod]
        public void NonVote_5()
        {
            string line = "[] Forgot to x the vote";
            VoteLine? voteLine = VoteLineParser.ParseLine(line);

            Assert.AreEqual(null, voteLine);
        }

        [TestMethod]
        public void NonVote_6()
        {
            string line = "{x] Wrong brackets";
            VoteLine? voteLine = VoteLineParser.ParseLine(line);

            Assert.AreEqual(null, voteLine);
        }

        [TestMethod]
        public void NonVote_7()
        {
            string line = "[[x] More wrong brackets";
            VoteLine? voteLine = VoteLineParser.ParseLine(line);

            Assert.AreEqual(null, voteLine);
        }

        [TestMethod]
        public void NonVote_8()
        {
            string line = "[-x] Improperly placed prefix";
            VoteLine? voteLine = VoteLineParser.ParseLine(line);

            Assert.AreEqual(null, voteLine);
        }

        [TestMethod]
        public void NonVote_9()
        {
            string line = "~~[x] Invalid prefix";
            VoteLine? voteLine = VoteLineParser.ParseLine(line);

            Assert.AreEqual(null, voteLine);
        }

        [TestMethod]
        public void NonVote_10()
        {
            string line = "[-2] Invalid rank";
            VoteLine? voteLine = VoteLineParser.ParseLine(line);

            Assert.AreEqual(null, voteLine);
        }

        #endregion

        #region Prefixes
        [TestMethod]
        public void Prefix_0()
        {
            string line = "[x] A normal vote line";
            VoteLine? voteLine = VoteLineParser.ParseLine(line);

            if (voteLine != null)
            {
                Assert.AreEqual("", voteLine.Prefix);
                Assert.AreEqual(0, voteLine.Depth);
                Assert.AreEqual("x", voteLine.Marker);
                Assert.AreEqual(MarkerType.Vote, voteLine.MarkerType);
                Assert.AreEqual(100, voteLine.MarkerValue);
                Assert.AreEqual("A normal vote line", voteLine.Content);
                Assert.AreEqual("", voteLine.Task);
            }

            Assert.AreNotEqual(null, voteLine);
        }

        [TestMethod]
        public void Prefix_1()
        {
            string line = "-[x] A normal vote line";
            VoteLine? voteLine = VoteLineParser.ParseLine(line);

            if (voteLine != null)
            {
                Assert.AreEqual("-", voteLine.Prefix);
                Assert.AreEqual(1, voteLine.Depth);
                Assert.AreEqual("x", voteLine.Marker);
                Assert.AreEqual(MarkerType.Vote, voteLine.MarkerType);
                Assert.AreEqual(100, voteLine.MarkerValue);
                Assert.AreEqual("A normal vote line", voteLine.Content);
                Assert.AreEqual("", voteLine.Task);
            }

            Assert.AreNotEqual(null, voteLine);
        }

        [TestMethod]
        public void Prefix_MultiDash()
        {
            string line = "---[x] A normal vote line";
            VoteLine? voteLine = VoteLineParser.ParseLine(line);

            if (voteLine != null)
            {
                Assert.AreEqual("---", voteLine.Prefix);
                Assert.AreEqual(3, voteLine.Depth);
                Assert.AreEqual("x", voteLine.Marker);
                Assert.AreEqual(MarkerType.Vote, voteLine.MarkerType);
                Assert.AreEqual(100, voteLine.MarkerValue);
                Assert.AreEqual("A normal vote line", voteLine.Content);
                Assert.AreEqual("", voteLine.Task);
            }

            Assert.AreNotEqual(null, voteLine);
        }

        [TestMethod]
        public void Prefix_MultiDash_Space()
        {
            string line = "- - - [x] A normal vote line";
            VoteLine? voteLine = VoteLineParser.ParseLine(line);

            if (voteLine != null)
            {
                Assert.AreEqual("---", voteLine.Prefix);
                Assert.AreEqual(3, voteLine.Depth);
                Assert.AreEqual("x", voteLine.Marker);
                Assert.AreEqual(MarkerType.Vote, voteLine.MarkerType);
                Assert.AreEqual(100, voteLine.MarkerValue);
                Assert.AreEqual("A normal vote line", voteLine.Content);
                Assert.AreEqual("", voteLine.Task);
            }

            Assert.AreNotEqual(null, voteLine);
        }

        [TestMethod]
        public void Prefix_MultiEmDash_Space()
        {
            string line = "— — — —[x] A normal vote line";
            VoteLine? voteLine = VoteLineParser.ParseLine(line);

            if (voteLine != null)
            {
                Assert.AreEqual("————", voteLine.Prefix);
                Assert.AreEqual(4, voteLine.Depth);
                Assert.AreEqual("x", voteLine.Marker);
                Assert.AreEqual(MarkerType.Vote, voteLine.MarkerType);
                Assert.AreEqual(100, voteLine.MarkerValue);
                Assert.AreEqual("A normal vote line", voteLine.Content);
                Assert.AreEqual("", voteLine.Task);
            }

            Assert.AreNotEqual(null, voteLine);
        }

        [TestMethod]
        public void Prefix_DashEmDash()
        {
            string line = "-—[x] A normal vote line";
            VoteLine? voteLine = VoteLineParser.ParseLine(line);

            if (voteLine != null)
            {
                Assert.AreEqual("-—", voteLine.Prefix);
                Assert.AreEqual(2, voteLine.Depth);
                Assert.AreEqual("x", voteLine.Marker);
                Assert.AreEqual(MarkerType.Vote, voteLine.MarkerType);
                Assert.AreEqual(100, voteLine.MarkerValue);
                Assert.AreEqual("A normal vote line", voteLine.Content);
                Assert.AreEqual("", voteLine.Task);
            }

            Assert.AreNotEqual(null, voteLine);
        }

        #endregion

        #region Markers
        [TestMethod]
        public void Marker_X()
        {
            string line = "[x] A normal vote line";
            VoteLine? voteLine = VoteLineParser.ParseLine(line);

            if (voteLine != null)
            {
                Assert.AreEqual("", voteLine.Prefix);
                Assert.AreEqual(0, voteLine.Depth);
                Assert.AreEqual("x", voteLine.Marker);
                Assert.AreEqual(MarkerType.Vote, voteLine.MarkerType);
                Assert.AreEqual(100, voteLine.MarkerValue);
                Assert.AreEqual("A normal vote line", voteLine.Content);
                Assert.AreEqual("", voteLine.Task);
            }

            Assert.AreNotEqual(null, voteLine);
        }

        [TestMethod]
        public void Marker_CapitalX()
        {
            string line = "[X] A normal vote line";
            VoteLine? voteLine = VoteLineParser.ParseLine(line);

            if (voteLine != null)
            {
                Assert.AreEqual("", voteLine.Prefix);
                Assert.AreEqual(0, voteLine.Depth);
                Assert.AreEqual("X", voteLine.Marker);
                Assert.AreEqual(MarkerType.Vote, voteLine.MarkerType);
                Assert.AreEqual(100, voteLine.MarkerValue);
                Assert.AreEqual("A normal vote line", voteLine.Content);
                Assert.AreEqual("", voteLine.Task);
            }

            Assert.AreNotEqual(null, voteLine);
        }

        [TestMethod]
        public void Marker_Checkmark()
        {
            string line = "[✓] A normal vote line";
            VoteLine? voteLine = VoteLineParser.ParseLine(line);

            if (voteLine != null)
            {
                Assert.AreEqual("", voteLine.Prefix);
                Assert.AreEqual(0, voteLine.Depth);
                Assert.AreEqual("✓", voteLine.Marker);
                Assert.AreEqual(MarkerType.Vote, voteLine.MarkerType);
                Assert.AreEqual(100, voteLine.MarkerValue);
                Assert.AreEqual("A normal vote line", voteLine.Content);
                Assert.AreEqual("", voteLine.Task);
            }

            Assert.AreNotEqual(null, voteLine);
        }

        [TestMethod]
        public void Marker_Box()
        {
            string line = "☒ A normal vote line";
            VoteLine? voteLine = VoteLineParser.ParseLine(line);

            if (voteLine != null)
            {
                Assert.AreEqual("", voteLine.Prefix);
                Assert.AreEqual(0, voteLine.Depth);
                Assert.AreEqual("☒", voteLine.Marker);
                Assert.AreEqual(MarkerType.Vote, voteLine.MarkerType);
                Assert.AreEqual(100, voteLine.MarkerValue);
                Assert.AreEqual("A normal vote line", voteLine.Content);
                Assert.AreEqual("", voteLine.Task);
            }

            Assert.AreNotEqual(null, voteLine);
        }

        [TestMethod]
        public void Marker_Checkbox()
        {
            string line = "☑ A normal vote line";
            VoteLine? voteLine = VoteLineParser.ParseLine(line);

            if (voteLine != null)
            {
                Assert.AreEqual("", voteLine.Prefix);
                Assert.AreEqual(0, voteLine.Depth);
                Assert.AreEqual("☑", voteLine.Marker);
                Assert.AreEqual(MarkerType.Vote, voteLine.MarkerType);
                Assert.AreEqual(100, voteLine.MarkerValue);
                Assert.AreEqual("A normal vote line", voteLine.Content);
                Assert.AreEqual("", voteLine.Task);
            }

            Assert.AreNotEqual(null, voteLine);
        }

        [TestMethod]
        public void Marker_BracketedBox()
        {
            string line = "[☒] A normal vote line";
            VoteLine? voteLine = VoteLineParser.ParseLine(line);

            if (voteLine != null)
            {
                Assert.AreEqual("", voteLine.Prefix);
                Assert.AreEqual(0, voteLine.Depth);
                Assert.AreEqual("☒", voteLine.Marker);
                Assert.AreEqual(MarkerType.Vote, voteLine.MarkerType);
                Assert.AreEqual(100, voteLine.MarkerValue);
                Assert.AreEqual("A normal vote line", voteLine.Content);
                Assert.AreEqual("", voteLine.Task);
            }

            Assert.AreNotEqual(null, voteLine);
        }

        [TestMethod]
        public void Marker_BracketedCheckbox()
        {
            string line = "[☑] A normal vote line";
            VoteLine? voteLine = VoteLineParser.ParseLine(line);

            if (voteLine != null)
            {
                Assert.AreEqual("", voteLine.Prefix);
                Assert.AreEqual(0, voteLine.Depth);
                Assert.AreEqual("☑", voteLine.Marker);
                Assert.AreEqual(MarkerType.Vote, voteLine.MarkerType);
                Assert.AreEqual(100, voteLine.MarkerValue);
                Assert.AreEqual("A normal vote line", voteLine.Content);
                Assert.AreEqual("", voteLine.Task);
            }

            Assert.AreNotEqual(null, voteLine);
        }

        [TestMethod]
        public void Marker_Rank()
        {
            string line = "[#1] A normal vote line";
            VoteLine? voteLine = VoteLineParser.ParseLine(line);

            if (voteLine != null)
            {
                Assert.AreEqual("", voteLine.Prefix);
                Assert.AreEqual(0, voteLine.Depth);
                Assert.AreEqual("#1", voteLine.Marker);
                Assert.AreEqual(MarkerType.Rank, voteLine.MarkerType);
                Assert.AreEqual(1, voteLine.MarkerValue);
                Assert.AreEqual("A normal vote line", voteLine.Content);
                Assert.AreEqual("", voteLine.Task);
            }

            Assert.AreNotEqual(null, voteLine);
        }

        [TestMethod]
        public void Marker_DefaultRank()
        {
            string line = "[1] A normal vote line";
            VoteLine? voteLine = VoteLineParser.ParseLine(line);

            if (voteLine != null)
            {
                Assert.AreEqual("", voteLine.Prefix);
                Assert.AreEqual(0, voteLine.Depth);
                Assert.AreEqual("1", voteLine.Marker);
                Assert.AreEqual(MarkerType.Rank, voteLine.MarkerType);
                Assert.AreEqual(1, voteLine.MarkerValue);
                Assert.AreEqual("A normal vote line", voteLine.Content);
                Assert.AreEqual("", voteLine.Task);
            }

            Assert.AreNotEqual(null, voteLine);
        }

        [TestMethod]
        public void Marker_HighRank()
        {
            string line = "[#10] A normal vote line";
            VoteLine? voteLine = VoteLineParser.ParseLine(line);

            if (voteLine != null)
            {
                Assert.AreEqual("", voteLine.Prefix);
                Assert.AreEqual(0, voteLine.Depth);
                Assert.AreEqual("#10", voteLine.Marker);
                Assert.AreEqual(MarkerType.Rank, voteLine.MarkerType);
                Assert.AreEqual(9, voteLine.MarkerValue);
                Assert.AreEqual("A normal vote line", voteLine.Content);
                Assert.AreEqual("", voteLine.Task);
            }

            Assert.AreNotEqual(null, voteLine);
        }

        [TestMethod]
        public void Marker_Score()
        {
            string line = "[95%] A normal vote line";
            VoteLine? voteLine = VoteLineParser.ParseLine(line);

            if (voteLine != null)
            {
                Assert.AreEqual("", voteLine.Prefix);
                Assert.AreEqual(0, voteLine.Depth);
                Assert.AreEqual("95%", voteLine.Marker);
                Assert.AreEqual(MarkerType.Score, voteLine.MarkerType);
                Assert.AreEqual(95, voteLine.MarkerValue);
                Assert.AreEqual("A normal vote line", voteLine.Content);
                Assert.AreEqual("", voteLine.Task);
            }

            Assert.AreNotEqual(null, voteLine);
        }

        [TestMethod]
        public void Marker_Score_Overflow()
        {
            string line = "[200%] A normal vote line";
            VoteLine? voteLine = VoteLineParser.ParseLine(line);

            if (voteLine != null)
            {
                Assert.AreEqual("", voteLine.Prefix);
                Assert.AreEqual(0, voteLine.Depth);
                Assert.AreEqual("200%", voteLine.Marker);
                Assert.AreEqual(MarkerType.Score, voteLine.MarkerType);
                Assert.AreEqual(100, voteLine.MarkerValue);
                Assert.AreEqual("A normal vote line", voteLine.Content);
                Assert.AreEqual("", voteLine.Task);
            }

            Assert.AreNotEqual(null, voteLine);
        }

        [TestMethod]
        public void Marker_Approval()
        {
            string line = "[+] A normal vote line";
            VoteLine? voteLine = VoteLineParser.ParseLine(line);

            if (voteLine != null)
            {
                Assert.AreEqual("", voteLine.Prefix);
                Assert.AreEqual(0, voteLine.Depth);
                Assert.AreEqual("+", voteLine.Marker);
                Assert.AreEqual(MarkerType.Approval, voteLine.MarkerType);
                Assert.AreEqual(80, voteLine.MarkerValue);
                Assert.AreEqual("A normal vote line", voteLine.Content);
                Assert.AreEqual("", voteLine.Task);
            }

            Assert.AreNotEqual(null, voteLine);
        }

        [TestMethod]
        public void Marker_Disapproval()
        {
            string line = "[-] A normal vote line";
            VoteLine? voteLine = VoteLineParser.ParseLine(line);

            if (voteLine != null)
            {
                Assert.AreEqual("", voteLine.Prefix);
                Assert.AreEqual(0, voteLine.Depth);
                Assert.AreEqual("-", voteLine.Marker);
                Assert.AreEqual(MarkerType.Approval, voteLine.MarkerType);
                Assert.AreEqual(20, voteLine.MarkerValue);
                Assert.AreEqual("A normal vote line", voteLine.Content);
                Assert.AreEqual("", voteLine.Task);
            }

            Assert.AreNotEqual(null, voteLine);
        }


        #endregion

        #region Tasks
        [TestMethod]
        public void Task_Basic()
        {
            string line = "[X][Tasky] A normal vote line";
            VoteLine? voteLine = VoteLineParser.ParseLine(line);

            if (voteLine != null)
            {
                Assert.AreEqual("", voteLine.Prefix);
                Assert.AreEqual(0, voteLine.Depth);
                Assert.AreEqual("X", voteLine.Marker);
                Assert.AreEqual(MarkerType.Vote, voteLine.MarkerType);
                Assert.AreEqual(100, voteLine.MarkerValue);
                Assert.AreEqual("A normal vote line", voteLine.Content);
                Assert.AreEqual("Tasky", voteLine.Task);
            }

            Assert.AreNotEqual(null, voteLine);
        }

        [TestMethod]
        public void Task_Complex()
        {
            string line = "[X][Do you~~Go!] A normal vote line";
            VoteLine? voteLine = VoteLineParser.ParseLine(line);

            if (voteLine != null)
            {
                Assert.AreEqual("", voteLine.Prefix);
                Assert.AreEqual(0, voteLine.Depth);
                Assert.AreEqual("X", voteLine.Marker);
                Assert.AreEqual(MarkerType.Vote, voteLine.MarkerType);
                Assert.AreEqual(100, voteLine.MarkerValue);
                Assert.AreEqual("A normal vote line", voteLine.Content);
                Assert.AreEqual("Do you~~Go!", voteLine.Task);
            }

            Assert.AreNotEqual(null, voteLine);
        }

        [TestMethod]
        public void Task_Url()
        {
            string line = "[X][『url=http://example.com』Tasky『/url』] A normal vote line";
            VoteLine? voteLine = VoteLineParser.ParseLine(line);

            if (voteLine != null)
            {
                Assert.AreEqual("", voteLine.Prefix);
                Assert.AreEqual(0, voteLine.Depth);
                Assert.AreEqual("X", voteLine.Marker);
                Assert.AreEqual(MarkerType.Vote, voteLine.MarkerType);
                Assert.AreEqual(100, voteLine.MarkerValue);
                Assert.AreEqual("A normal vote line", voteLine.Content);
                Assert.AreEqual("Tasky", voteLine.Task);
            }

            Assert.AreNotEqual(null, voteLine);
        }

        [TestMethod]
        public void Task_Bold()
        {
            string line = "[X][『b』Tasky『/b』] A normal vote line";
            VoteLine? voteLine = VoteLineParser.ParseLine(line);

            if (voteLine != null)
            {
                Assert.AreEqual("", voteLine.Prefix);
                Assert.AreEqual(0, voteLine.Depth);
                Assert.AreEqual("X", voteLine.Marker);
                Assert.AreEqual(MarkerType.Vote, voteLine.MarkerType);
                Assert.AreEqual(100, voteLine.MarkerValue);
                Assert.AreEqual("A normal vote line", voteLine.Content);
                Assert.AreEqual("Tasky", voteLine.Task);
            }

            Assert.AreNotEqual(null, voteLine);
        }

        #endregion

        #region General
        [TestMethod]
        public void General_Bold_Full()
        {
            string line = "『b』[X][Tasky] A normal vote line『/b』";
            VoteLine? voteLine = VoteLineParser.ParseLine(line);

            if (voteLine != null)
            {
                Assert.AreEqual("", voteLine.Prefix);
                Assert.AreEqual(0, voteLine.Depth);
                Assert.AreEqual("X", voteLine.Marker);
                Assert.AreEqual(MarkerType.Vote, voteLine.MarkerType);
                Assert.AreEqual(100, voteLine.MarkerValue);
                Assert.AreEqual("A normal vote line", voteLine.Content);
                Assert.AreEqual("Tasky", voteLine.Task);
            }

            Assert.AreNotEqual(null, voteLine);
        }

        [TestMethod]
        public void General_Bold_Partial()
        {
            string line = "[X][『b』Tasky] A normal vote line『/b』";
            VoteLine? voteLine = VoteLineParser.ParseLine(line);

            if (voteLine != null)
            {
                Assert.AreEqual("", voteLine.Prefix);
                Assert.AreEqual(0, voteLine.Depth);
                Assert.AreEqual("X", voteLine.Marker);
                Assert.AreEqual(MarkerType.Vote, voteLine.MarkerType);
                Assert.AreEqual(100, voteLine.MarkerValue);
                Assert.AreEqual("A normal vote line", voteLine.Content);
                Assert.AreEqual("Tasky", voteLine.Task);
            }

            Assert.AreNotEqual(null, voteLine);
        }

        [TestMethod]
        public void General_Bold_Content()
        {
            string line = "[X][Tasky] A 『b』normal『/b』 vote line";
            VoteLine? voteLine = VoteLineParser.ParseLine(line);

            if (voteLine != null)
            {
                Assert.AreEqual("", voteLine.Prefix);
                Assert.AreEqual(0, voteLine.Depth);
                Assert.AreEqual("X", voteLine.Marker);
                Assert.AreEqual(MarkerType.Vote, voteLine.MarkerType);
                Assert.AreEqual(100, voteLine.MarkerValue);
                Assert.AreEqual("A 『b』normal『/b』 vote line", voteLine.Content);
                Assert.AreEqual("Tasky", voteLine.Task);
            }

            Assert.AreNotEqual(null, voteLine);
        }

        [TestMethod]
        public void General_Bold_Task()
        {
            string line = @"[X]『i』『b』[Shopping]『/b』『/i』 Shopping 1";
            // Poor matching may result in the Content == 『i』『b』『/b』『/i』 Shopping 1
            VoteLine? voteLine = VoteLineParser.ParseLine(line);

            if (voteLine != null)
            {
                Assert.AreEqual("", voteLine.Prefix);
                Assert.AreEqual(0, voteLine.Depth);
                Assert.AreEqual("X", voteLine.Marker);
                Assert.AreEqual(MarkerType.Vote, voteLine.MarkerType);
                Assert.AreEqual(100, voteLine.MarkerValue);
                Assert.AreEqual("Shopping 1", voteLine.Content);
                Assert.AreEqual("Shopping", voteLine.Task);
            }

            Assert.AreNotEqual(null, voteLine);
        }

        #endregion
    }
}
