using System.IO;
using System.Threading.Tasks;
using HtmlAgilityPack;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NetTally.Forums;
using NetTally.Types.Enums;

namespace NetTally.Tests.Forums
{
    [TestClass]
    public class ForumIdentifierTests
    {
        [ClassInitialize]
        public static void ClassInit(TestContext context)
        {
        }

        [TestMethod]
        public async Task Identify_XenForo1()
        {
            HtmlDocument doc = new();
            string rawPage = await GetForumResource(ForumType.XenForo1);
            doc.LoadHtml(rawPage);

            ForumType forumType = ForumIdentifier.IdentifyForumTypeFromHtmlDocument(doc);

            Assert.AreEqual(ForumType.XenForo1, forumType);
        }

        [TestMethod]
        public async Task Identify_XenForo2()
        {
            HtmlDocument doc = new();
            string rawPage = await GetForumResource(ForumType.XenForo2);
            doc.LoadHtml(rawPage);

            ForumType forumType = ForumIdentifier.IdentifyForumTypeFromHtmlDocument(doc);

            Assert.AreEqual(ForumType.XenForo2, forumType);
        }

        [TestMethod]
        public async Task Identify_vBulletin3()
        {
            HtmlDocument doc = new();
            string rawPage = await GetForumResource(ForumType.vBulletin3);
            doc.LoadHtml(rawPage);

            ForumType forumType = ForumIdentifier.IdentifyForumTypeFromHtmlDocument(doc);

            Assert.AreEqual(ForumType.vBulletin3, forumType);
        }

        [TestMethod]
        public async Task Identify_vBulletin4()
        {
            HtmlDocument doc = new();
            string rawPage = await GetForumResource(ForumType.vBulletin4);
            doc.LoadHtml(rawPage);

            ForumType forumType = ForumIdentifier.IdentifyForumTypeFromHtmlDocument(doc);

            Assert.AreEqual(ForumType.vBulletin4, forumType);
        }

        [TestMethod]
        public async Task Identify_vBulletin5()
        {
            HtmlDocument doc = new();
            string rawPage = await GetForumResource(ForumType.vBulletin5);
            doc.LoadHtml(rawPage);

            ForumType forumType = ForumIdentifier.IdentifyForumTypeFromHtmlDocument(doc);

            Assert.AreEqual(ForumType.vBulletin5, forumType);
        }

        [Ignore]
        [TestMethod]
        public async Task Identify_NodeBB()
        {
            HtmlDocument doc = new();
            string rawPage = await GetForumResource(ForumType.NodeBB);
            doc.LoadHtml(rawPage);

            ForumType forumType = ForumIdentifier.IdentifyForumTypeFromHtmlDocument(doc);

            Assert.AreEqual(ForumType.NodeBB, forumType);
        }

        [TestMethod]
        public async Task Identify_phpBB()
        {
            HtmlDocument doc = new();
            string rawPage = await GetForumResource(ForumType.phpBB);
            doc.LoadHtml(rawPage);

            ForumType forumType = ForumIdentifier.IdentifyForumTypeFromHtmlDocument(doc);

            Assert.AreEqual(ForumType.phpBB, forumType);
        }


        private async Task<string> GetForumResource(ForumType forumType)
        {
            string filename = forumType switch
            {
                ForumType.vBulletin3 => "vBulletin3.html",
                ForumType.vBulletin4 => "vBulletin4.html",
                ForumType.vBulletin5 => "vBulletin5.html",
                ForumType.XenForo1 => "Xenforo1.html",
                ForumType.XenForo2 => "Xenforo2.html",
                ForumType.NodeBB => "NodeBB.html",
                ForumType.phpBB => "phpBB.html",
                _ => string.Empty,
            };

            if (string.IsNullOrEmpty(filename))
                return string.Empty;

            filename = Path.Combine("Resources", filename);

            return await LoadResource.Read(filename) ?? "";
        }
    }
}
