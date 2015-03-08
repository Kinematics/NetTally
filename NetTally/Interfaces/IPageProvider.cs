using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using HtmlAgilityPack;

namespace NetTally
{
    public interface IPageProvider
    {
        List<HtmlDocument> LoadPages(string questTitle, int startPost, int endPost);
        Task<List<HtmlDocument>> LoadPagesAsync(string questTitle, int startPost, int endPost);

        void ClearPageCache();

        event EventHandler<MessageEventArgs> StatusChanged;
    }
}
