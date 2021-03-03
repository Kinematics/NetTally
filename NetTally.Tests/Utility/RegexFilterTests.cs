using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NetTally.Utility.Filtering;

namespace NetTally.Tests.Utility
{
    [TestClass]
    public class RegexFilterTests
    {
        [TestMethod]
        public void Allow_All()
        {
            RegexFilter filter = RegexFilter.AllowAll;

            Assert.IsTrue(filter.Allows(""));
            Assert.IsTrue(filter.Allows("stuff"));
            Assert.IsTrue(filter.Allows("lots of stuff (omake)"));
            Assert.IsTrue(filter.Allows("[x][Task] Some vote"));
        }

        [TestMethod]
        public void Block_All()
        {
            RegexFilter filter = RegexFilter.BlockAll;

            Assert.IsFalse(filter.Allows(""));
            Assert.IsFalse(filter.Allows("stuff"));
            Assert.IsFalse(filter.Allows("lots of stuff (omake)"));
            Assert.IsFalse(filter.Allows("[x][Task] Some vote"));
        }

        [TestMethod]
        public void Block_Omake()
        {
            RegexPattern omake = new RegexPattern("omake");

            RegexFilter filter = RegexFilter.Block(omake);

            Assert.IsTrue(filter.Allows(""));
            Assert.IsTrue(filter.Allows("stuff"));
            Assert.IsFalse(filter.Allows("lots of stuff (omake)"));
            Assert.IsTrue(filter.Allows("lots of stuff (goomake)"));
            Assert.IsTrue(filter.Allows("[x][Task] Some vote"));
        }

        [TestMethod]
        public void Block_Pattern_1()
        {
            RegexPattern pattern = new RegexPattern(@"/\w+\d+/");

            RegexFilter filter = RegexFilter.Block(pattern);

            Assert.IsTrue(filter.Allows(""));
            Assert.IsTrue(filter.Allows("stuff"));
            Assert.IsTrue(filter.Allows("lots of stuff (omake)"));
            Assert.IsTrue(filter.Allows("lots of stuff (goomake)"));
            Assert.IsTrue(filter.Allows("[x][Task] Some vote"));
            Assert.IsFalse(filter.Allows("[x][Task] Some123 vote"));
        }

        [TestMethod]
        public void Block_Pattern_2()
        {
            RegexPattern pattern = new RegexPattern(@"/\w+\d+|goo/");

            RegexFilter filter = RegexFilter.Block(pattern);

            Assert.IsTrue(filter.Allows(""));
            Assert.IsTrue(filter.Allows("stuff"));
            Assert.IsTrue(filter.Allows("lots of stuff (omake)"));
            Assert.IsFalse(filter.Allows("lots of stuff (goomake)"));
            Assert.IsTrue(filter.Allows("[x][Task] Some vote"));
            Assert.IsFalse(filter.Allows("[x][Task] Some123 vote"));
        }

        [TestMethod]
        public void Block_MultiPattern_1()
        {
            RegexPattern pattern = new RegexPattern(@"stuff");
            RegexPattern omake = new RegexPattern("omake");

            RegexFilter filter = RegexFilter.Block(pattern, omake);

            Assert.IsTrue(filter.Allows(""));
            Assert.IsFalse(filter.Allows("stuff"));
            Assert.IsFalse(filter.Allows("lots of grass (omake)"));
            Assert.IsFalse(filter.Allows("lots of stuff (goomake)"));
            Assert.IsTrue(filter.Allows("[x][Task] Some vote"));
            Assert.IsTrue(filter.Allows("[x][Task] Some123 vote"));
        }

        [TestMethod]
        public void Allow_MultiPattern_1()
        {
            RegexPattern pattern = new RegexPattern(@"stuff");
            RegexPattern omake = new RegexPattern("omake");

            RegexFilter filter = RegexFilter.Allow(pattern, omake);

            Assert.IsFalse(filter.Allows(""));
            Assert.IsTrue(filter.Allows("stuff"));
            Assert.IsTrue(filter.Allows("lots of grass (omake)"));
            Assert.IsTrue(filter.Allows("lots of stuff (goomake)"));
            Assert.IsFalse(filter.Allows("[x][Task] Some vote"));
            Assert.IsFalse(filter.Allows("[x][Task] Some123 vote"));
        }

        [TestMethod]
        public void Block_MultiPattern_2()
        {
            RegexPattern pattern = new RegexPattern(@"/stuff/");
            RegexPattern omake = new RegexPattern("omake");

            RegexFilter filter = RegexFilter.Block(pattern, omake);

            Assert.IsTrue(filter.Allows(""));
            Assert.IsFalse(filter.Allows("stuff"));
            Assert.IsFalse(filter.Allows("lots of grass (omake)"));
            Assert.IsFalse(filter.Allows("lots of stuff (goomake)"));
            Assert.IsTrue(filter.Allows("[x][Task] Some vote"));
            Assert.IsTrue(filter.Allows("[x][Task] Some123 vote"));
        }

        [TestMethod]
        public void Allow_MultiPattern_2()
        {
            RegexPattern pattern = new RegexPattern(@"/stuff/");
            RegexPattern omake = new RegexPattern("omake");

            RegexFilter filter = RegexFilter.Allow(pattern, omake);

            Assert.IsFalse(filter.Allows(""));
            Assert.IsTrue(filter.Allows("stuff"));
            Assert.IsTrue(filter.Allows("lots of grass (omake)"));
            Assert.IsTrue(filter.Allows("lots of stuff (goomake)"));
            Assert.IsFalse(filter.Allows("[x][Task] Some vote"));
            Assert.IsFalse(filter.Allows("[x][Task] Some123 vote"));
        }
    }
}
