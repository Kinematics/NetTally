using System.Text.RegularExpressions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NetTally.Input.Utility;

namespace NetTally.Tests.Utility
{
    [TestClass]
    public class FilterTests
    {
        #region Empty and Null
        [TestMethod]
        public void EmptyFilter_default()
        {
            var filter = Filter.Empty;
            Assert.IsTrue(filter.IsEmpty);
            Assert.IsFalse(filter.IsAlwaysFalse);
            Assert.IsFalse(filter.IsInverted);
            Assert.IsTrue(filter.Match(""));
            Assert.IsFalse(filter.Match("stuff"));
            Assert.IsFalse(filter.Match("lots of stuff (omake)"));
            Assert.IsFalse(filter.Match("[x][Task] Some vote"));
        }

        [TestMethod]
        public void EmptyFilter_implied_string_empty()
        {
            var filter = new Filter("", null);
            Assert.IsTrue(filter.IsEmpty);
            Assert.IsFalse(filter.IsAlwaysFalse);
            Assert.IsFalse(filter.IsInverted);
            Assert.IsTrue(filter.Match(""));
            Assert.IsFalse(filter.Match("stuff"));
            Assert.IsFalse(filter.Match("lots of stuff (omake)"));
            Assert.IsFalse(filter.Match("[x][Task] Some vote"));
        }

        [TestMethod]
        public void EmptyFilter_implied_string_null()
        {
            var filter = new Filter(null, null);
            Assert.IsTrue(filter.IsEmpty);
            Assert.IsFalse(filter.IsAlwaysFalse);
            Assert.IsFalse(filter.IsInverted);
            Assert.IsTrue(filter.Match(""));
            Assert.IsFalse(filter.Match("stuff"));
            Assert.IsFalse(filter.Match("lots of stuff (omake)"));
            Assert.IsFalse(filter.Match("[x][Task] Some vote"));
        }

        [TestMethod]
        public void EmptyString_inject()
        {
            var filter = new Filter("", "omake");
            Assert.IsFalse(filter.IsEmpty);
            Assert.IsFalse(filter.IsAlwaysFalse);
            Assert.IsFalse(filter.IsInverted);
            Assert.IsFalse(filter.Match(""));
            Assert.IsFalse(filter.Match("stuff"));
            Assert.IsTrue(filter.Match("lots of stuff (omake)"));
            Assert.IsFalse(filter.Match("[x][Task] Some vote"));
        }

        [TestMethod]
        public void NullString_inject()
        {
            var filter = new Filter(null, "omake");
            Assert.IsFalse(filter.IsEmpty);
            Assert.IsFalse(filter.IsAlwaysFalse);
            Assert.IsFalse(filter.IsInverted);
            Assert.IsFalse(filter.Match(""));
            Assert.IsFalse(filter.Match("stuff"));
            Assert.IsTrue(filter.Match("lots of stuff (omake)"));
            Assert.IsFalse(filter.Match("[x][Task] Some vote"));
        }

        [TestMethod]
        public void NullFilter()
        {
            var filter = new Filter(null);
            Assert.IsFalse(filter.IsEmpty);
            Assert.IsTrue(filter.IsAlwaysFalse);
            Assert.IsFalse(filter.IsInverted);
            Assert.IsFalse(filter.Match(""));
            Assert.IsFalse(filter.Match("stuff"));
            Assert.IsFalse(filter.Match("lots of stuff (omake)"));
            Assert.IsFalse(filter.Match("[x][Task] Some vote"));
        }
        #endregion

        #region Regex
        [TestMethod]
        public void Regex_explicit()
        {
            Regex r = new Regex(@"stuff|omake");
            Filter filter = new Filter(r);
            Assert.IsFalse(filter.IsEmpty);
            Assert.IsFalse(filter.IsAlwaysFalse);
            Assert.IsFalse(filter.Match(""));
            Assert.IsTrue(filter.Match("stuff"));
            Assert.IsTrue(filter.Match("lots of stuffing"));
            Assert.IsTrue(filter.Match("lots of stuffing (omake)"));
            Assert.IsFalse(filter.Match("[x][Task] Some vote"));
        }

        [TestMethod]
        public void Regex_implicit()
        {
            Filter filter = new Filter("/stuff|omake/", null);
            Assert.IsFalse(filter.IsEmpty);
            Assert.IsFalse(filter.IsAlwaysFalse);
            Assert.IsFalse(filter.Match(""));
            Assert.IsTrue(filter.Match("stuff"));
            Assert.IsTrue(filter.Match("lots of stuffing"));
            Assert.IsTrue(filter.Match("lots of stuffing (omake)"));
            Assert.IsFalse(filter.Match("[x][Task] Some vote"));
        }
        #endregion

