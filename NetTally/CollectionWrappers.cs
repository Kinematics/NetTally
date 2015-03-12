using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Runtime.Serialization;

namespace NetTally
{
    /// <summary>
    /// Wrapper class to allow XML serialization of both the quest collection and the
    /// currently selected quest.
    /// </summary>
    [DataContract(Name = "Quests")]
    public class QuestCollectionWrapper
    {
        [DataMember(Order = 1)]
        public QuestCollection QuestCollection { get; set; }
        [DataMember(Order = 2)]
        public string CurrentQuest { get; set; }

        public QuestCollectionWrapper(QuestCollection questCollection, string currentQuest)
        {
            QuestCollection = questCollection;
            CurrentQuest = currentQuest;
        }
    }

}
