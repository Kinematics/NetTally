using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NetTally
{
    interface IQuest
    {
        string Name { get; set; }
        int StartPost { get; set; }
        int EndPost { get; set; }
    }
}
