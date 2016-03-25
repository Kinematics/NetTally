using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Text.RegularExpressions;
using NetTally.Utility;

namespace NetTally.Tests
{
    [TestClass]
    public class VoteStringTests
    {
        #region RemoveBBCode
        [TestMethod]
        public void RemoveBBCode_Empty()
        {
            string line = "";
            string cleanLine = "";

            Assert.AreEqual(cleanLine, VoteString.RemoveBBCode(line));
        }

        [TestMethod]
        public void RemoveBBCode_NoBBCode()
        {
            string line = "[x] Vote for stuff";
            string cleanLine = "[x] Vote for stuff";

            Assert.AreEqual(cleanLine, VoteString.RemoveBBCode(line));
        }

        [TestMethod]
        public void RemoveBBCode_FullLineBold()
        {
            string line = "『b』[x] Vote for stuff『/b』";
            string cleanLine = "[x] Vote for stuff";

            Assert.AreEqual(cleanLine, VoteString.RemoveBBCode(line));
        }

        [TestMethod]
        public void RemoveBBCode_FullLineColor()
        {
            string line = "『color=blue』[x] Vote for stuff『/color』";
            string cleanLine = "[x] Vote for stuff";

            Assert.AreEqual(cleanLine, VoteString.RemoveBBCode(line));

            line = "『color=#15fae6』[x] Vote for stuff『/color』";
            cleanLine = "[x] Vote for stuff";

            Assert.AreEqual(cleanLine, VoteString.RemoveBBCode(line));
        }

        [TestMethod]
        public void RemoveBBCode_PartialLineBold()
        {
            string line = "『b』[x] Vote for stuff";
            string cleanLine = "[x] Vote for stuff";

            Assert.AreEqual(cleanLine, VoteString.RemoveBBCode(line));

            line = "[x] Vote for stuff『/b』";
            cleanLine = "[x] Vote for stuff";

            Assert.AreEqual(cleanLine, VoteString.RemoveBBCode(line));
        }

        [TestMethod]
        public void RemoveBBCode_MultiCodes()
        {
            string line = "『color=blue』『b』[x] 『i』Vote『/i』 for stuff『/b』";
            string cleanLine = "[x] Vote for stuff";

            Assert.AreEqual(cleanLine, VoteString.RemoveBBCode(line));
        }
        #endregion

        #region DeUrl
        [TestMethod]
        public void DeUrl_Empty()
        {
            string content = "";
            string clean = "";

            Assert.AreEqual(clean, VoteString.DeUrlContent(content));
        }

        [TestMethod]
        public void DeUrl_None()
        {
            string content = "Vote for stuff";
            string clean = "Vote for stuff";

            Assert.AreEqual(clean, VoteString.DeUrlContent(content));
        }

        [TestMethod]
        public void DeUrl_NoneWithBBCode()
        {
            string content = "『i』Vote『/i』 for stuff";
            string clean = "『i』Vote『/i』 for stuff";

            Assert.AreEqual(clean, VoteString.DeUrlContent(content));
        }

        [TestMethod]
        public void DeUrl_Url()
        {
            string content = "[url=https://forum.questionablequesting.com/members/2392/]Xryuran[/url]";
            string clean = "Xryuran";

            Assert.AreEqual(clean, VoteString.DeUrlContent(content));
        }

        [TestMethod]
        public void DeUrl_AtUrl()
        {
            string content = "[url=https://forum.questionablequesting.com/members/2392/]@Xryuran[/url]";
            string clean = "Xryuran";

            Assert.AreEqual(clean, VoteString.DeUrlContent(content));
        }

        [TestMethod]
        public void DeUrl_Image()
        {
            string content = "[url=http://google.com/image/1.jpg]<Image>[/url]";
            string clean = "<Image>";

            Assert.AreEqual(clean, VoteString.DeUrlContent(content));
        }

