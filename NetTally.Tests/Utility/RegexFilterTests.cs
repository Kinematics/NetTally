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
            var filter = RegexFilter.AlwaysAllow;

            Assert.IsTrue(filter.Allows(""));
            Assert.IsTrue(filter.Allows("stuff"));
            Assert.IsTrue(filter.Allows("lots of stuff (omake)"));
            Assert.IsTrue(filter.Allows("[x][Task] Some vote"));
            Assert.IsFalse(filter.Blocks(""));
            Assert.IsFalse(filter.Blocks("stuff"));
            Assert.IsFalse(filter.Blocks("lots of stuff (omake)"));
            Assert.IsFalse(filter.Blocks("[x][Task] Some vote"));
        }

        [TestMethod]
        public void Block_All()
        {
            var filter = RegexFilter.AlwaysBlock;

            Assert.IsFalse(filter.Allows(""));
            Assert.IsFalse(filter.Allows("stuff"));
            Assert.IsFalse(filter.Allows("lots of stuff (omake)"));
            Assert.IsFalse(filter.Allows("[x][Task] Some vote"));
            Assert.IsTrue(filter.Blocks(""));
            Assert.IsTrue(filter.Blocks("stuff"));
            Assert.IsTrue(filter.Blocks("lots of stuff (omake)"));
            Assert.IsTrue(filter.Blocks("[x][Task] Some vote"));
        }

        [TestMethod]
        public void Block_Omake()
        {
            RegexPattern omake = RegexPattern.Create("omake");

            var filter = RegexFilter.Block(omake);

            Assert.IsTrue(filter.Allows(""));
            Assert.IsTrue(filter.Allows("stuff"));
            Assert.IsTrue(filter.Blocks("lots of stuff (omake)"));
            Assert.IsTrue(filter.Blocks("lots of stuff (goomake)"));
            Assert.IsTrue(filter.Allows("[x][Task] Some vote"));
        }

        [TestMethod]
        public void Block_Omake_Default()
        {
            var filter = RegexFilter.DefaultThreadmarkFilter;

            Assert.IsTrue(filter.Allows(""));
            Assert.IsTrue(filter.Allows("stuff"));
            Assert.IsTrue(filter.Blocks("lots of stuff (omake)"));
            Assert.IsTrue(filter.Allows("lots of stuff (goomake)"));
            Assert.IsTrue(filter.Allows("[x][Task] Some vote"));
        }

        [TestMethod]
        public void Block_Pattern_1()
        {
            RegexPattern pattern = RegexPattern.Create(@"/\w+\d+/");

            var filter = RegexFilter.Block(pattern);

            Assert.IsTrue(filter.Allows(""));
            Assert.IsTrue(filter.Allows("stuff"));
            Assert.IsTrue(filter.Allows("lots of stuff (omake)"));
            Assert.IsTrue(filter.Allows("lots of stuff (goomake)"));
            Assert.IsTrue(filter.Allows("[x][Task] Some vote"));
            Assert.IsTrue(filter.Blocks("[x][Task] Some123 vote"));
        }

        [TestMethod]
        public void Block_Pattern_2()
        {
            RegexPattern pattern = RegexPattern.Create(@"/\w+\d+|goo/");

            var filter = RegexFilter.Block(pattern);

            Assert.IsTrue(filter.Allows(""));
            Assert.IsTrue(filter.Allows("stuff"));
            Assert.IsTrue(filter.Allows("lots of stuff (omake)"));
            Assert.IsTrue(filter.Blocks("lots of stuff (goomake)"));
            Assert.IsTrue(filter.Allows("[x][Task] Some vote"));
            Assert.IsTrue(filter.Blocks("[x][Task] Some123 vote"));
        }

        [TestMethod]
        public void Block_MultiPattern_1()
        {
            RegexPattern pattern = RegexPattern.Create(@"stuff");
            RegexPattern omake = RegexPattern.Create("omake");

            var filter = RegexFilter.Block(pattern, omake);

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
            RegexPattern pattern = RegexPattern.Create(@"stuff");
            RegexPattern omake = RegexPattern.Create("omake");

            var filter = RegexFilter.Allow(pattern, omake);

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
            RegexPattern pattern = RegexPattern.Create(@"/stuff/");
            RegexPattern omake = RegexPattern.Create("omake");

            var filter = RegexFilter.Block(pattern, omake);

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
            RegexPattern pattern = RegexPattern.Create(@"/stuff/");
            RegexPattern omake = RegexPattern.Create("omake");

            var filter = RegexFilter.Allow(pattern, omake);

            Assert.IsFalse(filter.Allows(""));
            Assert.IsTrue(filter.Allows("stuff"));
            Assert.IsTrue(filter.Allows("lots of grass (omake)"));
            Assert.IsTrue(filter.Allows("lots of stuff (goomake)"));
            Assert.IsFalse(filter.Allows("[x][Task] Some vote"));
            Assert.IsFalse(filter.Allows("[x][Task] Some123 vote"));
        }

        [TestMethod]
        public void Block_Simple_Letter_1()
        {
            RegexPattern pattern = RegexPattern.Create(@"c?t");

            var filter = RegexFilter.Block(pattern);

            Assert.IsTrue(filter.Allows(""));
            Assert.IsTrue(filter.Allows("stuff"));
            Assert.IsTrue(filter.Blocks("lots of cat (omake)"));
            Assert.IsTrue(filter.Blocks("lots of cats (omake)"));
            Assert.IsTrue(filter.Allows("lots of stuff (goomake)"));
            Assert.IsTrue(filter.Allows("[x][Task] Some vote"));
            Assert.IsTrue(filter.Allows("[x][Task] Some123 vote"));
        }

        [TestMethod]
        public void Block_Simple_Letter_1_Bounded()
        {
            RegexPattern pattern = RegexPattern.Create(@"[c?t]");

            var filter = RegexFilter.Block(pattern);

            Assert.IsTrue(filter.Allows(""));
            Assert.IsTrue(filter.Allows("stuff"));
            Assert.IsTrue(filter.Blocks("lots of cat (omake)"));
            Assert.IsTrue(filter.Allows("lots of cats (omake)"));
            Assert.IsTrue(filter.Allows("lots of stuff (goomake)"));
            Assert.IsTrue(filter.Allows("[x][Task] Some vote"));
            Assert.IsTrue(filter.Allows("[x][Task] Some123 vote"));
        }

        [TestMethod]
        public void Block_Splat_1()
        {
            RegexPattern pattern = RegexPattern.Create(@"c*t");

            var filter = RegexFilter.Block(pattern);

            Assert.IsTrue(filter.Allows(""));
            Assert.IsTrue(filter.Allows("stuff"));
            Assert.IsTrue(filter.Blocks("lots of cat (omake)"));
            Assert.IsTrue(filter.Blocks("lots of cult (omake)"));
            Assert.IsTrue(filter.Blocks("lots of chocolat (goomake)"));
            Assert.IsTrue(filter.Blocks("lots of chocolate (goomake)"));
            Assert.IsTrue(filter.Allows("[x][Task] Some vote"));
            Assert.IsTrue(filter.Allows("[x][Task] Some123 vote"));
        }

        [TestMethod]
        public void Block_Splat_1_Bounded()
        {
            RegexPattern pattern = RegexPattern.Create(@"[c*t]");

            var filter = RegexFilter.Block(pattern);

            Assert.IsTrue(filter.Allows(""));
            Assert.IsTrue(filter.Allows("stuff"));
            Assert.IsTrue(filter.Blocks("lots of cat (omake)"));
            Assert.IsTrue(filter.Blocks("lots of cult (omake)"));
            Assert.IsTrue(filter.Blocks("lots of chocolat (goomake)"));
            Assert.IsTrue(filter.Allows("lots of chocolate (goomake)"));
            Assert.IsTrue(filter.Allows("[x][Task] Some vote"));
            Assert.IsTrue(filter.Allows("[x][Task] Some123 vote"));
        }

    }
}
