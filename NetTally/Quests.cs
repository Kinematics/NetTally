using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using System.ComponentModel;
using System.Globalization;
using System.Windows.Data;


namespace NetTally
{
    class Quests : INotifyPropertyChanged
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


        public static SortedList<string, Quest> questList = new SortedList<string, Quest>();

        public IList<string> QuestList
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