        [TestMethod]
        public void DeUrl_Link()
        {
            string content = "Vote for [url=http://google.com/myhome.html]me[/url]!";
            string clean = "Vote for me!";

            Assert.AreEqual(clean, VoteString.DeUrlContent(content));
        }
        #endregion

        #region CleanVoteLine
        [TestMethod]
        public void CleanVoteLine_Empty()
        {
            string content = "";
            string clean = "";

            Assert.AreEqual(clean, VoteString.DeUrlContent(content));
        }

        [TestMethod]
        public void CleanVoteLine_NoBBCode()
        {
            string line = "[x] Vote for stuff";
            string cleanLine = "[x] Vote for stuff";

            Assert.AreEqual(cleanLine, VoteString.CleanVoteLineBBCode(line));
        }

        [TestMethod]
        public void CleanVoteLine_FullLineBold()
        {
            string line = "『b』[x] Vote for stuff『/b』";
            string cleanLine = "[x] Vote for stuff";

            Assert.AreEqual(cleanLine, VoteString.CleanVoteLineBBCode(line));
        }

        [TestMethod]
        public void CleanVoteLine_FullLineBoldPre()
        {
            string line = "-『b』[x] Vote for stuff『/b』";
            string cleanLine = "-[x] Vote for stuff";

            Assert.AreEqual(cleanLine, VoteString.CleanVoteLineBBCode(line));
        }

        [TestMethod]
        public void CleanVoteLine_FullLineBoldPreTask()
        {
            string line = "-[x]『b』[Who] Vote for stuff『/b』";
            string cleanLine = "-[x][Who] Vote for stuff";

            Assert.AreEqual(cleanLine, VoteString.CleanVoteLineBBCode(line));
        }

        [TestMethod]
        public void CleanVoteLine_FullLineColor()
        {
            string line = "『color=blue』[x] Vote for stuff『/color』";
            string cleanLine = "[x] Vote for stuff";

            Assert.AreEqual(cleanLine, VoteString.CleanVoteLineBBCode(line));

            line = "『color=#15fae6』[x] Vote for stuff『/color』";
            cleanLine = "[x] Vote for stuff";

            Assert.AreEqual(cleanLine, VoteString.CleanVoteLineBBCode(line));
        }

        [TestMethod]
        public void CleanVoteLine_Color()
        {
            string line = "[x] 『color=blue』Vote for stuff『/color』";
            string cleanLine = "[x] 『color=blue』Vote for stuff『/color』";

            Assert.AreEqual(cleanLine, VoteString.CleanVoteLineBBCode(line));

            line = "[x] 『color=#15fae6』Vote for stuff『/color』";
            cleanLine = "[x] 『color=#15fae6』Vote for stuff『/color』";

            Assert.AreEqual(cleanLine, VoteString.CleanVoteLineBBCode(line));
        }

        [TestMethod]
        public void CleanVoteLine_PartialBold()
        {
            string line = "『b』[x] Vote for stuff";
            string cleanLine = "[x] Vote for stuff";

            Assert.AreEqual(cleanLine, VoteString.CleanVoteLineBBCode(line));

            line = "[x] Vote for stuff『/b』";
            cleanLine = "[x] Vote for stuff";

            Assert.AreEqual(cleanLine, VoteString.CleanVoteLineBBCode(line));
        }

        [TestMethod]
        public void CleanVoteLine_ItalicsInContent()
        {
            string line = "[x] 『i』Vote『/i』 for stuff";
            string cleanLine = "[x] 『i』Vote『/i』 for stuff";

            Assert.AreEqual(cleanLine, VoteString.CleanVoteLineBBCode(line));
        }

        [TestMethod]
        public void CleanVoteLine_MultiCodes()
        {
            string line = "[x] 『i』Vote『/i』 for 『b』stuff『/b』";
            string cleanLine = "[x] 『i』Vote『/i』 for 『b』stuff『/b』";

            Assert.AreEqual(cleanLine, VoteString.CleanVoteLineBBCode(line));
        }

