using Microsoft.VisualStudio.TestTools.UnitTesting;
using NetTally;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Runtime.CompilerServices;

namespace NetTally.Tests
{
    [TestClass()]
    public class VoteCounterTests
    {
        static VoteCounter voteCounter;
        static PrivateObject privateVote;

        [ClassInitialize()]
        public static void ClassInit(TestContext context)
        {
            voteCounter = new VoteCounter();
            privateVote = new PrivateObject(voteCounter);
        }

        [TestMethod()]
        public void TallyVotesTest()
        {
        }

        [TestMethod()]
        public void CleanVoteTest()
        {
            string input = "[X] We [i]did[/i] agree to non-lethal. My most [color=blue]powerful[/color] stuff either knocks people out or kills them without having to fight at all. Everything else I've learned to do so far feels like a witch barrier, and I try not to use that since it freaks everyone out.";
            string expected = "[x]wedidagreetonon-lethalmymostpowerfulstuffeitherknockspeopleoutorkillsthemwithouthavingtofightatalleverythingelsei'velearnedtodosofarfeelslikeawitchbarrier,anditrynottousethatsinceitfreakseveryoneout";

            var results = privateVote.Invoke("CleanVote", input);
            Assert.AreEqual(expected, results);
        }
    }
}