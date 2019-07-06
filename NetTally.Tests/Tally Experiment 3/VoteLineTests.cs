using System;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NetTally.Experiment3;
using NetTally.Votes;

namespace NetTally.Tests.Experiment3
{
    [TestClass]
    public class VoteLineTests
    {
        static IServiceProvider serviceProvider;

        [ClassInitialize]
        public static void ClassInit(TestContext context)
        {
            serviceProvider = TestStartup.ConfigureServices();
        }

        [TestMethod]
        public void Construct_Empty()
        {
            VoteLine vote = new VoteLine("", "", "", "", MarkerType.None, 0);

            Assert.AreEqual(VoteLine.Empty, vote);
        }

        #region Comparisons
        [TestMethod]
        public void Compare_Same()
        {
            VoteLine vote1 = new VoteLine("", "X", "", "A basic vote line", MarkerType.Vote, 100);
            VoteLine vote2 = new VoteLine("", "X", "", "A basic vote line", MarkerType.Vote, 100);

            Assert.AreEqual(vote1, vote2);
        }

        [TestMethod]
        public void Compare_Marker_Case()
        {
            VoteLine vote1 = new VoteLine("", "X", "", "A basic vote line", MarkerType.Vote, 100);
            VoteLine vote2 = new VoteLine("", "x", "", "A basic vote line", MarkerType.Vote, 100);

            Assert.AreEqual(vote1, vote2);
        }

        [TestMethod]
        public void Compare_Marker_Spacing()
        {
            VoteLine vote1 = new VoteLine("", "X", "", "A basic vote line", MarkerType.Vote, 100);
            VoteLine vote2 = new VoteLine("", "X", "", "A  basicvoteline", MarkerType.Vote, 100);

            Assert.AreEqual(vote1, vote2);
        }

        [TestMethod]
        public void Compare_Content_Case()
        {
            VoteLine vote1 = new VoteLine("", "X", "", "A basic vote line", MarkerType.Vote, 100);
            VoteLine vote2 = new VoteLine("", "X", "", "A BASIC vote Line", MarkerType.Vote, 100);

            Assert.AreEqual(vote1, vote2);
        }

        [TestMethod]
        public void Compare_Content_Score()
        {
            VoteLine vote1 = new VoteLine("", "X", "", "A basic vote line", MarkerType.Vote, 100);
            VoteLine vote2 = new VoteLine("", "100%", "", "A basic vote line", MarkerType.Score, 100);

            Assert.AreEqual(vote1, vote2);
        }

        [TestMethod]
        public void Compare_Prefix_1()
        {
            VoteLine vote1 = new VoteLine("", "X", "", "A basic vote line", MarkerType.Vote, 100);
            VoteLine vote2 = new VoteLine("-", "X", "", "A basic vote line", MarkerType.Vote, 100);

            Assert.AreNotEqual(vote1, vote2);
        }

        [TestMethod]
        public void Compare_Prefix_2()
        {
            VoteLine vote1 = new VoteLine("", "X", "", "A basic vote line", MarkerType.Vote, 100);
            VoteLine vote2 = new VoteLine("---", "X", "", "A basic vote line", MarkerType.Vote, 100);

            Assert.AreNotEqual(vote1, vote2);
        }

        [TestMethod]
        public void Compare_Score()
        {
            VoteLine vote1 = new VoteLine("", "50%", "", "A basic vote line", MarkerType.Score, 50);
            VoteLine vote2 = new VoteLine("", "75%", "", "A basic vote line", MarkerType.Score, 75);

            Assert.AreEqual(vote1, vote2);
        }

        [TestMethod]
        public void Compare_Score_Rank()
        {
            VoteLine vote1 = new VoteLine("", "#1", "", "A basic vote line", MarkerType.Rank, 1);
            VoteLine vote2 = new VoteLine("", "100%", "", "A basic vote line", MarkerType.Score, 100);

            Assert.AreEqual(vote1, vote2);
        }

        [TestMethod]
        public void Compare_Task_1()
        {
            VoteLine vote1 = new VoteLine("", "X", "Hallow", "A basic vote line", MarkerType.Vote, 100);
            VoteLine vote2 = new VoteLine("", "X", "Hallow", "A basic vote line", MarkerType.Vote, 100);

            Assert.AreEqual(vote1, vote2);
        }

