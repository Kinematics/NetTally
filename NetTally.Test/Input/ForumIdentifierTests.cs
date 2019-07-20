using System.Threading.Tasks;
using HtmlAgilityPack;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NetTally.Forums;

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
            HtmlDocument doc = new HtmlDocument();
            string rawPage = await GetForumResource(ForumType.XenForo1);
            doc.LoadHtml(rawPage);

            ForumType forumType = ForumIdentifier.IdentifyForumTypeFromHtmlDocument(doc);

            Assert.AreEqual(ForumType.XenForo1, forumType);
        }

        [TestMethod]
        public async Task Identify_XenForo2()
        {
            HtmlDocument doc = new HtmlDocument();
            string rawPage = await GetForumResource(ForumType.XenForo2);
            doc.LoadHtml(rawPage);

            ForumType forumType = ForumIdentifier.IdentifyForumTypeFromHtmlDocument(doc);

            Assert.AreEqual(ForumType.XenForo2, forumType);
        }

        [TestMethod]
        public async Task Identify_vBulletin3()
        {
            HtmlDocument doc = new HtmlDocument();
            string rawPage = await GetForumResource(ForumType.vBulletin3);
            doc.LoadHtml(rawPage);

            ForumType forumType = ForumIdentifier.IdentifyForumTypeFromHtmlDocument(doc);

            Assert.AreEqual(ForumType.vBulletin3, forumType);
        }

        [TestMethod]
        public async Task Identify_vBulletin4()
        {
            HtmlDocument doc = new HtmlDocument();
            string rawPage = await GetForumResource(ForumType.vBulletin4);
            doc.LoadHtml(rawPage);

            ForumType forumType = ForumIdentifier.IdentifyForumTypeFromHtmlDocument(doc);

            Assert.AreEqual(ForumType.vBulletin4, forumType);
        }

        [TestMethod]
        public async Task Identify_vBulletin5()
        {
            HtmlDocument doc = new HtmlDocument();
            string rawPage = await GetForumResource(ForumType.vBulletin5);
            doc.LoadHtml(rawPage);

            ForumType forumType = ForumIdentifier.IdentifyForumTypeFromHtmlDocument(doc);

            Assert.AreEqual(ForumType.vBulletin5, forumType);
        }

        [Ignore]
        [TestMethod]
        public async Task Identify_NodeBB()
        {
            HtmlDocument doc = new HtmlDocument();
            string rawPage = await GetForumResource(ForumType.NodeBB);
            doc.LoadHtml(rawPage);

            ForumType forumType = ForumIdentifier.IdentifyForumTypeFromHtmlDocument(doc);

            Assert.AreEqual(ForumType.NodeBB, forumType);
        }

        [TestMethod]
        public async Task Identify_phpBB()
        {
            HtmlDocument doc = new HtmlDocument();
            string rawPage = await GetForumResource(ForumType.phpBB);
            doc.LoadHtml(rawPage);

            ForumType forumType = ForumIdentifier.IdentifyForumTypeFromHtmlDocument(doc);

            Assert.AreEqual(ForumType.phpBB, forumType);
        }


        private async Task<string> GetForumResource(ForumType forumType)
        {
            string filename;

            switch (forumType)
            {
                case ForumType.vBulletin3:
                    filename = "vBulletin3.html";
                    break;
                case ForumType.vBulletin4:
                    filename = "vBulletin4.html";
                    break;
                case ForumType.vBulletin5:
                    filename = "vBulletin5.html";
                    break;
                case ForumType.XenForo1:
                    filename = "Xenforo1.html";
                    break;
                case ForumType.XenForo2:
                    filename = "Xenforo2.html";
                    break;
                case ForumType.NodeBB:
                    filename = "NodeBB.html";
                    break;
                case ForumType.phpBB:
                    filename = "phpBB.html";
                    break;
                default:
                    filename = "";
                    break;
            }

            if (string.IsNullOrEmpty(filename))
                return string.Empty;

            filename = $"Resources/{filename}";

            return await LoadResource.Read(filename) ?? "";
        }
    }
}
