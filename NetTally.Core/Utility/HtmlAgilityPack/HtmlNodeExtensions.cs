using System;
using System.Collections.Generic;
using System.Linq;
using HtmlAgilityPack;

namespace NetTally.Extensions
{
    /// <summary>
    /// Extension methods for HtmlNode objects.
    /// </summary>
    static class HtmlNodeEx
    {
        static readonly char[] classSeparators = new[] { ' ' };
        
        /// <summary>
        /// Determines whether the specified HTML node has the named class.
        /// Allows for multiple classes in the class attribute, separated by spaces.
        /// </summary>
        /// <param name="node">The HTML node.</param>
        /// <param name="class">The class name to check for.</param>
        /// <returns>
        ///   <c>true</c> if the node has the specified class; otherwise, <c>false</c>.
        /// </returns>
        /// <exception cref="System.ArgumentNullException">
        /// node
        /// or
        /// class
        /// </exception>
        [Obsolete("HAP library has implementation of this function.")]
        public static bool HasClass(this HtmlNode node, string @class)
        {
            if (string.IsNullOrEmpty(@class))
                throw new ArgumentNullException(nameof(@class));

            return node.GetAttributeValue("class", "").Split(classSeparators, StringSplitOptions.RemoveEmptyEntries).Contains(@class);
        }

        /// <summary>
        /// Get the single HTML node result for searching for a direct child element
        /// that has the specified class attribute.
        /// </summary>
        /// <param name="node">The object the extension method is being used on.</param>
        /// <param name="element">The name of the element we're looking for.
        /// If it's null or empty, any element will work.</param>
        /// <param name="@class">The name of the class to search for.  If this is null or empty,
        /// searches for elements without any classes at all.</param>
        /// <returns>Returns the element with the specified class, if found.  Otherwise, null.</returns>
        public static HtmlNode GetChildWithClass(this HtmlNode node, string element, string @class)
        {
            IEnumerable<HtmlNode> children;
            if (string.IsNullOrEmpty(element))
                children = node.ChildNodes;
            else
                children = node.Elements(element);

            HtmlNode find;

            if (string.IsNullOrEmpty(@class))
            {
                find = children.FirstOrDefault(n => string.IsNullOrEmpty(n.GetAttributeValue("class", "").Trim()));
            }
            else
            {
                find = children.FirstOrDefault(n => n.HasClass(@class));
            }

            return find;
        }

        /// <summary>
        /// Overloaded version to not require specifying the element name.
        /// </summary>
        /// <param name="node">The object the extension method is being used on.</param>
        /// <param name="@class">The name of the class to search for. If this is null or empty,
        /// searches for elements without any classes at all.</param>
        /// <returns>Returns the element with the specified class, if found.  Otherwise, null.</returns>
        public static HtmlNode GetChildWithClass(this HtmlNode node, string @class) => node.GetChildWithClass(null, @class);

        /// <summary>
        /// Gets all HTML child nodes matching an optional element type and specified class.
        /// </summary>
        /// <param name="node">The object the extension method is being used on.</param>
        /// <param name="element">The name of the element we're looking for.
        /// If it's null or empty, any element will work.</param>
        /// <param name="@class">The name of the class to search for.  If this is null or empty,
        /// searches for elements without any classes at all.</param>
        /// <returns>Returns a list with all found elements.</returns>
        public static IEnumerable<HtmlNode> GetChildrenWithClass(this HtmlNode node, string element, string @class)
        {
            IEnumerable<HtmlNode> children;
            if (string.IsNullOrEmpty(element))
                children = node.ChildNodes;
            else
                children = node.Elements(element);

            IEnumerable<HtmlNode> find;

            if (string.IsNullOrEmpty(@class))
            {
                find = children.Where(n => string.IsNullOrEmpty(n.GetAttributeValue("class", "").Trim()));
            }
            else
            {
                find = children.Where(n => n.HasClass(@class));
            }

            return find;
        }

        /// <summary>
        /// Overloaded version to not require specifying the element name.
        /// </summary>
        /// <param name="node">The object the extension method is being used on.</param>
        /// <param name="@class">The name of the class to search for.  If this is null or empty,
        /// searches for elements without any classes at all.</param>
        /// <returns>Returns a list with all found elements.</returns>
        public static IEnumerable<HtmlNode> GetChildrenWithClass(this HtmlNode node, string @class) => node.GetChildrenWithClass(null, @class);

        /// <summary>
        /// Get the single HTML node result for searching for any descendant element
        /// that has the specified class attribute.
        /// </summary>
        /// <param name="node">The object the extension method is being used on.</param>
        /// <param name="element">The name of the element we're looking for.
        /// If it's null or empty, any element will work.</param>
        /// <param name="@class">The name of the class to search for.  If this is null or empty,
        /// searches for elements without any classes at all.</param>
        /// <returns>Returns the element with the specified class, if found.  Otherwise, null.</returns>
        public static HtmlNode GetDescendantWithClass(this HtmlNode node, string element, string @class)
        {
            IEnumerable<HtmlNode> children;
            if (string.IsNullOrEmpty(element))
                children = node.Descendants();
            else
                children = node.Descendants(element);

            HtmlNode find;

            if (string.IsNullOrEmpty(@class))
            {
                find = children.FirstOrDefault(n => string.IsNullOrEmpty(n.GetAttributeValue("class", "").Trim()));
            }
            else
            {
                find = children.FirstOrDefault(n => n.HasClass(@class));
            }

            return find;
        }

        /// <summary>
        /// Overloaded version to not require specifying the element name.
        /// </summary>
        /// <param name="node">The object the extension method is being used on.</param>
        /// <param name="@class">The name of the class to search for.  If this is null or empty,
        /// searches for elements without any classes at all.</param>
        /// <returns>Returns the element with the specified class, if found.  Otherwise, null.</returns>
        public static HtmlNode GetDescendantWithClass(this HtmlNode node, string @class) => node.GetDescendantWithClass(null, @class);

        /// <summary>
        /// Gets all descendent nodes matching an optional element type and specified class.
        /// </summary>
        /// <param name="node">The object the extension method is being used on.</param>
        /// <param name="element">The name of the element we're looking for.
        /// If it's null or empty, any element will work.</param>
        /// <param name="@class">The name of the class to search for.  Must be provided.</param>
        /// <returns>Returns a list with all found elements.</returns>
        /// <exception cref="ArgumentNullException">If @class is null or empty.</exception>
        public static IEnumerable<HtmlNode> GetDescendantsWithClass(this HtmlNode node, string element, string @class)
        {
            IEnumerable<HtmlNode> children;
            if (string.IsNullOrEmpty(element))
                children = node.Descendants();
            else
                children = node.Descendants(element);

            IEnumerable<HtmlNode> find;

            if (string.IsNullOrEmpty(@class))
            {
                find = children.Where(n => string.IsNullOrEmpty(n.GetAttributeValue("class", "").Trim()));
            }
            else
            {
                find = children.Where(n => n.HasClass(@class));
            }

            return find;
        }

        /// <summary>
        /// Overloaded version to not require specifying the element name.
        /// </summary>
        /// <param name="node">The object the extension method is being used on.</param>
        /// <param name="@class">The name of the class to search for.  If this is null or empty,
        /// searches for elements without any classes at all.</param>
        /// <returns>Returns a list with all found elements.</returns>
        public static IEnumerable<HtmlNode> GetDescendantsWithClass(this HtmlNode node, string @class) => node.GetDescendantsWithClass(null, @class);

    }
}
