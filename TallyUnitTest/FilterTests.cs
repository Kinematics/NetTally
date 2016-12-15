using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NetTally.Filters;

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
        public void TestBlankFilter()
        {
            Filter filter = new Filter("", null);
            Assert.IsTrue(filter.Match(""));
            Assert.IsFalse(filter.Match("stuff"));
            Assert.IsFalse(filter.Match("lots of stuff (omake)"));
            Assert.IsFalse(filter.Match("[x][Task] Some vote"));
        }

        [TestMethod]
        public void TestDefaultOmakeFilter()
        {
            Filter filter = new Filter("", "omake");
            Assert.IsFalse(filter.Match(""));
            Assert.IsFalse(filter.Match("stuff"));
            Assert.IsTrue(filter.Match("lots of stuff (omake)"));
            Assert.IsFalse(filter.Match("[x][Task] Some vote"));
        }

        [TestMethod]
        public void TestOmakeStuffFilter()
        {
            Filter filter = new Filter("stuff", "omake");
            Assert.IsFalse(filter.Match(""));
            Assert.IsTrue(filter.Match("stuff"));
            Assert.IsTrue(filter.Match("lots of stuffing (omake)"));
            Assert.IsFalse(filter.Match("[x][Task] Some vote"));
        }

        [TestMethod]
        public void TestTaskFilter()
        {
            Filter filter = new Filter("My Task", Filter.EmptyLine);
            Assert.IsFalse(filter.Match("Task"));
            Assert.IsTrue(filter.Match("My Task"));
            Assert.IsFalse(filter.Match(""));
            Assert.IsFalse(filter.Match("Fountain"));
        }

        [TestMethod]
        public void TestMultiValueFilter()
        {
            Filter filter = new Filter("stuff, stuffing", "omake");
            Assert.IsFalse(filter.Match(""));
            Assert.IsTrue(filter.Match("stuff"));
            Assert.IsTrue(filter.Match("lots of stuffing"));
            Assert.IsTrue(filter.Match("lots of stuffing (omake)"));
            Assert.IsFalse(filter.Match("[x][Task] Some vote"));
        }
    }
}