        [TestMethod]
        public void CleanVoteLine_NestedCodes()
        {
            string line = "[x] 『b』『i』Vote『/i』 for stuff『/b』";
            string cleanLine = "[x] 『b』『i』Vote『/i』 for stuff『/b』";

            Assert.AreEqual(cleanLine, VoteString.CleanVoteLineBBCode(line));
        }

        [TestMethod]
        public void CleanVoteLine_TaskMarkup()
        {
            string line = "-[x][『b』Who『/b』] Vote for stuff";
            string cleanLine = "-[x][Who] Vote for stuff";

            Assert.AreEqual(cleanLine, VoteString.CleanVoteLineBBCode(line));
        }

        [TestMethod]
        public void CleanVoteLine_NestedTaskMarkup()
        {
            string line = "-[x][『color=#15fae6』『b』Who『/b』『/color』] Vote for stuff";
            string cleanLine = "-[x][Who] Vote for stuff";

            Assert.AreEqual(cleanLine, VoteString.CleanVoteLineBBCode(line));
        }

        [TestMethod]
        public void CleanVoteLine_InMarker()
        {
            string line = "-[『b』x][Who『/b』] Vote for stuff";
            string cleanLine = "-[x][Who] Vote for stuff";

            Assert.AreEqual(cleanLine, VoteString.CleanVoteLineBBCode(line));

            line = "-『b』[『b』x][Who『/b』] Vote for stuff『/b』";
            cleanLine = "-[x][Who] Vote for stuff";

            Assert.AreEqual(cleanLine, VoteString.CleanVoteLineBBCode(line));
        }

        [TestMethod]
        public void CleanVoteLine_InMarkerPlus()
        {
            string line = "-『b』[『b』x][Who『/b』] 『b』Vote『/b』 for stuff『/b』";
            string cleanLine = "-[x][Who] 『b』Vote『/b』 for stuff";

            Assert.AreEqual(cleanLine, VoteString.CleanVoteLineBBCode(line));
        }

        [TestMethod]
        public void CleanVoteLine_ColorMarker()
        {
            string line = "『color=blue』[x]『/color』[main] Vote for stuff";
            string cleanLine = "[x][main] Vote for stuff";

            Assert.AreEqual(cleanLine, VoteString.CleanVoteLineBBCode(line));
        }

        [TestMethod]
        public void CleanVoteLine_ColorMarkerPlus()
        {
            string line = "『color=blue』[x]『/color』[main] 『b』『b』Vote『/b』 for stuff";
            string cleanLine = "[x][main] 『b』Vote『/b』 for stuff";

            Assert.AreEqual(cleanLine, VoteString.CleanVoteLineBBCode(line));
        }

        [TestMethod]
        public void CleanVoteLine_LongSamplePartialColors()
        {
            string line = "[X] - Brutalize them. You haven’t had a chance to properly fight in 『/color』『i』『color=#ebebeb』years『/color』『/i』『color=#ebebeb』, and spars can only do so much. How thoughtful of the Herans to volunteer!";
            string cleanLine = "[X] - Brutalize them. You haven’t had a chance to properly fight in 『i』『color=#ebebeb』years『/color』『/i』, and spars can only do so much. How thoughtful of the Herans to volunteer!";

            Assert.AreEqual(cleanLine, VoteString.CleanVoteLineBBCode(line));
        }
        #endregion


        [TestMethod]
        public void GetVotePrefixTest()
        {
            string line1 = "[x] Vote for stuff";
            string line2 = "-[x] Vote for stuff";
            string line3 = "---[x] Vote for stuff";
            string line4 = "- [x] Vote for stuff";
            string line5 = "- - -[x] Vote for stuff";

            Assert.AreEqual("", VoteString.GetVotePrefix(line1));
            Assert.AreEqual("-", VoteString.GetVotePrefix(line2));
            Assert.AreEqual("---", VoteString.GetVotePrefix(line3));
            Assert.AreEqual("-", VoteString.GetVotePrefix(line4));
            Assert.AreEqual("---", VoteString.GetVotePrefix(line5));
        }

