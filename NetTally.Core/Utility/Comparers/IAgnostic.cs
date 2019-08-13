using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Text;
using NetTally.Comparers;

namespace NetTally.Utility.Comparers
{
    public interface IAgnostic
    {
        void ComparisonPropertyChanged(IQuest quest, PropertyChangedEventArgs e);
    }
}
