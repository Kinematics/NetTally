using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NetTally.Votes;

namespace NetTally.Tests.Votes
{
    [TestClass]
    public class VoteLineParserTests
    {
        #region Setup
        static IServiceProvider serviceProvider;

        [ClassInitialize]
        public static void ClassInit(TestContext context)
        {
            serviceProvider = TestStartup.ConfigureServices();
        }
        #endregion Setup

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

        [TestMethod]
        public void NonVote_11()
        {
            string line = "--*[2] Invalid prefix";
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
            string line = "[#100] A normal vote line";
            VoteLine? voteLine = VoteLineParser.ParseLine(line);

            if (voteLine != null)
            {
                Assert.AreEqual("", voteLine.Prefix);
                Assert.AreEqual(0, voteLine.Depth);
                Assert.AreEqual("#100", voteLine.Marker);
                Assert.AreEqual(MarkerType.Rank, voteLine.MarkerType);
                Assert.AreEqual(99, voteLine.MarkerValue);
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
        public void Task_Empty()
        {
            string line = "[X][] A normal vote line";
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
        public void Task_Complex_Spacing()
        {
            string line = "[X]  [Do you~~Go!] A normal vote line";
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
                Assert.AreEqual("A normal vote line", voteLine.CleanContent);
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
                Assert.AreEqual("A normal vote line", voteLine.CleanContent);
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
                Assert.AreEqual("A normal vote line", voteLine.CleanContent);
                Assert.AreEqual("Tasky", voteLine.Task);
            }

            Assert.AreNotEqual(null, voteLine);
        }

        [TestMethod]
        public void General_Bold_Task()
        {
            string line = @"[X]『i』『b』[Shopping]『/b』『/i』 Shopping 1";
            // Poor matching may result in the Content ==> 『i』『b』『/b』『/i』 Shopping 1
            VoteLine? voteLine = VoteLineParser.ParseLine(line);

            if (voteLine != null)
            {
                Assert.AreEqual("", voteLine.Prefix);
                Assert.AreEqual(0, voteLine.Depth);
                Assert.AreEqual("X", voteLine.Marker);
                Assert.AreEqual(MarkerType.Vote, voteLine.MarkerType);
                Assert.AreEqual(100, voteLine.MarkerValue);
                Assert.AreEqual("Shopping 1", voteLine.Content);
                Assert.AreEqual("Shopping 1", voteLine.CleanContent);
                Assert.AreEqual("Shopping", voteLine.Task);
            }

            Assert.AreNotEqual(null, voteLine);
        }

        #endregion

        #region BBCode
        [TestMethod]
        public void Unbalanced_Start()
        {
            string line = "『b』[X][Tasky] A normal vote line";
            VoteLine? voteLine = VoteLineParser.ParseLine(line);

            if (voteLine != null)
            {
                Assert.AreEqual("", voteLine.Prefix);
                Assert.AreEqual(0, voteLine.Depth);
                Assert.AreEqual("X", voteLine.Marker);
                Assert.AreEqual(MarkerType.Vote, voteLine.MarkerType);
                Assert.AreEqual(100, voteLine.MarkerValue);
                Assert.AreEqual("A normal vote line", voteLine.Content);
                Assert.AreEqual("A normal vote line", voteLine.CleanContent);
                Assert.AreEqual("Tasky", voteLine.Task);
            }

            Assert.AreNotEqual(null, voteLine);
        }

        [TestMethod]
        public void Unbalanced_Early_Content()
        {
            string line = "[X][Tasky] A normal 『b』vote line";
            VoteLine? voteLine = VoteLineParser.ParseLine(line);

            if (voteLine != null)
            {
                Assert.AreEqual("", voteLine.Prefix);
                Assert.AreEqual(0, voteLine.Depth);
                Assert.AreEqual("X", voteLine.Marker);
                Assert.AreEqual(MarkerType.Vote, voteLine.MarkerType);
                Assert.AreEqual(100, voteLine.MarkerValue);
                Assert.AreEqual("A normal vote line", voteLine.Content);
                Assert.AreEqual("A normal vote line", voteLine.CleanContent);
                Assert.AreEqual("Tasky", voteLine.Task);
            }

            Assert.AreNotEqual(null, voteLine);
        }

        [TestMethod]
        public void Unbalanced_End()
        {
            string line = "[X][Tasky] A normal vote line『/b』";
            VoteLine? voteLine = VoteLineParser.ParseLine(line);

            if (voteLine != null)
            {
                Assert.AreEqual("", voteLine.Prefix);
                Assert.AreEqual(0, voteLine.Depth);
                Assert.AreEqual("X", voteLine.Marker);
                Assert.AreEqual(MarkerType.Vote, voteLine.MarkerType);
                Assert.AreEqual(100, voteLine.MarkerValue);
                Assert.AreEqual("A normal vote line", voteLine.Content);
                Assert.AreEqual("A normal vote line", voteLine.CleanContent);
                Assert.AreEqual("Tasky", voteLine.Task);
            }

            Assert.AreNotEqual(null, voteLine);
        }

        [TestMethod]
        public void Unbalanced_Multiple()
        {
            string line = "[X][Tasky] 『b』『i』Vote『/i』 for stuff『/b』";
            VoteLine? voteLine = VoteLineParser.ParseLine(line);

            if (voteLine != null)
            {
                Assert.AreEqual("", voteLine.Prefix);
                Assert.AreEqual(0, voteLine.Depth);
                Assert.AreEqual("X", voteLine.Marker);
                Assert.AreEqual(MarkerType.Vote, voteLine.MarkerType);
                Assert.AreEqual(100, voteLine.MarkerValue);
                Assert.AreEqual("『b』『i』Vote『/i』 for stuff『/b』", voteLine.Content);
                Assert.AreEqual("Vote for stuff", voteLine.CleanContent);
                Assert.AreEqual("Tasky", voteLine.Task);
            }

            Assert.AreNotEqual(null, voteLine);
        }

        [TestMethod]
        public void Unbalanced_Multiple_Missing_End()
        {
            string line = "[X][Tasky] 『b』『i』Vote『/i』 for stuff";
            VoteLine? voteLine = VoteLineParser.ParseLine(line);

            if (voteLine != null)
            {
                Assert.AreEqual("", voteLine.Prefix);
                Assert.AreEqual(0, voteLine.Depth);
                Assert.AreEqual("X", voteLine.Marker);
                Assert.AreEqual(MarkerType.Vote, voteLine.MarkerType);
                Assert.AreEqual(100, voteLine.MarkerValue);
                Assert.AreEqual("『i』Vote『/i』 for stuff", voteLine.Content);
                Assert.AreEqual("Vote for stuff", voteLine.CleanContent);
                Assert.AreEqual("Tasky", voteLine.Task);
            }

            Assert.AreNotEqual(null, voteLine);
        }

        [TestMethod]
        public void Unbalanced_Multiple_Missing_Middle()
        {
            string line = "[X][Tasky] 『b』『i』Vote for stuff『/b』";
            VoteLine? voteLine = VoteLineParser.ParseLine(line);

            if (voteLine != null)
            {
                Assert.AreEqual("", voteLine.Prefix);
                Assert.AreEqual(0, voteLine.Depth);
                Assert.AreEqual("X", voteLine.Marker);
                Assert.AreEqual(MarkerType.Vote, voteLine.MarkerType);
                Assert.AreEqual(100, voteLine.MarkerValue);
                Assert.AreEqual("『b』Vote for stuff『/b』", voteLine.Content);
                Assert.AreEqual("Vote for stuff", voteLine.CleanContent);
                Assert.AreEqual("Tasky", voteLine.Task);
            }

            Assert.AreNotEqual(null, voteLine);
        }

        [TestMethod]
        public void Unbalanced_Multiple_Long_Colors()
        {
            string line = "[X][Tasky] Brutalize them. You haven’t had a chance to properly fight in 『/color』『i』『color=#ebebeb』years『/color』『/i』『color=#ebebeb』, and spars can only do so much. How thoughtful of the Herans to volunteer!";
            VoteLine? voteLine = VoteLineParser.ParseLine(line);

            if (voteLine != null)
            {
                Assert.AreEqual("", voteLine.Prefix);
                Assert.AreEqual(0, voteLine.Depth);
                Assert.AreEqual("X", voteLine.Marker);
                Assert.AreEqual(MarkerType.Vote, voteLine.MarkerType);
                Assert.AreEqual(100, voteLine.MarkerValue);
                // Convert ’ to '
                Assert.AreEqual("Brutalize them. You haven't had a chance to properly fight in 『i』『color=#ebebeb』years『/color』『/i』, and spars can only do so much. How thoughtful of the Herans to volunteer!", voteLine.Content);
                Assert.AreEqual("Brutalize them. You haven't had a chance to properly fight in years, and spars can only do so much. How thoughtful of the Herans to volunteer!", voteLine.CleanContent);
                Assert.AreEqual("Tasky", voteLine.Task);
            }

            Assert.AreNotEqual(null, voteLine);
        }

        #endregion

        #region Strip BBCode
        [TestMethod]
        public void Strip_BBCode_None()
        {
            string cleanExample = @"Text Nagisa's uncle about her visiting today. Establish a specific time. (Keep in mind Sayaka's hospital visit.)";

            string result = VoteLineParser.StripBBCode(cleanExample);
            Assert.AreEqual(cleanExample, result);
        }

        [TestMethod]
        public void Strip_BBCode_Basic()
        {
            string cleanExample = @"Text Nagisa's uncle about her visiting today. Establish a specific time. (Keep in mind Sayaka's hospital visit.)";
            string mediumExample = @"Text Nagisa's uncle about her 『i』visiting『/i』 today. Establish a specific time. (Keep in mind Sayaka's hospital visit.)";

            string result = VoteLineParser.StripBBCode(mediumExample);
            Assert.AreEqual(cleanExample, result);
        }

        [TestMethod]
        public void Strip_BBCode_More()
        {
            string cleanExample = @"Text Nagisa's uncle about her visiting today. Establish a specific time. (Keep in mind Sayaka's hospital visit.)";
            string largeExample = @"Text Nagisa's uncle about her 『i』visiting『/i』 today. Establish a 『b』specific time『/b』. 『color=orange』(Keep in mind Sayaka's hospital visit.)『/color』";

            string result = VoteLineParser.StripBBCode(largeExample);
            Assert.AreEqual(cleanExample, result);
        }

        [TestMethod]
        public void Strip_BBCode_Image_Url()
        {
            string clean = "Fancy <Image>";
            string imageExample = @"Fancy 『url='http://google.com/image/1.jpg'』<Image>『/url』";

            string result = VoteLineParser.StripBBCode(imageExample);
            Assert.AreEqual(clean, result);
        }

        [TestMethod]
        public void Strip_BBCode_UserRef_Url()
        {
            string clean = "@Xryuran";
            string urlExample = @"『url=""https://forum.questionablequesting.com/members/2392/""』@Xryuran『/url』";

            string result = VoteLineParser.StripBBCode(urlExample);
            Assert.AreEqual(clean, result);
        }

        [TestMethod]
        public void Strip_BBCode_Url()
        {
            string clean = "Vote for me!";
            string urlExample = @"Vote for 『url=""http://google.com/myhome.html""』me『/url』!";

            string result = VoteLineParser.StripBBCode(urlExample);
            Assert.AreEqual(clean, result);
        }

        #endregion
    }
}
