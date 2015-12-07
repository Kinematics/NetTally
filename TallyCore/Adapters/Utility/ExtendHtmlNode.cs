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
        public static HtmlNode GetChildWithClass(this HtmlNode node, string element, string @class)
        {
            if (string.IsNullOrEmpty(@class))
                throw new ArgumentNullException(nameof(@class));

            IEnumerable<HtmlNode> children;
            if (string.IsNullOrEmpty(element))
                children = node.ChildNodes;
            else
                children = node.Elements(element);

            return children.FirstOrDefault(n => n.GetAttributeValue("class", "").Split(' ').Contains(@class));
        }

        public static HtmlNode GetDescendantNodeWithClass(this HtmlNode node, string element, string @class)
        {
            if (string.IsNullOrEmpty(@class))
                throw new ArgumentNullException(nameof(@class));

            IEnumerable<HtmlNode> children;
            if (string.IsNullOrEmpty(element))
                children = node.Descendants();
            else
                children = node.Descendants(element);

            return children.FirstOrDefault(n => n.GetAttributeValue("class", "").Split(' ').Contains(@class));
        }
    }
}
