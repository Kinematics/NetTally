using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.ComponentModel;
using System.Xml.Serialization;
using System;

namespace NetTally
{
    public class Quests : IQuests, INotifyPropertyChanged
    {
        static List<IQuest> questList = new List<IQuest>();
        IQuest currentQuest;

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

        /// <summary>
        /// Public method call to force a property changed invocation for the quest list.
        /// </summary>
        public void Update()
        {
            OnPropertyChanged("QuestListNames");
        }
        #endregion

        #region Properties
        /// <summary>
        /// Property solely used for serialization of the quest list.
        /// Needs to be an array or it won't deserialize back properly.
        /// </summary>
        [XmlArray("QuestList")]
        [XmlArrayItem("Quest", Type = typeof(Quest))]
        public Quest[] QuestList
        {
            get { return questList.Cast<Quest>().ToArray(); }
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

        [XmlElement("CurrentQuestName")]
        public string CurrentQuestName
        {
            get { return CurrentQuest?.Name; }
            set { SetCurrentQuestByName(value); }
        }

        /// <summary>
        /// Gets the current list of quest names, ordered by name.
        /// Used for binding with the main window combo box.
        /// </summary>
        [XmlIgnore()]
        public List<string> QuestListNames
        {
            get
            {
                var names = from q in questList orderby q.Name select q.Name;
                return names.ToList();
            }
        }

        /// <summary>
        /// Used for binding with the main window combo box.
        /// </summary>
        [XmlIgnore()]
        public IQuest CurrentQuest
        {
            get { return currentQuest; }
            set
            {
                if (value != null && !questList.Contains(value))
                    throw new ArgumentOutOfRangeException("CurrentQuest", "Cannot set the current quest to a quest that is not in the quest list.");
                currentQuest = value;
                // Call OnPropertyChanged whenever the property is updated
                OnPropertyChanged();
            }
        }

        #endregion

        #region Quest object-based functions
        /// <summary>
        /// Add a quest to the current list of quests.
        /// Does not add the quest if another quest of the same name already exists.
        /// </summary>
        /// <param name="quest">The quest to add.</param>
        /// <returns>Returns whether the quest was added.</returns>
        public bool AddQuest(IQuest quest)
        {
            if (quest == null)
                throw new ArgumentNullException(nameof(quest));

            if (!questList.Any(q => q.Name == quest.Name))
            {
                questList.Add(quest);
                OnPropertyChanged("QuestListNames");
                if (CurrentQuest == null)
                    CurrentQuest = quest;
                return true;
            }

            return false;
        }

        /// <summary>
        /// Add a new quest (type Quest).
        /// </summary>
        /// <returns>Returns the new quest if it was added, or the existing quest of the same name
        /// if the new quest could not be added.</returns>
        public IQuest AddNewQuest()
        {
            var nq = new Quest();
            if (AddQuest(nq))
                return nq;
            else
                return GetQuestByName(nq.Name);
        }

        /// <summary>
        /// Remove the specified quest.
        /// </summary>
        /// <param name="quest">The quest to remove.</param>
        /// <returns>Returns true if the quest was found and removed.</returns>
        public bool RemoveQuest(IQuest quest)
        {
            if (questList.Remove(quest))
            {
                OnPropertyChanged("QuestListNames");
                if (CurrentQuest == quest)
                    CurrentQuest = questList.FirstOrDefault();
                return true;
            }

            return false;
        }

        /// <summary>
        /// Remove the current quest from the list of quest.
        /// </summary>
        public bool RemoveCurrentQuest()
        {
            return RemoveQuest(CurrentQuest);
        }

        /// <summary>
        /// Clear the list of quests.
        /// </summary>
        public void Clear()
        {
            questList.Clear();
            CurrentQuest = null;
            OnPropertyChanged("QuestListNames");
        }
        #endregion

        #region Name-based functions
        /// <summary>
        /// Get a quest by name.
        /// </summary>
        /// <param name="name">The name of the quest to get.</param>
        /// <returns>Returns the quest, if found.</returns>
        public IQuest GetQuestByName(string name)
        {
            return questList.FirstOrDefault(q => q.Name == name);
        }

        /// <summary>
        /// Get a quest by name.  Static version that can be called without a class instance,
        /// to allow the type converter to access the quest list.
        /// </summary>
        /// <param name="name">The name of the quest to get.</param>
        /// <returns>Returns the quest, if found.</returns>
        public static IQuest StaticGetQuestByName(string name)
        {
            return questList.FirstOrDefault(q => q.Name == name);
        }

        /// <summary>
        /// Sets the current quest to the quest specified by name.
        /// If the specified name is not found, and the current quest is null,
        /// it will set the current quest to the first quest in the quest list.
        /// </summary>
        /// <param name="name">The name of the quest to be made current.</param>
        public void SetCurrentQuestByName(string name)
        {
            IQuest q = questList.FirstOrDefault(a => a.Name == name);

            if (q == null && CurrentQuest == null)
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
