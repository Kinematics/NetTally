using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NetTally.Options;

namespace NetTally.Global
{
    public class ConfigInfoWrapper
    {
        public ConfigInfo ConfigInfo { get; set; } = new();
    }

    public class ConfigInfo
    {
        public ConfigInfo() 
        {
            Quests = new();
        }

        public ConfigInfo(IEnumerable<Quest> quests)
        {
            Quests = quests.ToList();
        }

        public List<Quest> Quests { get; set; }
    }
}
