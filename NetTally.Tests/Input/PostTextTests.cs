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


        [TestMethod]
        public void Full_Plan_Strikethrough_1()
        {
            string html =
@"<span style=""text-decoration: line-through"">[X] Plan Triplemancer<br />
-[X] Aeromancy (40%)<br />
-[X] Hydromancy (30%)<br />
-[X] Pyromancy (30%)</span><br />
<br />
WeirGarth has minmaxed our Life pretty good, but we do need some extra options. The three elements will do nicely. Most focus on Aeromancy to maximize the chance of finishing the Rod and synergy with griffins. Hydromancy for Deep Den and Lannisport plus reconnecting with Bel. Pyromancy because Melisandre should have the most free time availalbe and all our fire and light imagery makes me think we have the most talent with it compared to other elements.<br />
<br />
Edit: Vote changed. See next page.";

            string expectedText =
@"❰[X] Plan Triplemancer⦂-[X] Aeromancy (40%)⦂-[X] Hydromancy (30%)⦂-[X] Pyromancy (30%)❱
WeirGarth has minmaxed our Life pretty good, but we do need some extra options. The three elements will do nicely. Most focus on Aeromancy to maximize the chance of finishing the Rod and synergy with griffins. Hydromancy for Deep Den and Lannisport plus reconnecting with Bel. Pyromancy because Melisandre should have the most free time availalbe and all our fire and light imagery makes me think we have the most talent with it compared to other elements.
Edit: Vote changed. See next page.";

            var node = GetHtmlFromString(html);

            if (node != null)
            {
                string results = ForumPostTextConverter.ExtractPostText(node, GetXenForoPredicate(), exampleUri);


                ReadOnlySpan<char> e = expectedText.AsSpan();
                ReadOnlySpan<char> r = results.AsSpan();

                Assert.AreEqual(expectedText, results);
            }

            Assert.IsTrue(node != null);
        }

        [TestMethod]
        public void Full_Plan_Strikethrough_2()
        {
            string html =
@"<span style=""text-decoration: line-through"">
[X] Plan Triplemancer<br />
-[X] Aeromancy (40%)<br />
-[X] Hydromancy (30%)<br />
-[X] Pyromancy (30%)</span><br />
<br />
WeirGarth has minmaxed our Life pretty good, but we do need some extra options. The three elements will do nicely. Most focus on Aeromancy to maximize the chance of finishing the Rod and synergy with griffins. Hydromancy for Deep Den and Lannisport plus reconnecting with Bel. Pyromancy because Melisandre should have the most free time availalbe and all our fire and light imagery makes me think we have the most talent with it compared to other elements.<br />
<br />
Edit: Vote changed. See next page.";

            string expectedText =
@"❰⦂[X] Plan Triplemancer⦂-[X] Aeromancy (40%)⦂-[X] Hydromancy (30%)⦂-[X] Pyromancy (30%)❱
WeirGarth has minmaxed our Life pretty good, but we do need some extra options. The three elements will do nicely. Most focus on Aeromancy to maximize the chance of finishing the Rod and synergy with griffins. Hydromancy for Deep Den and Lannisport plus reconnecting with Bel. Pyromancy because Melisandre should have the most free time availalbe and all our fire and light imagery makes me think we have the most talent with it compared to other elements.
Edit: Vote changed. See next page.";

            var node = GetHtmlFromString(html);

            if (node != null)
            {
                string results = ForumPostTextConverter.ExtractPostText(node, GetXenForoPredicate(), exampleUri);


                ReadOnlySpan<char> e = expectedText.AsSpan();
                ReadOnlySpan<char> r = results.AsSpan();

                Assert.AreEqual(expectedText, results);
            }

            Assert.IsTrue(node != null);
        }

        [TestMethod]
        public void Partial_Strikethrough()
        {
            string html =
@"<span style=""text-decoration: line-through"">
[X] Plan Triplemancer<br />
-[X] Aeromancy (40%)<br />
-[X] Hydromancy (30%)<br />
</span>[X] Pyromancy (30%)<br />
<br />
WeirGarth has minmaxed our Life pretty good, but we do need some extra options. The three elements will do nicely. Most focus on Aeromancy to maximize the chance of finishing the Rod and synergy with griffins. Hydromancy for Deep Den and Lannisport plus reconnecting with Bel. Pyromancy because Melisandre should have the most free time availalbe and all our fire and light imagery makes me think we have the most talent with it compared to other elements.<br />
<br />
Edit: Vote changed. See next page.";

            string expectedText =
@"❰⦂[X] Plan Triplemancer⦂-[X] Aeromancy (40%)⦂-[X] Hydromancy (30%)⦂❱[X] Pyromancy (30%)
WeirGarth has minmaxed our Life pretty good, but we do need some extra options. The three elements will do nicely. Most focus on Aeromancy to maximize the chance of finishing the Rod and synergy with griffins. Hydromancy for Deep Den and Lannisport plus reconnecting with Bel. Pyromancy because Melisandre should have the most free time availalbe and all our fire and light imagery makes me think we have the most talent with it compared to other elements.
Edit: Vote changed. See next page.";

            var node = GetHtmlFromString(html);

            if (node != null)
            {
                string results = ForumPostTextConverter.ExtractPostText(node, GetXenForoPredicate(), exampleUri);


                ReadOnlySpan<char> e = expectedText.AsSpan();
                ReadOnlySpan<char> r = results.AsSpan();

                Assert.AreEqual(expectedText, results);
            }

            Assert.IsTrue(node != null);
        }

        [TestMethod]
        public void Content_Strikethrough()
        {
            string html =
@"[X] Plan Air, <span style=""text-decoration: line-through"">Earth, Water,</span> Fire<br />
-[X] Aeromancy (40%)<br />
-[X] Pyromancy (25%)<br />
-[X] Pyromantic Divination (35%)<br />
<br />
We seem to have a real talent for divination considering that we managed to affect the past when we used the glass candle.";

            string expectedText =
@"[X] Plan Air, ❰Earth, Water,❱ Fire
-[X] Aeromancy (40%)
-[X] Pyromancy (25%)
-[X] Pyromantic Divination (35%)
We seem to have a real talent for divination considering that we managed to affect the past when we used the glass candle.";

            var node = GetHtmlFromString(html);

            if (node != null)
            {
                string results = ForumPostTextConverter.ExtractPostText(node, GetXenForoPredicate(), exampleUri);

                Assert.AreEqual(expectedText, results);
            }

            Assert.IsTrue(node != null);
        }


        [TestMethod]
        public void Strikethrough_Overlap()
        {
            string html =
@"[X] Plan Air, <span style=""text-decoration: line-through"">Earth, Water, Fire<br />
-[X]</span> Aeromancy (40%)<br />
-[X] Pyromancy (25%)<br />
-[X] Pyromantic Divination (35%)<br />
<br />
We seem to have a real talent for divination considering that we managed to affect the past when we used the glass candle.";

            string expectedText =
@"[X] Plan Air, ❰Earth, Water, Fire⦂-[X]❱ Aeromancy (40%)
-[X] Pyromancy (25%)
-[X] Pyromantic Divination (35%)
We seem to have a real talent for divination considering that we managed to affect the past when we used the glass candle.";

            var node = GetHtmlFromString(html);

            if (node != null)
            {
                string results = ForumPostTextConverter.ExtractPostText(node, GetXenForoPredicate(), exampleUri);

                Assert.AreEqual(expectedText, results);

                Origin origin = new Origin("Kinematics", "123456", 10, new Uri("http://www.example.com/"), "http://www.example.com");
                Post post = new Post(origin, results);

                Assert.IsTrue(post.HasVote);
                Assert.AreEqual(3, post.VoteLines.Count);
                Assert.AreEqual("[X] Plan Air,", post.VoteLines[0].ToString());
                Assert.AreEqual("-[X] Pyromancy (25%)", post.VoteLines[1].ToString());
                Assert.AreEqual("-[X] Pyromantic Divination (35%)", post.VoteLines[2].ToString());

            }

            Assert.IsTrue(node != null);
        }

        [TestMethod]
        public void Fonts_Test()
        {
            string html =
@"<span style=""font-family: 'times new roman'"">[x][Wine] Red<br />
-[x] Pinot Noir</span><br />
<br />
Font test";

            string expectedText =
@"[x][Wine] Red
-[x] Pinot Noir
Font test";

            var node = GetHtmlFromString(html);

            if (node != null)
            {
                string results = ForumPostTextConverter.ExtractPostText(node, GetXenForoPredicate(), exampleUri);

                Assert.AreEqual(expectedText, results);

                Origin origin = new Origin("Kinematics", "123456", 10, new Uri("http://www.example.com/"), "http://www.example.com");
                Post post = new Post(origin, results);

                Assert.IsTrue(post.HasVote);
                Assert.AreEqual(2, post.VoteLines.Count);
                Assert.AreEqual("[x][Wine] Red", post.VoteLines[0].ToString());
                Assert.AreEqual("-[x] Pinot Noir", post.VoteLines[1].ToString());
            }

            Assert.IsTrue(node != null);
        }

        [TestMethod]
        public void Check_Embedded_Callout()
        {
            string html =
@"The referenced post did not have the problem described, but another post did.  Basically, <br />
[x] <a href=""https://forums.sufficientvelocity.com/members/4076/"" class=""username"" data-xf-init=""member-tooltip"" data-user-id=""4076"" data-username=""@Kinematics"">@Kinematics</a> <br />
Wouldn't be applied to my proposed plan because it got turned into a member link (the '@' symbol is dropped on QQ's forums, so that doesn't interfere in this case).<br />
<br />";

            string expectedText =
@"The referenced post did not have the problem described, but another post did.  Basically, 
[x] 『url=""https://forums.sufficientvelocity.com/members/4076/""』@Kinematics『/url』 
Wouldn't be applied to my proposed plan because it got turned into a member link (the '@' symbol is dropped on QQ's forums, so that doesn't interfere in this case).";

            var node = GetHtmlFromString(html);

            if (node != null)
            {
                string results = ForumPostTextConverter.ExtractPostText(node, GetXenForoPredicate(), exampleUri);

                Assert.AreEqual(expectedText, results);

                Origin origin = new Origin("Kinematics1", "123456", 10, new Uri("http://www.example.com/"), "http://www.example.com");
                Post post = new Post(origin, results);

                Assert.IsTrue(post.HasVote);
                Assert.AreEqual(1, post.VoteLines.Count);
            }

            Assert.IsTrue(node != null);
        }


        [TestMethod]
        public void Check_Embedded_Callout_Plan()
        {
            string html =
@"The referenced post did not have the problem described, but another post did.  Basically, <br />
[x] Plan <a href=""https://forums.sufficientvelocity.com/members/4076/"" class=""username"" data-xf-init=""member-tooltip"" data-user-id=""4076"" data-username=""@Kinematics"">@Kinematics</a> <br />
Wouldn't be applied to my proposed plan because it got turned into a member link (the '@' symbol is dropped on QQ's forums, so that doesn't interfere in this case).<br />
<br />";

            string expectedText =
@"The referenced post did not have the problem described, but another post did.  Basically, 
[x] Plan 『url=""https://forums.sufficientvelocity.com/members/4076/""』@Kinematics『/url』 
Wouldn't be applied to my proposed plan because it got turned into a member link (the '@' symbol is dropped on QQ's forums, so that doesn't interfere in this case).";

            var node = GetHtmlFromString(html);

            if (node != null)
            {
                string results = ForumPostTextConverter.ExtractPostText(node, GetXenForoPredicate(), exampleUri);

                Assert.AreEqual(expectedText, results);

                Origin origin = new Origin("Kinematics1", "123456", 10, new Uri("http://www.example.com/"), "http://www.example.com");
                Post post = new Post(origin, results);

                Assert.IsTrue(post.HasVote);
                Assert.AreEqual(1, post.VoteLines.Count);
            }

            Assert.IsTrue(node != null);
        }

    }
}
