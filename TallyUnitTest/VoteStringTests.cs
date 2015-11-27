using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NetTally;

namespace TallyUnitTest
{
    [TestClass()]
    public class VoteStringTests
    {
        [TestMethod()]
        public void NormalizeVoteTest()
        {
            string line1 = "[x] Vote for stuff";
            string normLine1 = "[x] Vote for stuff";

            string line2 = "[x] Vote for \"stuff\"";
            string normLine2 = "[x] Vote for \"stuff\"";

            string line3 = "[x] Vote for “stuff”";
            string normLine3 = "[x] Vote for \"stuff\"";

            string line4 = "[x] Don't vote for stuff";
            string normLine4 = "[x] Don't vote for stuff";

            string line5 = "[x] Donʼt vote for stuff";
            string normLine5 = "[x] Don't vote for stuff";

            string line6 = "[x] Don`t vote for stuff";
            string normLine6 = "[x] Don't vote for stuff";

            string line7 = @"[x] Vote for “stuff”
[x] Vote for more “stuff”";
            string normLine7 = @"[x] Vote for ""stuff""
[x] Vote for more ""stuff""";

            string e = "";

            Assert.AreEqual(normLine1, VoteString.CleanVote(line1));
            Assert.AreEqual(normLine2, VoteString.CleanVote(line2));
            Assert.AreEqual(normLine3, VoteString.CleanVote(line3));
            Assert.AreEqual(normLine4, VoteString.CleanVote(line4));
            Assert.AreEqual(normLine5, VoteString.CleanVote(line5));
            Assert.AreEqual(normLine6, VoteString.CleanVote(line6));
            Assert.AreEqual(normLine7, VoteString.CleanVote(line7));

            Assert.AreEqual(e, VoteString.CleanVote(e));
        }


        [TestMethod()]
        public void CleanVoteTest()
        {
            string cleanLine1 = "[x] Vote for stuff";
            string cleanLine2 = "-[x] Vote for stuff";
            string cleanLine3 = "[x][main] Vote for stuff";
            string cleanLine4 = "-[x] Vote for \"stuff\"";

            string line1 = "[b][x] Vote for stuff[/b]";
            string line2 = "[color=blue][x] Vote for stuff[/color]";
            string line3 = "[b][x] Vote for stuff";
            string line4 = "[color=blue][b][x] Vote for stuff[/b]";
            string line5 = "[b]-[x] Vote for stuff";
            string line6 = "[color=blue]-[x] Vote for “stuff”[/color]";
            string line7 = "[color=blue][x][main] Vote for stuff[/color]";

            string e = "";

            Assert.AreEqual(cleanLine1, VoteString.CleanVote(cleanLine1));
            Assert.AreEqual(cleanLine1, VoteString.CleanVote(line1));
            Assert.AreEqual(cleanLine1, VoteString.CleanVote(line2));
            Assert.AreEqual(cleanLine1, VoteString.CleanVote(line3));
            Assert.AreEqual(cleanLine1, VoteString.CleanVote(line4));

            Assert.AreEqual(cleanLine2, VoteString.CleanVote(cleanLine2));
            Assert.AreEqual(cleanLine2, VoteString.CleanVote(line5));
            Assert.AreEqual(cleanLine4, VoteString.CleanVote(line6));

            Assert.AreEqual(cleanLine3, VoteString.CleanVote(cleanLine3));
            Assert.AreEqual(cleanLine3, VoteString.CleanVote(line7));

            Assert.AreEqual(e, VoteString.CleanVote(e));
        }


        [TestMethod()]
        public void MinimizeVoteTest()
        {
            string input = "[X] We [i]did[/i] agree to non-lethal. My most [color=blue]powerful[/color] stuff either knocks people out or kills them without having to fight at all. Everything else I've learned to do so far feels like a witch barrier, and I try not to use that since it freaks everyone out.";
            string expected = "[x]wedidagreetonon-lethalmymostpowerfulstuffeitherknockspeopleoutorkillsthemwithouthavingtofightatalleverythingelsei'velearnedtodosofarfeelslikeawitchbarrier,anditrynottousethatsinceitfreakseveryoneout";

            string results = VoteString.MinimizeVote(input);
            Assert.AreEqual(expected, results);
        }

        [TestMethod()]
        public void MinimizeBlockTest()
        {
            string input = "-[X] We [i]did[/i] agree to non-lethal. My most [color=blue]powerful[/color] stuff either knocks people out or kills them without having to fight at all. Everything else I've learned to do so far feels like a witch barrier, and I try not to use that since it freaks everyone out.";
            string expected = "[x]wedidagreetonon-lethalmymostpowerfulstuffeitherknockspeopleoutorkillsthemwithouthavingtofightatalleverythingelsei'velearnedtodosofarfeelslikeawitchbarrier,anditrynottousethatsinceitfreakseveryoneout";

            string results = VoteString.MinimizeVote(input);
            Assert.AreEqual(expected, results);
        }

        [TestMethod()]
        public void MinimizeLineTest()
        {
            string input = "-[X] We [i]did[/i] agree to non-lethal. My most [color=blue]powerful[/color] stuff either knocks people out or kills them without having to fight at all. Everything else I've learned to do so far feels like a witch barrier, and I try not to use that since it freaks everyone out.";
            string expected = "[x]wedidagreetonon-lethalmymostpowerfulstuffeitherknockspeopleoutorkillsthemwithouthavingtofightatalleverythingelsei'velearnedtodosofarfeelslikeawitchbarrier,anditrynottousethatsinceitfreakseveryoneout";

            string results = VoteString.MinimizeVote(input);
            Assert.AreEqual(expected, results);
        }
        
        [TestMethod()]
        public void GetVotePrefixTest()
        {
            string line1 = "[x] Vote for stuff";
            string line2 = "-[x] Vote for stuff";
            string line3 = "---[x] Vote for stuff";

            Assert.AreEqual("", VoteString.GetVotePrefix(line1));
            Assert.AreEqual("-", VoteString.GetVotePrefix(line2));
            Assert.AreEqual("---", VoteString.GetVotePrefix(line3));
        }

        [TestMethod()]
        public void GetVoteMarkerTest()
        {
            string line1 = "[x] Vote for stuff";
            string line2 = "-[X] Vote for stuff";
            string line3 = "-[+] Vote for stuff";
            string line4 = "[✓][major] Vote for stuff";
            string line5 = "-[ ✔][ animal] Vote for stuff";

            Assert.AreEqual("x", VoteString.GetVoteMarker(line1));
            Assert.AreEqual("X", VoteString.GetVoteMarker(line2));
            Assert.AreEqual("+", VoteString.GetVoteMarker(line3));
            Assert.AreEqual("✓", VoteString.GetVoteMarker(line4));
            Assert.AreEqual("✔", VoteString.GetVoteMarker(line5));

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

            Assert.AreEqual("", VoteString.GetVoteTask(line1));
            Assert.AreEqual("", VoteString.GetVoteTask(line2));
            Assert.AreEqual("major", VoteString.GetVoteTask(line3));
            Assert.AreEqual("animal", VoteString.GetVoteTask(line4));
            Assert.AreEqual("MINOR", VoteString.GetVoteTask(line5));
            Assert.AreEqual("Trade Relations", VoteString.GetVoteTask(line6));
            Assert.AreEqual("", VoteString.GetVoteTask(line7));
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
            string expected = "We did agree to non-lethal. My most powerful stuff either knocks people out or kills them without having to fight at all. Everything else I've learned to do so far feels like a witch barrier, and I try not to use that since it freaks everyone out.";

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

            input = "[color=blue]-[x] Vote for stuff[/color]";
            expected = "Vote for stuff";
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
        public void CondenseVoteTest()
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
            expected = "[Major] Vote for stuff";

            Assert.AreEqual(expected, VoteString.CondenseVote(input));

            input = "[b][1] Vote for stuff[/b]";
            expected = "[] Vote for stuff";

            Assert.AreEqual(expected, VoteString.CondenseVote(input));

        }

    }
}
