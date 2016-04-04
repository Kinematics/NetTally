using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NetTally;
using NetTally.Utility;

namespace NetTally.ViewModels
{
    public class MainViewModel : ViewModelBase
    {
        public ObservableCollectionExt<string> VoteCollection { get; }
        public ObservableCollectionExt<string> VoterCollection { get; }
        public QuestCollection QuestCollection { get; }

        public MainViewModel(QuestCollectionWrapper config)
        {

        }
    }
}