        [TestMethod]
        public void GetVoteMarkerTest()
        {
            string line1 = "[x] Vote for stuff";
            string line2 = "-[X] Vote for stuff";
            string line3 = "[✓][major] Vote for stuff";
            string line4 = "-[ ✔][ animal] Vote for stuff";

            Assert.AreEqual("x", VoteString.GetVoteMarker(line1));
            Assert.AreEqual("X", VoteString.GetVoteMarker(line2));
            Assert.AreEqual("✓", VoteString.GetVoteMarker(line3));
            Assert.AreEqual("✔", VoteString.GetVoteMarker(line4));

            // + is no longer valid
            line1 = "-[+] Vote for stuff";
            Assert.AreEqual("", VoteString.GetVoteMarker(line1));
            line1 = "[a] Vote for stuff";
            Assert.AreEqual("", VoteString.GetVoteMarker(line1));
            line1 = "[k] Vote for stuff";
            Assert.AreEqual("", VoteString.GetVoteMarker(line1));
            line1 = "[jk] Vote for stuff";
            Assert.AreEqual("", VoteString.GetVoteMarker(line1));
            line1 = "[xx] Vote for stuff";
            Assert.AreEqual("", VoteString.GetVoteMarker(line1));
            line1 = "[O] Vote for stuff";
            Assert.AreEqual("", VoteString.GetVoteMarker(line1));
            line1 = "[o] Vote for stuff";
            Assert.AreEqual("", VoteString.GetVoteMarker(line1));
            line1 = "[-] Vote for stuff";
            Assert.AreEqual("", VoteString.GetVoteMarker(line1));
            line1 = "[=] Vote for stuff";
            Assert.AreEqual("", VoteString.GetVoteMarker(line1));
            line1 = "[@] Vote for stuff";
            Assert.AreEqual("", VoteString.GetVoteMarker(line1));
            line1 = "[q] Vote for stuff";
            Assert.AreEqual("", VoteString.GetVoteMarker(line1));
        }

        [TestMethod]
        public void GetVoteTaskTest()
        {
            string line1 = "[x] Vote for stuff";
            string line2 = "-[x] Vote for stuff";
            string line3 = "[x][major] Vote for stuff";
            string line4 = "-[x][ animal] Vote for stuff";
            string line5 = "[x][MINOR] Vote for stuff";
            string line6 = "[x][Trade Relations] Vote for stuff";
            string line7 = "[x] 『url=http://google.com』<image>『/url』";
            string line8 = "[x] 『b』Vote for stuff『/b』";


            Assert.AreEqual("", VoteString.GetVoteTask(line1));
            Assert.AreEqual("", VoteString.GetVoteTask(line2));
            Assert.AreEqual("major", VoteString.GetVoteTask(line3));
            Assert.AreEqual("animal", VoteString.GetVoteTask(line4));
            Assert.AreEqual("MINOR", VoteString.GetVoteTask(line5));
            Assert.AreEqual("Trade Relations", VoteString.GetVoteTask(line6));
            Assert.AreEqual("", VoteString.GetVoteTask(line7));
            Assert.AreEqual("", VoteString.GetVoteTask(line8));
        }

        [TestMethod]
        public void GetCondensedVoteTaskTest()
        {
            string line1 = "[x] Vote for stuff";
            string line2 = "[] Vote for stuff";
            string line3 = "[major] Vote for stuff";
            string line4 = "[ animal] Vote for stuff";
            string line5 = "[MINOR] Vote for stuff";
            string line6 = "[Trade Relations] Vote for stuff";
            string line7 = "[] [url=http://google.com]<image>[/url]";

            Assert.AreEqual("x", VoteString.GetVoteTask(line1, VoteType.Rank));
            Assert.AreEqual("", VoteString.GetVoteTask(line2, VoteType.Rank));
            Assert.AreEqual("major", VoteString.GetVoteTask(line3, VoteType.Rank));
            Assert.AreEqual("animal", VoteString.GetVoteTask(line4, VoteType.Rank));
            Assert.AreEqual("MINOR", VoteString.GetVoteTask(line5, VoteType.Rank));
            Assert.AreEqual("Trade Relations", VoteString.GetVoteTask(line6, VoteType.Rank));
            Assert.AreEqual("", VoteString.GetVoteTask(line7, VoteType.Rank));
        }

