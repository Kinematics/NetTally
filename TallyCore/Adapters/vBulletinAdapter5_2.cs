using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using HtmlAgilityPack;

namespace NetTally.Adapters
{
    public class vBulletinAdapter5_2 : IForumAdapter2
    {
        public int DefaultPostsPerPage => 20;

        public Uri Site
        {
            get
            {
                throw new NotImplementedException();
            }

            set
            {
                throw new NotImplementedException();
            }
        }

        public string GetPermalinkForId(string postId)
        {
            throw new NotImplementedException();
        }

        public IEnumerable<PostComponents> GetPosts(HtmlDocument page)
        {
            throw new NotImplementedException();
        }

        public Task<ThreadStartValue> GetStartingPostNumber(IQuest quest, IPageProvider pageProvider, CancellationToken token)
        {
            throw new NotImplementedException();
        }

        public ThreadInfo GetThreadInfo(HtmlDocument page)
        {
            throw new NotImplementedException();
        }

        public string GetUrlForPage(int page)
        {
            throw new NotImplementedException();
        }

        #region Detection
        /// <summary>
        /// Static detection of whether the provided web page is a XenForo forum thread.
        /// </summary>
        /// <param name="page">Web page to examine.</param>
        /// <returns>Returns true if it's detected as a XenForo page.  Otherwise, false.</returns>
        public static bool CanHandlePage(HtmlDocument page)
        {
            if (page == null)
                return false;

            return page.DocumentNode.Element("html")?.Element("body")?.Id == "vb-page-body";
        }
        #endregion

    }
}
