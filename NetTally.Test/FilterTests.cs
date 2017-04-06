using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NetTally.Utility;

namespace NetTally.Tests
{
    [TestClass]
    public class FilterTests
    {
        [TestMethod]
        public void TestNullFilter()
        {
            Filter filter = new Filter(null);
            Assert.IsFalse(filter.Match(""));
            Assert.IsFalse(filter.Match("stuff"));
            Assert.IsFalse(filter.Match("lots of stuff (omake)"));
            Assert.IsFalse(filter.Match("[x][Task] Some vote"));
        }

        [TestMethod]
        public void TestBlankFilter_Simple()
        {
            Filter filter = new Filter("", null);
            Assert.IsTrue(filter.Match(""));
            Assert.IsFalse(filter.Match("stuff"));
            Assert.IsFalse(filter.Match("lots of stuff (omake)"));
            Assert.IsFalse(filter.Match("[x][Task] Some vote"));
        }

        [TestMethod]
        public void TestBlankFilter_Full()
        {
            Filter filter = new Filter("", null);
            Assert.IsTrue(filter.Match(""));
            Assert.IsFalse(filter.Match("stuff"));
            Assert.IsFalse(filter.Match("lots of stuff (omake)"));
            Assert.IsFalse(filter.Match("[x][Task] Some vote"));
        }

        [TestMethod]
        public void TestDefaultOmakeFilter_Simple()
        {
            Filter filter = new Filter("", Quest.OmakeFilter);
            Assert.IsFalse(filter.Match(""));
            Assert.IsFalse(filter.Match("stuff"));
            Assert.IsTrue(filter.Match("lots of stuff (omake)"));
            Assert.IsFalse(filter.Match("[x][Task] Some vote"));
        }

        [TestMethod]
        public void TestDefaultOmakeFilter_Full()
        {
            Filter filter = new Filter("", Quest.OmakeFilter);
            Assert.IsFalse(filter.Match(""));
            Assert.IsFalse(filter.Match("stuff"));
            Assert.IsTrue(filter.Match("lots of stuff (omake)"));
            Assert.IsFalse(filter.Match("[x][Task] Some vote"));
        }

        [TestMethod]
        public void TestOmakeStuffFilter_Simple()
        {
            Filter filter = new Filter("stuff", Quest.OmakeFilter);
            Assert.IsFalse(filter.Match(""));
            Assert.IsTrue(filter.Match("stuff"));
            Assert.IsTrue(filter.Match("lots of stuffing (omake)"));
            Assert.IsFalse(filter.Match("[x][Task] Some vote"));
        }

        [TestMethod]
        public void TestOmakeStuffFilter_Full()
        {
            Filter filter = new Filter("/stuff/", Quest.OmakeFilter);
            Assert.IsFalse(filter.Match(""));
            Assert.IsTrue(filter.Match("stuff"));
            Assert.IsTrue(filter.Match("lots of stuffing (omake)"));
            Assert.IsFalse(filter.Match("[x][Task] Some vote"));
        }

        [TestMethod]
        public void TestMultiValueFilter_Simple()
        {
            Filter filter = new Filter("stuff, stuffing", Quest.OmakeFilter);
            Assert.IsFalse(filter.Match(""));
            Assert.IsTrue(filter.Match("stuff"));
            Assert.IsTrue(filter.Match("lots of stuffing"));
            Assert.IsTrue(filter.Match("lots of stuffing (omake)"));
            Assert.IsFalse(filter.Match("[x][Task] Some vote"));
        }

        [TestMethod]
        public void TestMultiValueFilter_Full()
        {
            Filter filter = new Filter("/stuff|stuffing/", Quest.OmakeFilter);
            Assert.IsFalse(filter.Match(""));
            Assert.IsTrue(filter.Match("stuff"));
            Assert.IsTrue(filter.Match("lots of stuffing"));
            Assert.IsTrue(filter.Match("lots of stuffing (omake)"));
            Assert.IsFalse(filter.Match("[x][Task] Some vote"));
        }

        [TestMethod]
        public void TestGlobFilter_Simple()
        {
            Filter filter = new Filter("stuff*", Quest.OmakeFilter);
            Assert.IsFalse(filter.Match(""));
            Assert.IsTrue(filter.Match("stuff"));
            Assert.IsTrue(filter.Match("lots of stuffing"));
            Assert.IsTrue(filter.Match("lots of stuffing (omake)"));
            Assert.IsFalse(filter.Match("lots of stufing day"));
            Assert.IsTrue(filter.Match("lots of stuffing2 day"));
            Assert.IsFalse(filter.Match("[x][Task] Some vote"));
        }

        [TestMethod]
        public void TestGlobFilter2_Simple()
        {
            Filter filter = new Filter("stuff* day", Quest.OmakeFilter);
            Assert.IsFalse(filter.Match(""));
            Assert.IsFalse(filter.Match("stuff"));
            Assert.IsFalse(filter.Match("lots of stuffing"));
            Assert.IsTrue(filter.Match("lots of stuffing (omake)"));
            Assert.IsTrue(filter.Match("lots of stuffing day"));
            Assert.IsTrue(filter.Match("lots of stuffing2 day"));
            Assert.IsFalse(filter.Match("lots of stuffing days"));
            Assert.IsFalse(filter.Match("lots of stuffing2 days"));
            Assert.IsFalse(filter.Match("[x][Task] Some vote"));
        }

        [TestMethod]
        public void TestParenFilter_Simple()
        {
            Filter filter = new Filter("(info)", Quest.OmakeFilter);
            Assert.IsFalse(filter.Match(""));
            Assert.IsFalse(filter.Match("stuff"));
            Assert.IsFalse(filter.Match("lots of stuffing"));
            Assert.IsTrue(filter.Match("lots of stuffing (omake)"));
            Assert.IsTrue(filter.Match("lots of stuffing day (info)"));
            Assert.IsFalse(filter.Match("lots of stuffing info"));
            Assert.IsFalse(filter.Match("[x][Task] Some vote"));
        }

        [TestMethod]
        public void TestInfoFilter_Simple()
        {
            Filter filter = new Filter("info", Quest.OmakeFilter);
            Assert.IsFalse(filter.Match(""));
            Assert.IsFalse(filter.Match("stuff"));
            Assert.IsTrue(filter.Match("lots of info"));
            Assert.IsFalse(filter.Match("lots of information"));
            Assert.IsTrue(filter.Match("lots of stuff (info)"));
            Assert.IsTrue(filter.Match("info: lots of stuffing"));
            Assert.IsTrue(filter.Match("[x][info] Some vote"));
            Assert.IsFalse(filter.Match("[x][inform] Some vote"));
        }

        [TestMethod]
        public void TestRegexFilter_Full()
        {
            Filter filter = new Filter("/stuff(ing)?/", Quest.OmakeFilter);
            Assert.IsFalse(filter.Match(""));
            Assert.IsTrue(filter.Match("stuff"));
            Assert.IsTrue(filter.Match("lots of stuffing"));
            Assert.IsTrue(filter.Match("lots of stuffing (omake)"));
            Assert.IsFalse(filter.Match("[x][Task] Some vote"));
        }
    }
}