        [TestMethod]
        public void Compare_Task_2()
        {
            VoteLine vote1 = new VoteLine("", "X", "HALLOW", "A basic vote line", MarkerType.Vote, 100);
            VoteLine vote2 = new VoteLine("", "X", "hallow", "A basic vote line", MarkerType.Vote, 100);

            Assert.AreEqual(vote1, vote2);
        }

        [TestMethod]
        public void Compare_Task_3()
        {
            VoteLine vote1 = new VoteLine("", "X", "HALLOW", "A basic vote line", MarkerType.Vote, 100);
            VoteLine vote2 = new VoteLine("", "X", "hallo", "A basic vote line", MarkerType.Vote, 100);

            Assert.AreNotEqual(vote1, vote2);
        }

        [TestMethod]
        public void Compare_BBCode_1()
        {
            VoteLine vote1 = new VoteLine("", "X", "", "A normal vote line", MarkerType.Vote, 100);
            VoteLine vote2 = new VoteLine("", "X", "", "A 『b』normal『/b』 vote line", MarkerType.Vote, 100);

            Assert.AreEqual(vote1, vote2);
        }

        [TestMethod]
        public void Compare_BBCode_2()
        {
            VoteLine vote1 = new VoteLine("", "X", "", "A normal vote line", MarkerType.Vote, 100);
            VoteLine vote2 = new VoteLine("", "X", "", "『b』A normal vote line『/b』", MarkerType.Vote, 100);

            Assert.AreEqual(vote1, vote2);
        }

        [TestMethod]
        public void Compare_BBCode_3()
        {
            VoteLine vote1 = new VoteLine("", "X", "", "A normal vote line", MarkerType.Vote, 100);
            VoteLine vote2 = new VoteLine("", "X", "", "『b』A 『url='http://example.com/image.jpg'』normal『/url』 vote line『/b』", MarkerType.Vote, 100);

            Assert.AreEqual(vote1, vote2);
        }



        #endregion

        #region Promotion
        [TestMethod]
        public void Promote_Default_NoPrefix()
        {
            VoteLine vote = new VoteLine("", "X", "", "A basic vote line", MarkerType.Vote, 100);
            var promotedLine = vote.GetPromotedLine();

            Assert.AreEqual("", promotedLine.Prefix);
            Assert.AreEqual(0, promotedLine.Depth);
        }

        [TestMethod]
        public void Promote_Default_OnePrefix_Dash()
        {
            VoteLine vote = new VoteLine("-", "X", "", "A basic vote line", MarkerType.Vote, 100);
            var promotedLine = vote.GetPromotedLine();

            Assert.AreEqual("", promotedLine.Prefix);
            Assert.AreEqual(0, promotedLine.Depth);
        }

        [TestMethod]
        public void Promote_Default_SomePrefix_Dash()
        {
            VoteLine vote = new VoteLine("--", "X", "", "A basic vote line", MarkerType.Vote, 100);
            var promotedLine = vote.GetPromotedLine();

            Assert.AreEqual("-", promotedLine.Prefix);
            Assert.AreEqual(1, promotedLine.Depth);
        }

        [TestMethod]
        public void Promote_Default_OnePrefix_EmDash()
        {
            VoteLine vote = new VoteLine("—", "X", "", "A basic vote line", MarkerType.Vote, 100);
            var promotedLine = vote.GetPromotedLine();

            Assert.AreEqual("", promotedLine.Prefix);
            Assert.AreEqual(0, promotedLine.Depth);
        }

        [TestMethod]
        public void Promote_Default_SomePrefix_EmDash()
        {
            VoteLine vote = new VoteLine("——", "X", "", "A basic vote line", MarkerType.Vote, 100);
            var promotedLine = vote.GetPromotedLine();

            Assert.AreEqual("—", promotedLine.Prefix);
            Assert.AreEqual(1, promotedLine.Depth);
        }

        /*************************************/

