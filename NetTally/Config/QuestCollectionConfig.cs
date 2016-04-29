using System.Configuration;

namespace NetTally
{
    /// <summary>
    /// Wrapper class for a collection of quest elements to be added to the user config file.
    /// </summary>
    public class QuestElementCollection : ConfigurationElementCollection
    {
        protected override ConfigurationElement CreateNewElement() => new QuestElement();

        protected override object GetElementKey(ConfigurationElement element)
        {
            QuestElement qe = element as QuestElement;
            if (qe.ThreadName != "")
                return qe.ThreadName;
            return qe.DisplayName;
        }

        public new QuestElement this[string name] => (QuestElement)BaseGet(name);

        public QuestElement this[int index]
        {
            get
            {
                return (QuestElement)BaseGet(index);
            }
            set
            {
                if (BaseGet(index) != null)
                {
                    BaseRemoveAt(index);
                }
                BaseAdd(index, value);
            }
        }

        public void Add(IQuest quest)
        {
            var questElement = new QuestElement(quest.ThreadName, quest.DisplayName, quest.PostsPerPage, quest.StartPost, quest.EndPost,
                quest.CheckForLastThreadmark, quest.PartitionMode, quest.UseCustomThreadmarkFilters, quest.CustomThreadmarkFilters,
                quest.TrimExtendedText);
            BaseAdd(questElement, false);
        }

        public void Clear()
        {
            BaseClear();
        }
    }
}
