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
        [TestMethod()]
        public void TallyVotesTest()
        {
            Assert.Fail();
            var a = new VoteCounter();
            var b = new PrivateObject(a);
        }
    }
}