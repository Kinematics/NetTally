using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using HtmlAgilityPack;

namespace NetTally.Adapters
{
    public class vBulletinAdapter4_2 : IForumAdapter2
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
    }
}