        [TestMethod]
        public void GetVoteContentTest()
        {
            string input = "[X] We 『i』did『/i』 agree to non-lethal. My most 『color=blue』powerful『/color』 stuff either knocks people out or kills them without having to fight at all. Everything else I've learned to do so far feels like a witch barrier, and I try not to use that since it freaks everyone out.";
            string expected = "We 『i』did『/i』 agree to non-lethal. My most 『color=blue』powerful『/color』 stuff either knocks people out or kills them without having to fight at all. Everything else I've learned to do so far feels like a witch barrier, and I try not to use that since it freaks everyone out.";

            Assert.AreEqual(expected, VoteString.GetVoteContent(input));

            input = "[x] Vote for stuff";
            expected = "Vote for stuff";
            Assert.AreEqual(expected, VoteString.GetVoteContent(input));

            input = "[x][major] Vote for stuff";
            expected = "Vote for stuff";
            Assert.AreEqual(expected, VoteString.GetVoteContent(input));

            input = "-[x][ animal] Vote for stuff";
            expected = "Vote for stuff";
            Assert.AreEqual(expected, VoteString.GetVoteContent(input));

            // Invalid line.  Leading BBCode should have already been removed.
            input = "『color=blue』-[x] Vote for stuff『/color』";
            expected = "";
            Assert.AreEqual(expected, VoteString.GetVoteContent(input));

        }

        [TestMethod]
        public void GetCondensedVoteContentTest()
        {
            string line1 = "[x] Vote for stuff";
            string line2 = "[] Vote for stuff";
            string line3 = "[major] Vote for stuff";
            string line4 = "[ animal] Vote for stuff";
            string line5 = "[MINOR] Vote for stuff";
            string line6 = "[Trade Relations] Vote for stuff";
            string line7 = "[] [url=http://google.com]<image>[/url]";

            Assert.AreEqual("Vote for stuff", VoteString.GetVoteContent(line1, VoteType.Rank));
            Assert.AreEqual("Vote for stuff", VoteString.GetVoteContent(line2, VoteType.Rank));
            Assert.AreEqual("Vote for stuff", VoteString.GetVoteContent(line3, VoteType.Rank));
            Assert.AreEqual("Vote for stuff", VoteString.GetVoteContent(line4, VoteType.Rank));
            Assert.AreEqual("Vote for stuff", VoteString.GetVoteContent(line5, VoteType.Rank));
            Assert.AreEqual("Vote for stuff", VoteString.GetVoteContent(line6, VoteType.Rank));
            Assert.AreEqual("[url=http://google.com]<image>[/url]", VoteString.GetVoteContent(line7, VoteType.Rank));
        }


        [TestMethod]
        public void GetVotePlanNameTest1()
        {
            string input = "[x] Kinematics";
            string expected1 = "Kinematics";
            string expected2 = "\u25C8Kinematics";
            var result = VoteString.GetVoteReferenceNames(input);
            Assert.AreEqual(2, result[ReferenceType.Any].Count);
            Assert.IsTrue(result[ReferenceType.Any].Contains(expected1));
            Assert.IsTrue(result[ReferenceType.Any].Contains(expected2));
        }

        [TestMethod]
        public void GetVotePlanNameTest2()
        {
            string input = "[x] Plan Assault";
            string expected1 = "Assault";
            string expected2 = "\u25C8Assault";
            var result = VoteString.GetVoteReferenceNames(input);
            Assert.AreEqual(2, result[ReferenceType.Any].Count);
            Assert.IsTrue(result[ReferenceType.Any].Contains(expected1));
            Assert.IsTrue(result[ReferenceType.Any].Contains(expected2));
        }

