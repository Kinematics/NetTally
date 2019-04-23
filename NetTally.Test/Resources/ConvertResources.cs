using System.Text;
using System.Threading.Tasks;
using HtmlAgilityPack;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NetTally;
using NetTally.Forums;
using NetTally.Forums.Adapters;

namespace NTTests.Resources
{
    [TestClass]
    public class ConvertResources
    {
        [TestMethod]
        [Ignore]
        public async Task ConvertResourcePosts()
        {
            var resourceContent = await LoadResource.Read("Resources/RenascenceSV.html");
            HtmlDocument doc = new HtmlDocument();
            doc.LoadHtml(resourceContent);
            var forumType = ForumIdentifier.IdentifyForumTypeFromHtmlDocument(doc);
            var adapter = ForumAdapterSelector.GetForumAdapter(forumType);
            Assert.IsInstanceOfType(adapter, typeof(XenForo1Adapter));

            Quest quest = new Quest
            {
                DisplayName = "Convert Quest"
            };

            var posts = adapter.GetPosts(doc, quest);

            StringBuilder sb = new StringBuilder();

            foreach (var post in posts)
            {
                sb.AppendLine(post.Text);
                sb.AppendLine("~!~");
            }

            await LoadResource.Write("Resources/RenascenceSV.txt", sb.ToString());
        }
    }
}
