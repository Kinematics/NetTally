using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.ComponentModel;
using System.Xml.Serialization;


namespace NetTally
{
    public class Quests : INotifyPropertyChanged
    {
        static List<Quest> questList = new List<Quest>();
        Quest currentQuest;

        /// <summary>
        /// Empty constructor for XML serialization.
        /// </summary>
        public Quests() { }

        #region Property update notifications
        /// <summary>
        /// Event for INotifyPropertyChanged.
        /// </summary>
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

        #region Functions for manipulating the quest list
        /// <summary>
        /// Add a quest to the current list of quests.
        /// </summary>
        /// <param name="quest"></param>
        public void AddToQuestList(Quest quest)
        {
            if (!questList.Any(q => q.Name == quest.Name))
            {
                questList.Add(quest);
                OnPropertyChanged("QuestListNames");
            }
        }

        /// <summary>
        /// Remove the current quest from the list of quest.
        /// </summary>
        public void RemoveCurrentQuest()
        {
            if (questList.Remove(CurrentQuest))
            {
                OnPropertyChanged("QuestListNames");
                CurrentQuest = questList.FirstOrDefault();
            }
        }

        /// <summary>
        /// Remove the specified quest from the list of quests.
        /// </summary>
        /// <param name="quest">The quest to remove.</param>
        public void RemoveFromQuestList(Quest quest)
        {
            if (questList.Remove(quest))
            {
                OnPropertyChanged("QuestListNames");
                if (!questList.Contains(CurrentQuest))
                    CurrentQuest = questList.FirstOrDefault();
            }
        }

        /// <summary>
        /// Public method call to force a property changed invocation for the quest list.
        /// </summary>
        public void Update()
        {
            OnPropertyChanged("QuestListNames");
        }
        #endregion

        #region Access functions for binding and serialization
        /// <summary>
        /// Static query function to allow the type converter to access the quest list.
        /// </summary>
        /// <param name="name">The name of the quest to find.</param>
        /// <returns>Returns the quest that matches the provided name.</returns>
        public static Quest GetQuest(string name)
        {
            return questList.FirstOrDefault(q => q.Name == name);
        }


        /// <summary>
        /// Property solely used for serialization of the quest list.
        /// Needs to be an array or it won't deserialize back properly.
        /// </summary>
        [XmlArray("QuestList")]
        [XmlArrayItem("Quest", Type = typeof(Quest))]
        public Quest[] QuestList
        {
            get { return questList.ToArray(); }
            set
            {
                if (value != null)
                {
                    questList.Clear();
                    questList.AddRange(value);
                    OnPropertyChanged("QuestListNames");
                }
            }
        }

        /// <summary>
        /// Used for binding with the main window combo box.
        /// </summary>
        [XmlIgnore()]
        public string[] QuestListNames
        {
            get
            {
                var names = from q in questList orderby q.Name select q.Name;
                return names.ToArray();
            }
        }


        /// <summary>
        /// Used for binding with the main window combo box.
        /// </summary>
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

        /// <summary>
        /// The name of the current quest.
        /// Setting this changes the current quest, not the current quest name.
        /// </summary>
        public string CurrentQuestName
        {
            get { return CurrentQuest?.Name; }
            set { SetCurrentQuestByName(value); }
        }

        /// <summary>
        /// Function to set the current quest based on the supplied name.
        /// </summary>
        /// <param name="name">The name of the quest to change to.</param>
        public void SetCurrentQuestByName(string name)
        {
            Quest q = questList.FirstOrDefault(a => a.Name == name);
            if (q == null && CurrentQuestName == null)
            {
                q = questList.FirstOrDefault();
            }
            if (q != null)
            {
                CurrentQuest = q;
            }
        }
        #endregion
    }
}
