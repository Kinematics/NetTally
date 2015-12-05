using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using NetTally.Utility;

namespace NetTally.Tests
{
    [TestClass()]
    public class VoteStringTests
    {
        [TestMethod()]
        public void RemoveBBCodeTest()
        {
            string cleanLine1 = "[x] Vote for stuff";
            string cleanLine2 = "-[x] Vote for stuff";
            string cleanLine3 = "[x][main] Vote for stuff";
            string cleanLine4 = "-[x] Vote for “stuff”";

            string line1 = "[b][x] Vote for stuff[/b]";
            string line2 = "[color=blue][x] Vote for stuff[/color]";
            string line3 = "[b][x] Vote for stuff";
            string line4 = "[color=blue][b][x] Vote for stuff[/b]";
            string line5 = "[b]-[x] Vote for stuff";
            string line6 = "[color=blue]-[x] Vote for “stuff”[/color]";
            string line7 = "[color=blue][x][main] Vote for stuff[/color]";

            string e = "";

            Assert.AreEqual(e, VoteString.RemoveBBCode(e));

            Assert.AreEqual(cleanLine1, VoteString.RemoveBBCode(cleanLine1));
            Assert.AreEqual(cleanLine1, VoteString.RemoveBBCode(line1));
            Assert.AreEqual(cleanLine1, VoteString.RemoveBBCode(line2));
            Assert.AreEqual(cleanLine1, VoteString.RemoveBBCode(line3));
            Assert.AreEqual(cleanLine1, VoteString.RemoveBBCode(line4));

            Assert.AreEqual(cleanLine2, VoteString.RemoveBBCode(cleanLine2));
            Assert.AreEqual(cleanLine2, VoteString.RemoveBBCode(line5));
            Assert.AreEqual(cleanLine4, VoteString.RemoveBBCode(line6));

            Assert.AreEqual(cleanLine3, VoteString.RemoveBBCode(cleanLine3));
            Assert.AreEqual(cleanLine3, VoteString.RemoveBBCode(line7));
        }

        [TestMethod()]
        public void CleanVoteTest1()
        {
            string line1 = "[b][x] Vote for stuff[/b]";
            string line2 = "[color=blue][x] Vote for stuff[/color]";
            string line3 = "[b][x] Vote for stuff";
            string line4 = "[color=blue][b][x] Vote for stuff[/b]";
            string line5 = "[b]-[x] Vote for stuff";
            string line6 = "[color=blue]-[x] Vote for “stuff”[/color]";
            string line7 = "[color=blue][x][main] Vote for stuff[/color]";
            string line8 = "[x] [b]Vote for stuff";

            Assert.AreEqual("[x] Vote for stuff", VoteString.CleanVoteLineBBCode(line1));
            Assert.AreEqual("[x] Vote for stuff", VoteString.CleanVoteLineBBCode(line2));
            Assert.AreEqual("[x] Vote for stuff", VoteString.CleanVoteLineBBCode(line3));
            Assert.AreEqual("[x] Vote for stuff", VoteString.CleanVoteLineBBCode(line4));
            Assert.AreEqual("-[x] Vote for stuff", VoteString.CleanVoteLineBBCode(line5));
            Assert.AreEqual("-[x] Vote for “stuff”", VoteString.CleanVoteLineBBCode(line6));
            Assert.AreEqual("[x][main] Vote for stuff", VoteString.CleanVoteLineBBCode(line7));
            Assert.AreEqual("[x] [b]Vote for stuff[/b]", VoteString.CleanVoteLineBBCode(line8));
        }


        [TestMethod()]
        public void CleanVoteTest2()
        {
            string line1 = "[[b]x] Vote for stuff[/b]";
            string line2 = "[x] [color=blue]Vote for stuff[/color]";
            string line3 = "[x] Vote [b]for[/b] stuff";
            string line4 = "[color=blue][x] [b]Vote for stuff[/b]";
            string line5 = "-[x] [url=http://link]Vote for stuff[/url]";
            string line6 = "-[x] Vote for [color=blue]“stuff”[/color]";
            string line7 = "[color=blue][x][/color][main] Vote for stuff";
            string line8 = "[color=blue][x][/color][main] [b][b]Vote[/b] for stuff";

            Assert.AreEqual("[x] Vote for stuff", VoteString.CleanVoteLineBBCode(line1));
            Assert.AreEqual("[x] [color=blue]Vote for stuff[/color]", VoteString.CleanVoteLineBBCode(line2));
            Assert.AreEqual("[x] Vote [b]for[/b] stuff", VoteString.CleanVoteLineBBCode(line3));
            Assert.AreEqual("[x] [b]Vote for stuff[/b]", VoteString.CleanVoteLineBBCode(line4));
            Assert.AreEqual("-[x] [url=http://link]Vote for stuff[/url]", VoteString.CleanVoteLineBBCode(line5));
            Assert.AreEqual("-[x] Vote for [color=blue]“stuff”[/color]", VoteString.CleanVoteLineBBCode(line6));
            Assert.AreEqual("[x][main] Vote for stuff", VoteString.CleanVoteLineBBCode(line7));
            Assert.AreEqual("[x][main] [b][b]Vote[/b] for stuff[/b]", VoteString.CleanVoteLineBBCode(line8));
        }

