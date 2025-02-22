using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using HtmlAgilityPack;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NetTally.Enums;
using NetTally.Web;

namespace NetTally.Tests.Input
{
    [TestClass]
    //[Ignore]
    public class WebPageProviderTests
    {
        static IPageProvider? pageProvider;
        static IServiceProvider? serviceProvider;

        [ClassInitialize]
        public static void ClassInit(TestContext _)
        {
            serviceProvider = TestStartup.ConfigureServices();

            pageProvider = serviceProvider.GetRequiredService<IPageProvider>();
        }


        [TestMethod]
        public async Task LoadPage_DoesNotExist()
        {
            Assert.IsNotNull(pageProvider);

            var result = await pageProvider.GetHtmlDocumentAsync(
                "https://en.wikipedia.org/a",
                "Test nonexistent Wiki page",
                CachingMode.NoCache,
                SuppressNotifications.No,
                CancellationToken.None)
                .ConfigureAwait(ConfigureAwaitOptions.None);

            Assert.IsNull(result);
        }

        [TestMethod]
        public async Task LoadPage_Exists()
        {
            Assert.IsNotNull(pageProvider);

            var result = await pageProvider.GetHtmlDocumentAsync(
                "https://en.wikipedia.org/",
                "Test Wiki home page",
                CachingMode.NoCache,
                SuppressNotifications.No,
                CancellationToken.None)
                .ConfigureAwait(ConfigureAwaitOptions.None);

            Assert.IsNotNull(result);
        }

        [TestMethod]
        public async Task LoadPage_SV()
        {
            Assert.IsNotNull(pageProvider);

            var result = await pageProvider.GetHtmlDocumentAsync(
                "https://forums.sufficientvelocity.com/threads/fog-on-the-horizon-arpeggio-of-blue-steel-quest.13528/",
                "Test SV page",
                CachingMode.NoCache,
                SuppressNotifications.No,
                CancellationToken.None)
                .ConfigureAwait(ConfigureAwaitOptions.None);

            Assert.IsNotNull(result);
        }

        [TestMethod]
        public async Task LoadPage_QQ_SFW()
        {
            Assert.IsNotNull(pageProvider);

            var result = await pageProvider.GetHtmlDocumentAsync(
                "https://forum.questionablequesting.com/threads/tally-program-testing-thread.1698/",
                "Test QQ SFW page",
                CachingMode.NoCache,
                SuppressNotifications.No,
                CancellationToken.None)
                .ConfigureAwait(ConfigureAwaitOptions.None);

            Assert.IsNotNull(result);
        }

        [TestMethod]
        public async Task LoadPage_QQ_NSFW()
        {
            Assert.IsNotNull(pageProvider);

            var result = await pageProvider.GetHtmlDocumentAsync(
                "https://forum.questionablequesting.com/threads/my-pet-nekoshou-high-school-dxd-mahou-sensei-negima-quest.14022/",
                "Test QQ NSFW page",
                CachingMode.NoCache,
                SuppressNotifications.No,
                CancellationToken.None)
                .ConfigureAwait(ConfigureAwaitOptions.None);

            Assert.IsNotNull(result);
        }

        [TestMethod]
        public async Task LoadPage_SV_RSS()
        {
            Assert.IsNotNull(pageProvider);

            var result = await pageProvider.GetXmlDocumentAsync(
                "https://forums.sufficientvelocity.com/threads/fog-on-the-horizon-arpeggio-of-blue-steel-quest.13528/threadmarks.rss?category_id=1",
                "Test SV RSS page",
                CachingMode.NoCache,
                SuppressNotifications.No,
                CancellationToken.None)
                .ConfigureAwait(ConfigureAwaitOptions.None);

            Assert.IsNotNull(result);
        }

        [TestMethod]
        public async Task LoadPage_Github_API()
        {
            Assert.IsNotNull(pageProvider);

            var result = await pageProvider.GetJsonDocumentAsync(
                "https://api.github.com/repos/Kinematics/NetTally/releases",
                "Test Github API",
                CachingMode.NoCache,
                SuppressNotifications.No,
                CancellationToken.None)
                .ConfigureAwait(ConfigureAwaitOptions.None);

            Assert.IsNotNull(result);
        }

