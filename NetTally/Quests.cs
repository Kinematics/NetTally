using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.ComponentModel;
using System.Xml.Serialization;


namespace NetTally
{
    public class Quests : INotifyPropertyChanged
    {
        public Quests()
        {
        }


        #region Property update notifications
        public event PropertyChangedEventHandler PropertyChanged;

        /// <summary>
        /// Function to raise events when a property has been changed.
        /// </summary>
        /// <param name="propertyName">The name of the property that was modified.</param>
        protected void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
        #endregion

        [XmlIgnore()]
        public static SortedList<string, Quest> questList = new SortedList<string, Quest>();


        [XmlArray("QuestList")]
        [XmlArrayItem("Quest", Type = typeof(Quest))]
        public Quest[] QuestList
        {
            get { return new List<Quest>(questList.Values).ToArray(); }
            set
            {
                if (value != null)
                {
                    foreach (var q in value)
                    {
                        questList.Add(q.Name, q);
                    }
                }
            }
        }

        public void Init()
        {
            OnPropertyChanged("QuestList");
            if (questList.Count > 0)
            {
                CurrentQuest = questList.First().Value;
            }
        }


        [XmlIgnore()]
        public IList<string> QuestListNames
        {
            get { return questList.Keys; }
        }

        public void AddToQuestList(Quest quest)
        {
            if (!questList.ContainsKey(quest.Name))
            {
                questList.Add(quest.Name, quest);
                OnPropertyChanged("QuestList");
                CurrentQuest = questList.First().Value;
            }
        }

        public void RemoveFromQuestList(Quest quest)
        {
            if (questList.Remove(quest.Name))
            {
                OnPropertyChanged("QuestList");
                CurrentQuest = questList.FirstOrDefault().Value;
            }
        }

        Quest currentQuest;
        [XmlIgnore()]
        public Quest CurrentQuest
        {
            get { return currentQuest; }
            set
            {
                currentQuest = value;
                // Call OnPropertyChanged whenever the property is updated
                OnPropertyChanged();
            }
        }
    }
}