        [TestMethod]
        public void GetVotePlanNameTest3()
        {
            string input = "[x] Kinematics.";
            string expected1 = "Kinematics.";
            string expected2 = "\u25C8Kinematics.";
            string expected3 = "Kinematics";
            string expected4 = "\u25C8Kinematics";
            var result = VoteString.GetVoteReferenceNames(input);
            Assert.AreEqual(2, result[ReferenceType.Any].Count);
            Assert.IsTrue(result[ReferenceType.Any].Contains(expected1));
            Assert.IsTrue(result[ReferenceType.Any].Contains(expected2));
            Assert.IsFalse(result[ReferenceType.Any].Contains(expected3));
            Assert.IsFalse(result[ReferenceType.Any].Contains(expected4));
        }

        [TestMethod]
        public void GetVotePlanNameTest4()
        {
            string input = "[x] Plan Assault.";
            string expected1 = "Assault.";
            string expected2 = "\u25C8Assault.";
            string expected3 = "Assault";
            string expected4 = "\u25C8Assault";
            var result = VoteString.GetVoteReferenceNames(input);
            Assert.AreEqual(2, result[ReferenceType.Any].Count);
            Assert.IsTrue(result[ReferenceType.Any].Contains(expected1));
            Assert.IsTrue(result[ReferenceType.Any].Contains(expected2));
            Assert.IsFalse(result[ReferenceType.Any].Contains(expected3));
            Assert.IsFalse(result[ReferenceType.Any].Contains(expected4));
            
        }

        [TestMethod]
        public void GetVotePlanNameTest5()
        {
            string input = "[x] Plan [url=https://forum.questionablequesting.com/members/2392/]Xryuran[/url]";
            string expected1 = "Xryuran";
            string expected2 = "\u25C8Xryuran";
            var result = VoteString.GetVoteReferenceNames(input);
            Assert.AreEqual(2, result[ReferenceType.Any].Count);
            Assert.IsTrue(result[ReferenceType.Any].Contains(expected1));
            Assert.IsTrue(result[ReferenceType.Any].Contains(expected2));
        }

        [TestMethod]
        public void GetVotePlanNameTest6()
        {
            string input = "[x] [url=https://forum.questionablequesting.com/members/2392/]Xryuran[/url].";
            string expected1 = "Xryuran.";
            string expected2 = "\u25C8Xryuran.";
            string expected3 = "Xryuran";
            string expected4 = "\u25C8Xryuran";
            var result = VoteString.GetVoteReferenceNames(input);
            Assert.AreEqual(2, result[ReferenceType.Any].Count);
            Assert.IsTrue(result[ReferenceType.Any].Contains(expected1));
            Assert.IsTrue(result[ReferenceType.Any].Contains(expected2));
            Assert.IsFalse(result[ReferenceType.Any].Contains(expected3));
            Assert.IsFalse(result[ReferenceType.Any].Contains(expected4));
        }

        [TestMethod]
        public void GetVotePlanNameTest7()
        {
            string input = "[x] [url=https://forum.questionablequesting.com/members/2392/]@Xryuran[/url]";
            string expected1 = "Xryuran";
            string expected2 = "\u25C8Xryuran";
            var result = VoteString.GetVoteReferenceNames(input);
            Assert.AreEqual(2, result[ReferenceType.Any].Count);
            Assert.IsTrue(result[ReferenceType.Any].Contains(expected1));
            Assert.IsTrue(result[ReferenceType.Any].Contains(expected2));
        }

        [TestMethod]
        public void GetVoteComponentsTest1()
        {
            string input = "[x] Vote for stuff";
            string prefix;
            string marker;
            string task;
            string content;

            VoteString.GetVoteComponents(input, out prefix, out marker, out task, out content);

            Assert.AreEqual("", prefix);
            Assert.AreEqual("x", marker);
            Assert.AreEqual("", task);
            Assert.AreEqual("Vote for stuff", content);
        }