        [TestMethod]
        public void DefaultOmakeFilter_Simple()
        {
            Filter filter = new Filter("", Quest.OmakeFilter);
            Assert.IsFalse(filter.Match(""));
            Assert.IsFalse(filter.Match("stuff"));
            Assert.IsTrue(filter.Match("lots of stuff (omake)"));
            Assert.IsFalse(filter.Match("[x][Task] Some vote"));
        }

        [TestMethod]
        public void DefaultOmakeFilter_Full()
        {
            Filter filter = new Filter("", Quest.OmakeFilter);
            Assert.IsFalse(filter.Match(""));
            Assert.IsFalse(filter.Match("stuff"));
            Assert.IsTrue(filter.Match("lots of stuff (omake)"));
            Assert.IsFalse(filter.Match("[x][Task] Some vote"));
        }

        [TestMethod]
        public void OmakeStuffFilter_Simple()
        {
            Filter filter = new Filter("stuff", Quest.OmakeFilter);
            Assert.IsFalse(filter.Match(""));
            Assert.IsTrue(filter.Match("stuff"));
            Assert.IsTrue(filter.Match("lots of stuffing (omake)"));
            Assert.IsFalse(filter.Match("[x][Task] Some vote"));
        }

        [TestMethod]
        public void OmakeStuffFilter_Full()
        {
            Filter filter = new Filter("/stuff/", Quest.OmakeFilter);
            Assert.IsFalse(filter.Match(""));
            Assert.IsTrue(filter.Match("stuff"));
            Assert.IsTrue(filter.Match("lots of stuffing (omake)"));
            Assert.IsFalse(filter.Match("[x][Task] Some vote"));
        }

        [TestMethod]
        public void MultiValueFilter_Simple()
        {
            Filter filter = new Filter("stuff, stuffing", Quest.OmakeFilter);
            Assert.IsFalse(filter.Match(""));
            Assert.IsTrue(filter.Match("stuff"));
            Assert.IsTrue(filter.Match("lots of stuffing"));
            Assert.IsTrue(filter.Match("lots of stuffing (omake)"));
            Assert.IsFalse(filter.Match("[x][Task] Some vote"));
        }

        [TestMethod]
        public void MultiValueFilter_Full()
        {
            Filter filter = new Filter("/stuff|stuffing/", Quest.OmakeFilter);
            Assert.IsFalse(filter.Match(""));
            Assert.IsTrue(filter.Match("stuff"));
            Assert.IsTrue(filter.Match("lots of stuffing"));
            Assert.IsTrue(filter.Match("lots of stuffing (omake)"));
            Assert.IsFalse(filter.Match("[x][Task] Some vote"));
        }

        [TestMethod]
        public void GlobFilter_Simple()
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
        public void GlobFilter2_Simple()
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
        public void ParenFilter_Simple()
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
        public void InfoFilter_Simple()
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
        public void RegexFilter_Full()
        {
            Filter filter = new Filter("/stuff(ing)?/", Quest.OmakeFilter);
            Assert.IsFalse(filter.Match(""));
            Assert.IsTrue(filter.Match("stuff"));
            Assert.IsTrue(filter.Match("lots of stuffing"));
            Assert.IsTrue(filter.Match("lots of stuffing (omake)"));
            Assert.IsFalse(filter.Match("[x][Task] Some vote"));
        }

        [TestMethod]
        public void MultiValueFilter_Simple_inverted()
        {
            Filter filter = new Filter("!stuff, stuffing", Quest.OmakeFilter);
            Assert.IsTrue(filter.IsInverted);
            Assert.IsTrue(filter.Match(""));
            Assert.IsFalse(filter.Match("stuff"));
            Assert.IsFalse(filter.Match("lots of stuffing"));
            Assert.IsFalse(filter.Match("lots of stuffing (omake)"));
            Assert.IsTrue(filter.Match("[x][Task] Some vote"));
        }

        [TestMethod]
        public void MultiValueFilter_Full_inverted()
        {
            Filter filter = new Filter("!/stuff|stuffing/", Quest.OmakeFilter);
            Assert.IsTrue(filter.IsInverted);
            Assert.IsTrue(filter.Match(""));
            Assert.IsFalse(filter.Match("stuff"));
            Assert.IsFalse(filter.Match("lots of stuffing"));
            Assert.IsFalse(filter.Match("lots of stuffing (omake)"));
            Assert.IsTrue(filter.Match("[x][Task] Some vote"));
        }


    }
}
