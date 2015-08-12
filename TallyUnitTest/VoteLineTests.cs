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
    public class VoteLineTests
    {
        [TestMethod()]
        public void CleanVoteTest()
        {
            string cleanLine1 = "[x] Vote for stuff";
            string cleanLine2 = "-[x] Vote for stuff";
            string cleanLine3 = "[x][main] Vote for stuff";

            string line1 = "[b][x] Vote for stuff[/b]";
            string line2 = "[color=blue][x] Vote for stuff[/color]";
            string line3 = "[b][x] Vote for stuff";
            string line4 = "[color=blue][b][x] Vote for stuff[/b]";
            string line5 = "[b]-[x] Vote for stuff";
            string line6 = "[color=blue]-[x] Vote for stuff[/color]";
            string line7 = "[color=blue][x][main] Vote for stuff[/color]";

            string e = "";

            Assert.AreEqual(cleanLine1, VoteString.CleanVote(cleanLine1));
            Assert.AreEqual(cleanLine1, VoteString.CleanVote(line1));
            Assert.AreEqual(cleanLine1, VoteString.CleanVote(line2));
            Assert.AreEqual(cleanLine1, VoteString.CleanVote(line3));
            Assert.AreEqual(cleanLine1, VoteString.CleanVote(line4));

            Assert.AreEqual(cleanLine2, VoteString.CleanVote(cleanLine2));
            Assert.AreEqual(cleanLine2, VoteString.CleanVote(line5));
            Assert.AreEqual(cleanLine2, VoteString.CleanVote(line6));

            Assert.AreEqual(cleanLine3, VoteString.CleanVote(cleanLine3));
            Assert.AreEqual(cleanLine3, VoteString.CleanVote(line7));

            Assert.AreEqual(e, VoteString.CleanVote(e));
        }


        [TestMethod()]
        public void MinimizeVoteTest()
        {
            string input = "[X] We [i]did[/i] agree to non-lethal. My most [color=blue]powerful[/color] stuff either knocks people out or kills them without having to fight at all. Everything else I've learned to do so far feels like a witch barrier, and I try not to use that since it freaks everyone out.";
            string expected = "[x]wedidagreetonon-lethalmymostpowerfulstuffeitherknockspeopleoutorkillsthemwithouthavingtofightatalleverythingelsei'velearnedtodosofarfeelslikeawitchbarrier,anditrynottousethatsinceitfreakseveryoneout";

            IQuest quest = new Quest() { PartitionMode = PartitionMode.None };
            string results = VoteString.MinimizeVote(input, quest.PartitionMode);
            Assert.AreEqual(expected, results);
        }

        [TestMethod()]
        public void MinimizeBlockTest()
        {
            string input = "-[X] We [i]did[/i] agree to non-lethal. My most [color=blue]powerful[/color] stuff either knocks people out or kills them without having to fight at all. Everything else I've learned to do so far feels like a witch barrier, and I try not to use that since it freaks everyone out.";
            string expected = "-[x]wedidagreetonon-lethalmymostpowerfulstuffeitherknockspeopleoutorkillsthemwithouthavingtofightatalleverythingelsei'velearnedtodosofarfeelslikeawitchbarrier,anditrynottousethatsinceitfreakseveryoneout";

            IQuest quest = new Quest() { PartitionMode = PartitionMode.ByBlock };
            string results = VoteString.MinimizeVote(input, quest.PartitionMode);
            Assert.AreEqual(expected, results);
        }

        [TestMethod()]
        public void MinimizeLineTest()
        {
            string input = "-[X] We [i]did[/i] agree to non-lethal. My most [color=blue]powerful[/color] stuff either knocks people out or kills them without having to fight at all. Everything else I've learned to do so far feels like a witch barrier, and I try not to use that since it freaks everyone out.";
            string expected = "[x]wedidagreetonon-lethalmymostpowerfulstuffeitherknockspeopleoutorkillsthemwithouthavingtofightatalleverythingelsei'velearnedtodosofarfeelslikeawitchbarrier,anditrynottousethatsinceitfreakseveryoneout";

            IQuest quest = new Quest() { PartitionMode = PartitionMode.ByLine };
            string results = VoteString.MinimizeVote(input, quest.PartitionMode);
            Assert.AreEqual(expected, results);
        }
        
        [TestMethod()]
        public void GetVotePrefixTest()
        {
            string line1 = "[x] Vote for stuff";
            string line2 = "-[x] Vote for stuff";

            Assert.AreEqual("", VoteString.GetVotePrefix(line1));
            Assert.AreEqual("-", VoteString.GetVotePrefix(line2));

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

            Assert.AreEqual("", VoteString.GetVoteTask(line1));
            Assert.AreEqual("", VoteString.GetVoteTask(line2));
            Assert.AreEqual("Major", VoteString.GetVoteTask(line3));
            Assert.AreEqual("Animal", VoteString.GetVoteTask(line4));
            Assert.AreEqual("Minor", VoteString.GetVoteTask(line5));
            Assert.AreEqual("Trade relations", VoteString.GetVoteTask(line6));
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
        public void GetVotePlanNameTest()
        {
            string input = "[x] Kinematics";
            string expected = "Kinematics";
            Assert.AreEqual(expected, VoteString.GetVoteReferenceName(input));

            input = "[x] Plan Assault";
            expected = "\u25C8Assault";
            Assert.AreEqual(expected, VoteString.GetVoteReferenceName(input));
        }

        [TestMethod()]
        public void GetVoteAltPlanNameTest()
        {
            string input = "[x] Kinematics";
            string expected = "\u25C8Kinematics";
            Assert.AreEqual(expected, VoteString.GetVoteReferenceName(input, true));

            input = "[x] Plan Assault";
            expected = "Assault";
            Assert.AreEqual(expected, VoteString.GetVoteReferenceName(input, true));
        }

        [TestMethod()]
        public void GetVoteComponentsTest()
        {
            string input = "[color=blue]-[x][MAJOR] Vote for stuff[/color]";
            string prefix;
            string marker;
            string task;
            string content;

            VoteString.GetVoteComponents(input, out prefix, out marker, out task, out content);

            Assert.AreEqual("-", prefix);
            Assert.AreEqual("x", marker);
            Assert.AreEqual("Major", task);
            Assert.AreEqual("Vote for stuff", content);
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

    }
}