        [TestMethod]
        public void Promote_Double_NoPrefix()
        {
            VoteLine vote = new VoteLine("", "X", "", "A basic vote line", MarkerType.Vote, 100);
            var promotedLine = vote.GetPromotedLine(2);

            Assert.AreEqual("", promotedLine.Prefix);
            Assert.AreEqual(0, promotedLine.Depth);
        }

        [TestMethod]
        public void Promote_Double_OnePrefix_Dash()
        {
            VoteLine vote = new VoteLine("-", "X", "", "A basic vote line", MarkerType.Vote, 100);
            var promotedLine = vote.GetPromotedLine(2);

            Assert.AreEqual("", promotedLine.Prefix);
            Assert.AreEqual(0, promotedLine.Depth);
        }

        [TestMethod]
        public void Promote_Double_SomePrefix_Dash()
        {
            VoteLine vote = new VoteLine("--", "X", "", "A basic vote line", MarkerType.Vote, 100);
            var promotedLine = vote.GetPromotedLine(2);

            Assert.AreEqual("", promotedLine.Prefix);
            Assert.AreEqual(0, promotedLine.Depth);
        }

        [TestMethod]
        public void Promote_Double_OnePrefix_EmDash()
        {
            VoteLine vote = new VoteLine("—", "X", "", "A basic vote line", MarkerType.Vote, 100);
            var promotedLine = vote.GetPromotedLine(2);

            Assert.AreEqual("", promotedLine.Prefix);
            Assert.AreEqual(0, promotedLine.Depth);
        }

        [TestMethod]
        public void Promote_Double_SomePrefix_EmDash()
        {
            VoteLine vote = new VoteLine("——", "X", "", "A basic vote line", MarkerType.Vote, 100);
            var promotedLine = vote.GetPromotedLine(2);

            Assert.AreEqual("", promotedLine.Prefix);
            Assert.AreEqual(0, promotedLine.Depth);
        }

        /*************************************/

        [TestMethod]
        public void Promote_Zero_NoPrefix()
        {
            VoteLine vote = new VoteLine("", "X", "", "A basic vote line", MarkerType.Vote, 100);
            var promotedLine = vote.GetPromotedLine(0);

            Assert.AreEqual("", promotedLine.Prefix);
            Assert.AreEqual(0, promotedLine.Depth);
        }

        [TestMethod]
        public void Promote_Zero_OnePrefix_Dash()
        {
            VoteLine vote = new VoteLine("-", "X", "", "A basic vote line", MarkerType.Vote, 100);
            var promotedLine = vote.GetPromotedLine(0);

            Assert.AreEqual("-", promotedLine.Prefix);
            Assert.AreEqual(1, promotedLine.Depth);
        }

        [TestMethod]
        public void Promote_Zero_SomePrefix_Dash()
        {
            VoteLine vote = new VoteLine("--", "X", "", "A basic vote line", MarkerType.Vote, 100);
            var promotedLine = vote.GetPromotedLine(0);

            Assert.AreEqual("--", promotedLine.Prefix);
            Assert.AreEqual(2, promotedLine.Depth);
        }

        [TestMethod]
        public void Promote_Zero_OnePrefix_EmDash()
        {
            VoteLine vote = new VoteLine("—", "X", "", "A basic vote line", MarkerType.Vote, 100);
            var promotedLine = vote.GetPromotedLine(0);

            Assert.AreEqual("—", promotedLine.Prefix);
            Assert.AreEqual(1, promotedLine.Depth);
        }

        [TestMethod]
        public void Promote_Zero_SomePrefix_EmDash()
        {
            VoteLine vote = new VoteLine("——", "X", "", "A basic vote line", MarkerType.Vote, 100);
            var promotedLine = vote.GetPromotedLine(0);

            Assert.AreEqual("——", promotedLine.Prefix);
            Assert.AreEqual(2, promotedLine.Depth);
        }

        /*****************************************/

        [TestMethod]
        public void Promote_Negative_NoPrefix()
        {
            VoteLine vote = new VoteLine("", "X", "", "A basic vote line", MarkerType.Vote, 100);
            var promotedLine = vote.GetPromotedLine(-5);

            Assert.AreEqual("", promotedLine.Prefix);
            Assert.AreEqual(0, promotedLine.Depth);
        }

