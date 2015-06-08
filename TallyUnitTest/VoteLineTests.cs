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
        public void MinimizeVoteTest()
        {
            string input = "[X] We [i]did[/i] agree to non-lethal. My most [color=blue]powerful[/color] stuff either knocks people out or kills them without having to fight at all. Everything else I've learned to do so far feels like a witch barrier, and I try not to use that since it freaks everyone out.";
            string expected = "[x]wedidagreetonon-lethalmymostpowerfulstuffeitherknockspeopleoutorkillsthemwithouthavingtofightatalleverythingelsei'velearnedtodosofarfeelslikeawitchbarrier,anditrynottousethatsinceitfreakseveryoneout";

            IQuest quest = new Quest() { UseVotePartitions = false, PartitionByLine = true };
            string results = VoteLine.MinimizeVote(input, quest);
            Assert.AreEqual(expected, results);
        }

        [TestMethod()]
        public void MinimizeBlockTest()
        {
            string input = "-[X] We [i]did[/i] agree to non-lethal. My most [color=blue]powerful[/color] stuff either knocks people out or kills them without having to fight at all. Everything else I've learned to do so far feels like a witch barrier, and I try not to use that since it freaks everyone out.";
            string expected = "-[x]wedidagreetonon-lethalmymostpowerfulstuffeitherknockspeopleoutorkillsthemwithouthavingtofightatalleverythingelsei'velearnedtodosofarfeelslikeawitchbarrier,anditrynottousethatsinceitfreakseveryoneout";

            IQuest quest = new Quest() { UseVotePartitions = true, PartitionByLine = false };
            string results = VoteLine.MinimizeVote(input, quest);
            Assert.AreEqual(expected, results);
        }

        [TestMethod()]
        public void MinimizeLineTest()
        {
            string input = "-[X] We [i]did[/i] agree to non-lethal. My most [color=blue]powerful[/color] stuff either knocks people out or kills them without having to fight at all. Everything else I've learned to do so far feels like a witch barrier, and I try not to use that since it freaks everyone out.";
            string expected = "[x]wedidagreetonon-lethalmymostpowerfulstuffeitherknockspeopleoutorkillsthemwithouthavingtofightatalleverythingelsei'velearnedtodosofarfeelslikeawitchbarrier,anditrynottousethatsinceitfreakseveryoneout";

            IQuest quest = new Quest() { UseVotePartitions = true, PartitionByLine = true };
            string results = VoteLine.MinimizeVote(input, quest);
            Assert.AreEqual(expected, results);
        }

        [TestMethod()]
        public void StripFormattingTest()
        {
            string line1 = "[x] Vote for stuff";
            string line2 = "-[x] Vote for stuff";

            string line3 = "[b][x] Vote for stuff[/b]";
            string line4 = "[color=blue][x] Vote for stuff[/color]";
            string line5 = "[b][x] Vote for stuff";
            string line6 = "[color=blue][b][x] Vote for stuff[/b]";
            string line7 = "[b]-[x] Vote for stuff";
            string line8 = "[color=blue]-[x] Vote for stuff[/color]";

            string e = "";

            Assert.AreEqual(line1, VoteLine.CleanVote(line1));
            Assert.AreEqual(line1, VoteLine.CleanVote(line3));
            Assert.AreEqual(line1, VoteLine.CleanVote(line4));
            Assert.AreEqual(line1, VoteLine.CleanVote(line5));
            Assert.AreEqual(line1, VoteLine.CleanVote(line6));

            Assert.AreEqual(line2, VoteLine.CleanVote(line2));
            Assert.AreEqual(line2, VoteLine.CleanVote(line7));
            Assert.AreEqual(line2, VoteLine.CleanVote(line8));

            Assert.AreEqual(e, VoteLine.CleanVote(e));
        }

    }
}