        [TestMethod()]
        public void CleanVoteTest3()
        {
            string line1      = "[X] - Brutalize them. You haven’t had a chance to properly fight in [/color][i][color=#ebebeb]years[/color][/i][color=#ebebeb], and spars can only do so much. How thoughtful of the Herans to volunteer!";
            string cleanLine1 = "[X] - Brutalize them. You haven’t had a chance to properly fight in [i][color=#ebebeb]years[/color][/i], and spars can only do so much. How thoughtful of the Herans to volunteer!";

            string out1 = VoteString.CleanVoteLineBBCode(cleanLine1);
            string out2 = VoteString.CleanVoteLineBBCode(line1);

            Assert.AreEqual(cleanLine1, out1);
            Assert.AreEqual(cleanLine1, out2);
        }

        [TestMethod()]
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

        [TestMethod()]
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

        [TestMethod()]
        public void GetVoteTaskTest()
        {
            string line1 = "[x] Vote for stuff";
            string line2 = "-[x] Vote for stuff";
            string line3 = "[x][major] Vote for stuff";
            string line4 = "-[x][ animal] Vote for stuff";
            string line5 = "[x][MINOR] Vote for stuff";
            string line6 = "[x][Trade Relations] Vote for stuff";
            string line7 = "[x] [url=http://google.com]<image>[/url]";
            string line8 = "[x] [b]Vote for stuff[/b]";

            Assert.AreEqual("", VoteString.GetVoteTask(line1));
            Assert.AreEqual("", VoteString.GetVoteTask(line2));
            Assert.AreEqual("major", VoteString.GetVoteTask(line3));
            Assert.AreEqual("animal", VoteString.GetVoteTask(line4));
            Assert.AreEqual("MINOR", VoteString.GetVoteTask(line5));
            Assert.AreEqual("Trade Relations", VoteString.GetVoteTask(line6));
            Assert.AreEqual("", VoteString.GetVoteTask(line7));
            Assert.AreEqual("", VoteString.GetVoteTask(line8));
        }

        [TestMethod()]
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

        [TestMethod()]
        public void GetVoteContentTest()
        {
            string input = "[X] We [i]did[/i] agree to non-lethal. My most [color=blue]powerful[/color] stuff either knocks people out or kills them without having to fight at all. Everything else I've learned to do so far feels like a witch barrier, and I try not to use that since it freaks everyone out.";
            string expected = "We [i]did[/i] agree to non-lethal. My most [color=blue]powerful[/color] stuff either knocks people out or kills them without having to fight at all. Everything else I've learned to do so far feels like a witch barrier, and I try not to use that since it freaks everyone out.";

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
            input = "[color=blue]-[x] Vote for stuff[/color]";
            expected = "";
            Assert.AreEqual(expected, VoteString.GetVoteContent(input));

        }

        [TestMethod()]
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


        [TestMethod()]
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

        [TestMethod()]
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

        [TestMethod()]
        public void GetVotePlanNameTest3()
        {
            string input = "[x] Kinematics.";
            string expected1 = "Kinematics.";
            string expected2 = "\u25C8Kinematics.";
            string expected3 = "Kinematics";
            string expected4 = "\u25C8Kinematics";
            var result = VoteString.GetVoteReferenceNames(input);
            Assert.AreEqual(4, result[ReferenceType.Any].Count);
            Assert.IsTrue(result[ReferenceType.Any].Contains(expected1));
            Assert.IsTrue(result[ReferenceType.Any].Contains(expected2));
            Assert.IsTrue(result[ReferenceType.Any].Contains(expected3));
            Assert.IsTrue(result[ReferenceType.Any].Contains(expected4));
        }

        [TestMethod()]
        public void GetVotePlanNameTest4()
        {
            string input = "[x] Plan Assault.";
            string expected1 = "Assault.";
            string expected2 = "\u25C8Assault.";
            string expected3 = "Assault";
            string expected4 = "\u25C8Assault";
            var result = VoteString.GetVoteReferenceNames(input);
            Assert.AreEqual(4, result[ReferenceType.Any].Count);
            Assert.IsTrue(result[ReferenceType.Any].Contains(expected1));
            Assert.IsTrue(result[ReferenceType.Any].Contains(expected2));
            Assert.IsTrue(result[ReferenceType.Any].Contains(expected3));
            Assert.IsTrue(result[ReferenceType.Any].Contains(expected4));
            
        }

        [TestMethod()]
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