        [TestMethod]
        public void Promote_Negative_OnePrefix_Dash()
        {
            VoteLine vote = new VoteLine("-", "X", "", "A basic vote line", MarkerType.Vote, 100);
            var promotedLine = vote.GetPromotedLine(-5);

            Assert.AreEqual("", promotedLine.Prefix);
            Assert.AreEqual(0, promotedLine.Depth);
        }

        [TestMethod]
        public void Promote_Negative_SomePrefix_Dash()
        {
            VoteLine vote = new VoteLine("--", "X", "", "A basic vote line", MarkerType.Vote, 100);
            var promotedLine = vote.GetPromotedLine(-5);

            Assert.AreEqual("-", promotedLine.Prefix);
            Assert.AreEqual(1, promotedLine.Depth);
        }

        [TestMethod]
        public void Promote_Negative_OnePrefix_EmDash()
        {
            VoteLine vote = new VoteLine("—", "X", "", "A basic vote line", MarkerType.Vote, 100);
            var promotedLine = vote.GetPromotedLine(-5);

            Assert.AreEqual("", promotedLine.Prefix);
            Assert.AreEqual(0, promotedLine.Depth);
        }

        [TestMethod]
        public void Promote_Negative_SomePrefix_EmDash()
        {
            VoteLine vote = new VoteLine("——", "X", "", "A basic vote line", MarkerType.Vote, 100);
            var promotedLine = vote.GetPromotedLine(-5);

            Assert.AreEqual("—", promotedLine.Prefix);
            Assert.AreEqual(1, promotedLine.Depth);
        }
        #endregion

        #region Strings
        [TestMethod]
        public void ToString_Basic()
        {
            VoteLine vote = new VoteLine("", "X", "", "A basic vote line", MarkerType.Vote, 100);

            Assert.AreEqual("[X] A basic vote line", vote.ToString());
        }

        [TestMethod]
        public void ToString_Basic_Prefix()
        {
            VoteLine vote = new VoteLine("-", "X", "", "A basic vote line", MarkerType.Vote, 100);

            Assert.AreEqual("-[X] A basic vote line", vote.ToString());
        }

        [TestMethod]
        public void ToString_Basic_Marker()
        {
            VoteLine vote = new VoteLine("", "x", "", "A basic vote line", MarkerType.Vote, 100);

            Assert.AreEqual("[x] A basic vote line", vote.ToString());
        }

        [TestMethod]
        public void ToString_Rank()
        {
            VoteLine vote = new VoteLine("", "#2", "", "A basic vote line", MarkerType.Rank, 2);

            Assert.AreEqual("[#2] A basic vote line", vote.ToString());
        }

        [TestMethod]
        public void ToString_Score()
        {
            VoteLine vote = new VoteLine("", "77%", "", "A basic vote line", MarkerType.Score, 77);

            Assert.AreEqual("[77%] A basic vote line", vote.ToString());
        }

        [TestMethod]
        public void ToString_Box()
        {
            VoteLine vote = new VoteLine("", "☒", "", "A basic vote line", MarkerType.Vote, 100);

            Assert.AreEqual("☒ A basic vote line", vote.ToString());
        }

        [TestMethod]
        public void ToString_Basic_Task()
        {
            VoteLine vote = new VoteLine("", "X", "Orb", "A basic vote line", MarkerType.Vote, 100);

            Assert.AreEqual("[X][Orb] A basic vote line", vote.ToString());
        }

        [TestMethod]
        public void ToString_Basic_Prefix_Task()
        {
            VoteLine vote = new VoteLine("-", "X", "Orbus?", "A basic vote line", MarkerType.Vote, 100);

            Assert.AreEqual("-[X][Orbus?] A basic vote line", vote.ToString());
        }

        [TestMethod]
        public void ToString_Basic_Marker_Task()
        {
            VoteLine vote = new VoteLine("", "x", "Orb-us", "A basic vote line", MarkerType.Vote, 100);

            Assert.AreEqual("[x][Orb-us] A basic vote line", vote.ToString());
        }

