using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using HtmlAgilityPack;

namespace NetTally.Adapters
{
    /// <summary>
    /// Class for extracting data from Sufficient Velocity forums.
    /// </summary>
    public class SufficientVelocityAdapter : XenForoAdapter
    {
        override protected string ForumUrl { get; } = "http://forums.sufficientvelocity.com/";
        override protected string ThreadsUrl { get; } = "http://forums.sufficientvelocity.com/threads/";
        override protected string PostsUrl { get; } = "http://forums.sufficientvelocity.com/posts/";

        public override int GetPostsPerPage()
        {
            return 25;
        }
    }
}