        [TestMethod]
        public void GetVoteComponentsTest2()
        {
            string input = "-[x][MAJOR] Vote for stuff";
            string prefix;
            string marker;
            string task;
            string content;

            VoteString.GetVoteComponents(input, out prefix, out marker, out task, out content);

            Assert.AreEqual("-", prefix);
            Assert.AreEqual("x", marker);
            Assert.AreEqual("MAJOR", task);
            Assert.AreEqual("Vote for stuff", content);
        }

        [TestMethod]
        public void GetVoteComponentsTest3()
        {
            string input = "- [ x ][MAJOR] Vote for stuff";
            string prefix;
            string marker;
            string task;
            string content;

            VoteString.GetVoteComponents(input, out prefix, out marker, out task, out content);

            Assert.AreEqual("-", prefix);
            Assert.AreEqual("x", marker);
            Assert.AreEqual("MAJOR", task);
            Assert.AreEqual("Vote for stuff", content);
        }

        [TestMethod]
        [ExpectedException(typeof(InvalidOperationException))]
        public void GetVoteComponentsTest4()
        {
            string input = "『color=blue』-[x][MAJOR] Vote for stuff『/color』";
            string prefix;
            string marker;
            string task;
            string content;

            VoteString.GetVoteComponents(input, out prefix, out marker, out task, out content);

            Assert.AreEqual("-", prefix);
            Assert.AreEqual("x", marker);
            Assert.AreEqual("MAJOR", task);
            Assert.AreEqual("『color=blue』Vote for stuff『/color』", content);
        }

        [TestMethod]
        [ExpectedException(typeof(InvalidOperationException))]
        public void GetVoteComponentsTest5()
        {
            string input = "-[『b』x][MAJOR] Vote for stuff『/b』";
            string prefix;
            string marker;
            string task;
            string content;

            VoteString.GetVoteComponents(input, out prefix, out marker, out task, out content);

            Assert.AreEqual("-", prefix);
            Assert.AreEqual("x", marker);
            Assert.AreEqual("MAJOR", task);
            Assert.AreEqual("『b』Vote for stuff『/b』", content);
        }


        [TestMethod]
        public void IsRankedVoteTest()
        {
            string test = "[1] Cat";
            Assert.IsTrue(VoteString.IsRankedVote(test));
            test = "[1][Animal] Cat";
            Assert.IsTrue(VoteString.IsRankedVote(test));
            test = "[2] Cat";
            Assert.IsTrue(VoteString.IsRankedVote(test));
            test = "[9] Cat";
            Assert.IsTrue(VoteString.IsRankedVote(test));
            test = "-[1] Cat";
            Assert.IsTrue(VoteString.IsRankedVote(test));
            test = "-- [1] Cat";
            Assert.IsTrue(VoteString.IsRankedVote(test));
            test = "-[1][Animal] Cat";
            Assert.IsTrue(VoteString.IsRankedVote(test));
            test = "-[1] [Animal] Cat";
            Assert.IsTrue(VoteString.IsRankedVote(test));
            test = "- [1] Cat";
            Assert.IsTrue(VoteString.IsRankedVote(test));
            test = "- [ 1] Cat";
            Assert.IsTrue(VoteString.IsRankedVote(test));

            test = "[x] Cat";
            Assert.IsFalse(VoteString.IsRankedVote(test));
            test = "-[x] Cat";
            Assert.IsFalse(VoteString.IsRankedVote(test));
            test = "- [x] Cat";
            Assert.IsFalse(VoteString.IsRankedVote(test));
            test = "- [X] Cat";
            Assert.IsFalse(VoteString.IsRankedVote(test));
            test = "- [+] Cat";
            Assert.IsFalse(VoteString.IsRankedVote(test));
            test = "[✓] Cat";
            Assert.IsFalse(VoteString.IsRankedVote(test));
            test = "[x][animal] Cat";
            Assert.IsFalse(VoteString.IsRankedVote(test));
            test = "-[x] [Animal] Cat";
            Assert.IsFalse(VoteString.IsRankedVote(test));
            test = "[10] Cat";
            Assert.IsFalse(VoteString.IsRankedVote(test));
            test = "[0] Cat";
            Assert.IsFalse(VoteString.IsRankedVote(test));
        }