        [TestMethod()]
        public void GetVotePlanNameTest6()
        {
            string input = "[x] [url=https://forum.questionablequesting.com/members/2392/]Xryuran[/url].";
            string expected1 = "Xryuran.";
            string expected2 = "\u25C8Xryuran.";
            string expected3 = "Xryuran";
            string expected4 = "\u25C8Xryuran";
            var result = VoteString.GetVoteReferenceNames(input);
            Assert.AreEqual(4, result[ReferenceType.Any].Count);
            Assert.IsTrue(result[ReferenceType.Any].Contains(expected1));
            Assert.IsTrue(result[ReferenceType.Any].Contains(expected2));
            Assert.IsTrue(result[ReferenceType.Any].Contains(expected3));
            Assert.IsTrue(result[ReferenceType.Any].Contains(expected4));
        }

        [TestMethod()]
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

        [TestMethod()]
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

        [TestMethod()]
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

        [TestMethod()]
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

        [TestMethod()]
        [ExpectedException(typeof(InvalidOperationException))]
        public void GetVoteComponentsTest4()
        {
            string input = "[color=blue]-[x][MAJOR] Vote for stuff[/color]";
            string prefix;
            string marker;
            string task;
            string content;

            VoteString.GetVoteComponents(input, out prefix, out marker, out task, out content);

            Assert.AreEqual("-", prefix);
            Assert.AreEqual("x", marker);
            Assert.AreEqual("MAJOR", task);
            Assert.AreEqual("[color=blue]Vote for stuff[/color]", content);
        }

        [TestMethod()]
        [ExpectedException(typeof(InvalidOperationException))]
        public void GetVoteComponentsTest5()
        {
            string input = "-[[b]x][MAJOR] Vote for stuff[/b]";
            string prefix;
            string marker;
            string task;
            string content;

            VoteString.GetVoteComponents(input, out prefix, out marker, out task, out content);

            Assert.AreEqual("-", prefix);
            Assert.AreEqual("x", marker);
            Assert.AreEqual("MAJOR", task);
            Assert.AreEqual("[b]Vote for stuff[/b]", content);
        }


        [TestMethod()]
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

        [TestMethod()]
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

            input = "[1][Major] [b]Vote for stuff[/b]";
            expected = "[Major] [b]Vote for stuff[/b]";

            Assert.AreEqual(expected, VoteString.CondenseVote(input));

        }

        [TestMethod()]
        [ExpectedException(typeof(InvalidOperationException))]
        public void CondenseVoteTest2()
        {
            // Shouldn't be able to generate this working string anymore:
            string input = "[b][1] Vote for stuff[/b]";
            string expected = "[] ";

            Assert.AreEqual(expected, VoteString.CondenseVote(input));
        }

        [TestMethod()]
        public void GetPlanNameTest1()
        {
            string input = "[X] Kinematics";

            Assert.AreEqual(null, VoteString.GetPlanName(input));
            Assert.AreEqual(null, VoteString.GetPlanName(input, true));
            Assert.AreEqual(null, VoteString.GetPlanName(input, false));
        }

        [TestMethod()]
        public void GetPlanNameTest2()
        {
            string input = "[X] Plan Kinematics";

            Assert.AreEqual("Kinematics", VoteString.GetPlanName(input));
            Assert.AreEqual(null, VoteString.GetPlanName(input, true));
            Assert.AreEqual("Kinematics", VoteString.GetPlanName(input, false));
        }

        [TestMethod()]
        public void GetPlanNameTest3()
        {
            string input = "[X] Base Plan Kinematics";

            Assert.AreEqual("Kinematics", VoteString.GetPlanName(input));
            Assert.AreEqual("Kinematics", VoteString.GetPlanName(input, true));
            Assert.AreEqual("Kinematics", VoteString.GetPlanName(input, false));
        }

        [TestMethod()]
        public void GetPlanNameTest4()
        {
            string input = "[X] Base Plan : Kinematics";

            Assert.AreEqual("Kinematics", VoteString.GetPlanName(input));
            Assert.AreEqual("Kinematics", VoteString.GetPlanName(input, true));
            Assert.AreEqual("Kinematics", VoteString.GetPlanName(input, false));
        }

        [TestMethod()]
        public void GetPlanNameTest5()
        {
            string input = "[X] Plan: Kinematics";

            Assert.AreEqual("Kinematics", VoteString.GetPlanName(input));
            Assert.AreEqual(null, VoteString.GetPlanName(input, true));
            Assert.AreEqual("Kinematics", VoteString.GetPlanName(input, false));
        }

        [TestMethod()]
        public void GetPlanNameTest6()
        {
            string input = "[X] Plan: Kinematics";
            string expect = Text.PlanNameMarker + "Kinematics";

            Assert.AreEqual(expect, VoteString.GetMarkedPlanName(input));
        }

        [TestMethod()]
        public void GetPlanNameTest7()
        {
            string input = "[X] Kinematics";
            string expect = Text.PlanNameMarker + "Kinematics";

            Assert.AreEqual(null, VoteString.GetMarkedPlanName(input));
        }


    }
}