        [TestMethod]
        public async Task Load_Github_Redirect()
        {
            Assert.IsNotNull(pageProvider);

            var result = await pageProvider.GetRedirectUrlAsync(
                "https://github.com/Kinematics/NetTally/releases/latest",
                "Test Github API",
                SuppressNotifications.No,
                CancellationToken.None)
                .ConfigureAwait(ConfigureAwaitOptions.None);

            Assert.IsNotNull(result);
            Assert.IsTrue(Uri.IsWellFormedUriString(result, UriKind.Absolute));
            Assert.IsFalse(result.EndsWith("latest"));
        }

        [TestMethod]
        public async Task LoadPage_SV_6_Page()
        {
            Assert.IsNotNull(pageProvider);

            List<Task<HtmlDocument?>> documents = [];

            documents.Add(pageProvider.GetHtmlDocumentAsync(
                "https://forums.sufficientvelocity.com/threads/fog-on-the-horizon-arpeggio-of-blue-steel-quest.13528/",
                "Test SV pages, p1",
                CachingMode.NoCache, SuppressNotifications.No, CancellationToken.None));

            documents.Add(pageProvider.GetHtmlDocumentAsync(
                "https://forums.sufficientvelocity.com/threads/fog-on-the-horizon-arpeggio-of-blue-steel-quest.13528/page-2",
                "Test SV pages, p2",
                CachingMode.NoCache, SuppressNotifications.No, CancellationToken.None));

            documents.Add(pageProvider.GetHtmlDocumentAsync(
                "https://forums.sufficientvelocity.com/threads/fog-on-the-horizon-arpeggio-of-blue-steel-quest.13528/page-3",
                "Test SV pages, p3",
                CachingMode.NoCache, SuppressNotifications.No, CancellationToken.None));

            documents.Add(pageProvider.GetHtmlDocumentAsync(
                "https://forums.sufficientvelocity.com/threads/fog-on-the-horizon-arpeggio-of-blue-steel-quest.13528/page-4",
                "Test SV pages, p4",
                CachingMode.NoCache, SuppressNotifications.No, CancellationToken.None));

            documents.Add(pageProvider.GetHtmlDocumentAsync(
                "https://forums.sufficientvelocity.com/threads/fog-on-the-horizon-arpeggio-of-blue-steel-quest.13528/page-5",
                "Test SV pages, p5",
                CachingMode.NoCache, SuppressNotifications.No, CancellationToken.None));

            documents.Add(pageProvider.GetHtmlDocumentAsync(
                "https://forums.sufficientvelocity.com/threads/fog-on-the-horizon-arpeggio-of-blue-steel-quest.13528/page-6",
                "Test SV pages, p6",
                CachingMode.NoCache, SuppressNotifications.No, CancellationToken.None));

            var doneDocs = await Task.WhenAll(documents);

            Assert.IsNotNull(doneDocs);
            Assert.IsTrue(doneDocs.All(d => d is not null));
        }

        [TestMethod]
        public async Task LoadRedirect_UnescapeURL()
        {
            Assert.IsNotNull(pageProvider);
            string rawURL = "https://forum.questionablequesting.com/threads/the-rising-legends-anthro-pokémon-adventure.28932/";
            string escapedURL = "https://forum.questionablequesting.com/threads/the-rising-legends-anthro-pok%C3%A9mon-adventure.28932/";

            var result = await pageProvider.GetRedirectUrlAsync(
                rawURL,
                "Test Unescape URL",
                SuppressNotifications.No,
                CancellationToken.None)
                .ConfigureAwait(ConfigureAwaitOptions.None);

            Assert.AreNotEqual(rawURL, result);
            Assert.AreEqual(escapedURL, result);
        }

        [TestMethod]
        public async Task LoadHtml_UnescapeURL()
        {
            Assert.IsNotNull(pageProvider);
            string rawURL = "https://forum.questionablequesting.com/threads/the-rising-legends-anthro-pokémon-adventure.28932/";
            //string escapedURL = "https://forum.questionablequesting.com/threads/the-rising-legends-anthro-pok%C3%A9mon-adventure.28932/";

            var result = await pageProvider.GetHtmlDocumentAsync(
               rawURL,
               "Test Escaped HTML page",
               CachingMode.NoCache,
               SuppressNotifications.No,
               CancellationToken.None)
               .ConfigureAwait(ConfigureAwaitOptions.None);

            Assert.IsNotNull(result);
        }

    }
}