        [TestMethod]
        public void CondenseVoteTest1()
        {
            string input = "[x] Vote for stuff";
            string expected = "[] Vote for stuff";

            Assert.AreEqual(expected, VoteString.CondenseVote(input));

            input = "[1] Vote for stuff";
            expected = "[] Vote for stuff";

            Assert.AreEqual(expected, VoteString.CondenseVote(input));

            input = "[1][Major] Vote for stuff";
            expected = "[Major] Vote for stuff";

            Assert.AreEqual(expected, VoteString.CondenseVote(input));

            input = "[1][Major] 『b』Vote for stuff『/b』";
            expected = "[Major] 『b』Vote for stuff『/b』";

            Assert.AreEqual(expected, VoteString.CondenseVote(input));

        }

        [TestMethod]
        [ExpectedException(typeof(InvalidOperationException))]
        public void CondenseVoteTest2()
        {
            // Shouldn't be able to generate this working string anymore:
            string input = "『b』[1] Vote for stuff『/b』";
            string expected = "[] ";

            Assert.AreEqual(expected, VoteString.CondenseVote(input));
        }

        [TestMethod]
        public void GetPlanNameTest1()
        {
            string input = "[X] Kinematics";

            Assert.AreEqual(null, VoteString.GetPlanName(input));
            Assert.AreEqual(null, VoteString.GetPlanName(input, true));
            Assert.AreEqual(null, VoteString.GetPlanName(input, false));
        }

        [TestMethod]
        public void GetPlanNameTest2()
        {
            string input = "[X] Plan Kinematics";

            Assert.AreEqual("Kinematics", VoteString.GetPlanName(input));
            Assert.AreEqual(null, VoteString.GetPlanName(input, true));
            Assert.AreEqual("Kinematics", VoteString.GetPlanName(input, false));
        }

        [TestMethod]
        public void GetPlanNameTest3()
        {
            string input = "[X] Base Plan Kinematics";

            Assert.AreEqual("Kinematics", VoteString.GetPlanName(input));
            Assert.AreEqual("Kinematics", VoteString.GetPlanName(input, true));
            Assert.AreEqual("Kinematics", VoteString.GetPlanName(input, false));
        }

        [TestMethod]
        public void GetPlanNameTest4()
        {
            string input = "[X] Base Plan : Kinematics";

            Assert.AreEqual("Kinematics", VoteString.GetPlanName(input));
            Assert.AreEqual("Kinematics", VoteString.GetPlanName(input, true));
            Assert.AreEqual("Kinematics", VoteString.GetPlanName(input, false));
        }

        [TestMethod]
        public void GetPlanNameTest5()
        {
            string input = "[X] Plan: Kinematics";

            Assert.AreEqual("Kinematics", VoteString.GetPlanName(input));
            Assert.AreEqual(null, VoteString.GetPlanName(input, true));
            Assert.AreEqual("Kinematics", VoteString.GetPlanName(input, false));
        }

        [TestMethod]
        public void GetPlanNameTest6()
        {
            string input = "[X] Plan: Kinematics";
            string expect = StringUtility.PlanNameMarker + "Kinematics";

            Assert.AreEqual(expect, VoteString.GetMarkedPlanName(input));
        }

        [TestMethod]
        public void GetPlanNameTest7()
        {
            string input = "[X] Kinematics";
            string expect = StringUtility.PlanNameMarker + "Kinematics";

            Assert.AreEqual(null, VoteString.GetMarkedPlanName(input));
        }


    }
}
