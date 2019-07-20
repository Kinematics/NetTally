using System;
using System.Collections.Generic;
using System.Text;
using HtmlAgilityPack;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NetTally.Forums;

namespace NetTally.Tests.Forums
{
    [TestClass]
    public class PostTextTests
    {
        #region Setup
        static IServiceProvider serviceProvider;
        static Uri exampleUri = new Uri("http://www.example.com/");

        [ClassInitialize]
        public static void ClassInit(TestContext context)
        {
            serviceProvider = TestStartup.ConfigureServices();
        }

        [TestInitialize]
        public void Initialize()
        {
        }
        #endregion

        private HtmlNode GetHtmlFromString(string input)
        {
            HtmlDocument doc = new HtmlDocument();
            doc.LoadHtml(input);
            return doc.DocumentNode;
        }

        private Predicate<HtmlNode> GetXenForoPredicate()
        {
            List<string> excludedClasses = new List<string> { "bbCodeQuote", "messageTextEndMarker","advbbcodebar_encadre",
                "advbbcodebar_article", "adv_tabs_wrapper", "adv_slider_wrapper"};

            return ForumPostTextConverter.GetClassesExclusionPredicate(excludedClasses);
        }

        [TestMethod]
        public void Test_Load_Basic_HTML()
        {
            string html = @"[X] Plan Perfectly Balanced (As all things should be)<br />
-[X] Life (50%)<br />
-[X] Aeromancy (50%)<br />
<br />
I think Garth&#039;s entire thing so far is life, so he should try &quot;max&quot; that out as <i>quickly</i> as possible. On the other hand, some versatility is required, and he has already started down the Aeromancy path.<br />";

            var node = GetHtmlFromString(html);

            if (node != null)
            {
                var italic = node.Element("i");
                Assert.IsTrue(italic != null);
            }

            Assert.IsTrue(node != null);
        }

        [TestMethod]
        public void Convert_Basic_HTML()
        {
            string html = @"[X] Plan Perfectly Balanced (As all things should be)<br />
-[X] Life (50%)<br />
-[X] Aeromancy (50%)<br />
<br />
I think Garth&#039;s entire thing so far is life, so he should try &quot;max&quot; that out as <i>quickly</i> as possible. On the other hand, some versatility is required, and he has already started down the Aeromancy path.<br />";

            string expectedText =
@"[X] Plan Perfectly Balanced (As all things should be)
-[X] Life (50%)
-[X] Aeromancy (50%)
I think Garth's entire thing so far is life, so he should try ""max"" that out as 『i』quickly『/i』 as possible. On the other hand, some versatility is required, and he has already started down the Aeromancy path.";

            var node = GetHtmlFromString(html);

            if (node != null)
            {
                string results = ForumPostTextConverter.ExtractPostText(node, GetXenForoPredicate(), exampleUri);

                Assert.AreEqual(expectedText, results);
            }

            Assert.IsTrue(node != null);

        }

    }
}