        [TestMethod]
        public void ToString_Rank_Task()
        {
            VoteLine vote = new VoteLine("", "#2", "Color", "A basic vote line", MarkerType.Rank, 2);

            Assert.AreEqual("[#2][Color] A basic vote line", vote.ToString());
        }

        [TestMethod]
        public void ToString_Score_Task()
        {
            VoteLine vote = new VoteLine("", "77%", "Cape", "A basic vote line", MarkerType.Score, 77);

            Assert.AreEqual("[77%][Cape] A basic vote line", vote.ToString());
        }

        [TestMethod]
        public void ToString_Box_Task()
        {
            VoteLine vote = new VoteLine("", "☒", "Hat", "A basic vote line", MarkerType.Vote, 100);

            Assert.AreEqual("☒[Hat] A basic vote line", vote.ToString());
        }

        /*******************************/

        [TestMethod]
        public void ToComparableString_Basic()
        {
            VoteLine vote = new VoteLine("", "X", "", "A basic vote line", MarkerType.Vote, 100);

            Assert.AreEqual("[] A basic vote line", vote.ToComparableString());
        }

        [TestMethod]
        public void ToComparableString_Basic_Prefix()
        {
            VoteLine vote = new VoteLine("-", "X", "", "A basic vote line", MarkerType.Vote, 100);

            Assert.AreEqual("-[] A basic vote line", vote.ToComparableString());
        }

        [TestMethod]
        public void ToComparableString_Basic_Marker()
        {
            VoteLine vote = new VoteLine("", "x", "", "A basic vote line", MarkerType.Vote, 100);

            Assert.AreEqual("[] A basic vote line", vote.ToComparableString());
        }

        [TestMethod]
        public void ToComparableString_Rank()
        {
            VoteLine vote = new VoteLine("", "#2", "", "A basic vote line", MarkerType.Rank, 2);

            Assert.AreEqual("[] A basic vote line", vote.ToComparableString());
        }

        [TestMethod]
        public void ToComparableString_Score()
        {
            VoteLine vote = new VoteLine("", "77%", "", "A basic vote line", MarkerType.Score, 77);

            Assert.AreEqual("[] A basic vote line", vote.ToComparableString());
        }

        [TestMethod]
        public void ToComparableString_Box()
        {
            VoteLine vote = new VoteLine("-", "☒", "", "A basic vote line", MarkerType.Vote, 100);

            Assert.AreEqual("-[] A basic vote line", vote.ToComparableString());
        }

        [TestMethod]
        public void ToComparableString_Basic_Task()
        {
            VoteLine vote = new VoteLine("", "X", "Orb", "A basic vote line", MarkerType.Vote, 100);

            Assert.AreEqual("[][Orb] A basic vote line", vote.ToComparableString());
        }

        [TestMethod]
        public void ToComparableString_Basic_Prefix_Task()
        {
            VoteLine vote = new VoteLine("-", "X", "Orbus?", "A basic vote line", MarkerType.Vote, 100);

            Assert.AreEqual("-[][Orbus?] A basic vote line", vote.ToComparableString());
        }

        [TestMethod]
        public void ToComparableString_Basic_Marker_Task()
        {
            VoteLine vote = new VoteLine("", "x", "Orb-us", "A basic vote line", MarkerType.Vote, 100);

            Assert.AreEqual("[][Orb-us] A basic vote line", vote.ToComparableString());
        }

        [TestMethod]
        public void ToComparableString_Rank_Task()
        {
            VoteLine vote = new VoteLine("", "#2", "Color", "A basic vote line", MarkerType.Rank, 2);

            Assert.AreEqual("[][Color] A basic vote line", vote.ToComparableString());
        }

        [TestMethod]
        public void ToComparableString_Score_Task()
        {
            VoteLine vote = new VoteLine("", "77%", "Cape", "A basic vote line", MarkerType.Score, 77);

            Assert.AreEqual("[][Cape] A basic vote line", vote.ToComparableString());
        }

        [TestMethod]
        public void ToComparableString_Box_Task()
        {
            VoteLine vote = new VoteLine("", "☒", "Hat", "A basic vote line", MarkerType.Vote, 100);

            Assert.AreEqual("[][Hat] A basic vote line", vote.ToComparableString());
        }
        #endregion
    }
}
