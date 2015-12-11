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
        /// <summary>
        /// Get the single HTML node result for searching for a direct child element
        /// that has the specified class attribute.
        /// </summary>
        /// <param name="node">The object the extension method is being used on.</param>
        /// <param name="element">The name of the element we're looking for.
        /// If it's null or empty, any element will work.</param>
        /// <param name="@class">The name of the class to search for.  Must be provided.</param>
        /// <returns>Returns the element with the specified class, if found.  Otherwise, null.</returns>
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

        /// <summary>
        /// Overloaded version to not require specifying the element name.
        /// </summary>
        /// <param name="node">The object the extension method is being used on.</param>
        /// <param name="@class">The name of the class to search for.  Must be provided.</param>
        /// <returns>Returns the element with the specified class, if found.  Otherwise, null.</returns>
        public static HtmlNode GetChildWithClass(this HtmlNode node, string @class) => node.GetChildWithClass(null, @class);

        /// <summary>
        /// Get the single HTML node result for searching for any descendant element
        /// that has the specified class attribute.
        /// </summary>
        /// <param name="node">The object the extension method is being used on.</param>
        /// <param name="element">The name of the element we're looking for.
        /// If it's null or empty, any element will work.</param>
        /// <param name="@class">The name of the class to search for.  Must be provided.</param>
        /// <returns>Returns the element with the specified class, if found.  Otherwise, null.</returns>
        public static HtmlNode GetDescendantWithClass(this HtmlNode node, string element, string @class)
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

        /// <summary>
        /// Overloaded version to not require specifying the element name.
        /// </summary>
        /// <param name="node">The object the extension method is being used on.</param>
        /// <param name="@class">The name of the class to search for.  Must be provided.</param>
        /// <returns>Returns the element with the specified class, if found.  Otherwise, null.</returns>
        public static HtmlNode GetDescendantWithClass(this HtmlNode node, string @class) => node.GetDescendantWithClass(null, @class);

    }
}
