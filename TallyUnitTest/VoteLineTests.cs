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

    }
}
