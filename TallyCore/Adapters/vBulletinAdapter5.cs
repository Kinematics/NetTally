using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using HtmlAgilityPack;

namespace NetTally.Adapters
{
    public class vBulletinAdapter5 : IForumAdapter
    {
        public vBulletinAdapter5()
        {

        }

        public vBulletinAdapter5(string site)
        {
            ForumUrl = site;
            ThreadsUrl = site;
            PostsUrl = site;
        }

        protected virtual string ForumUrl { get; }
        protected virtual string ThreadsUrl { get; }
        protected virtual string PostsUrl { get; }

        public string GetThreadsUrl(string questTitle)
        {
            throw new NotImplementedException();
        }

        public string GetThreadPageBaseUrl(string questTitle)
        {
            throw new NotImplementedException();
        }

        public string GetThreadmarksPageUrl(string questTitle)
        {
            throw new NotImplementedException();
        }

        public string GetPageUrl(string questTitle, int page)
        {
            throw new NotImplementedException();
        }

        public string GetPostUrlFromId(string postId)
        {
            throw new NotImplementedException();
        }

        public string GetUrlFromRelativeAddress(string relative)
        {
            throw new NotImplementedException();
        }

        public bool IsValidThreadName(string name)
        {
            throw new NotImplementedException();
        }

        public Task<int> GetStartingPostNumber(IPageProvider pageProvider, IQuest quest, CancellationToken token)
        {
            throw new NotImplementedException();
        }

        public int GetPageNumberFromPostNumber(int postNumber)
        {
            throw new NotImplementedException();
        }

        public int GetLastPageNumberOfThread(HtmlDocument page)
        {
            throw new NotImplementedException();
        }

        public string GetAuthorOfThread(HtmlDocument page)
        {
            throw new NotImplementedException();
        }

        public IEnumerable<HtmlNode> GetPostsFromPage(HtmlDocument page)
        {
            throw new NotImplementedException();
        }

        public string GetIdOfPost(HtmlNode post)
        {
            throw new NotImplementedException();
        }

        public int GetPostNumberOfPost(HtmlNode post)
        {
            throw new NotImplementedException();
        }

        public string GetAuthorOfPost(HtmlNode post)
        {
            throw new NotImplementedException();
        }

        public string GetTextOfPost(HtmlNode post)
        {
            throw new NotImplementedException();
        }

        public string GetPageTitle(HtmlDocument page)
        {
            throw new NotImplementedException();
        }
    }
}
