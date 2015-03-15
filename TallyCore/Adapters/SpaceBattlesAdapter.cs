using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NetTally.Adapters
{
    public class SpaceBattlesAdapter : XenForoAdapter
    {
        override protected string ForumUrl { get; } = "http://forums.spacebattles.com/";
        override protected string ThreadsUrl { get; } = "http://forums.spacebattles.com/threads/";
        override protected string PostsUrl { get; } = "http://forums.spacebattles.com/posts/";

    }
}
