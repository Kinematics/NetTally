using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using HtmlAgilityPack;

namespace NetTally.Adapters
{
    public static class ExtendHtmlNode
    {
        public static HtmlNode GetChildNodeWithClass(this HtmlNode node, string htmlClass, string childName = null)
        {
            if (string.IsNullOrEmpty(htmlClass))
                throw new ArgumentNullException(nameof(htmlClass));

            IEnumerable<HtmlNode> children;
            if (string.IsNullOrEmpty(childName))
                children = node.ChildNodes;
            else
                children = node.Elements(childName);

            return children.FirstOrDefault(n => n.GetAttributeValue("class", "").Split(' ').Contains(htmlClass));
        }
    }
}
