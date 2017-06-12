using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

namespace NetTally.Collections
{
    /// <summary>
    /// Generic observable collection for Quest items.
    /// Can be serialized via Data Contract.
    /// </summary>
    public class QuestCollection : ObservableCollection<IQuest>
    {
        /// <summary>
        /// Indexer into the collection by quest name.
        /// </summary>
        /// <param name="name">The name of the quest to look for.</param>
        /// <returns>Returns the quest if found, or null if not.</returns>
        public IQuest this[string name] => this.FirstOrDefault(q => q.ThreadName == name);

        /// <summary>
        /// Add a new quest to the current collection.
        /// </summary>
        /// <returns>Returns the newly created quest if it was successfully added,
        /// or returns null if it was not (ie: duplicate).</returns>
        public IQuest AddNewQuest()
        {
            var nq = new Quest();
            Add(nq);
            if (this.Contains(nq))
                return nq;

            return this.FirstOrDefault(q => q.ThreadName == nq.ThreadName);
        }

        /// <summary>
        /// Override InsertItem so that we can prevent duplicate entries.
        /// </summary>
        /// <param name="index">Index to enter the new item at.</param>
        /// <param name="item">Item to be entered.</param>
        protected override void InsertItem(int index, IQuest item)
        {
            if (this.Any(q => q.ThreadName == item.ThreadName))
                return;

            base.InsertItem(index, item);
        }


        #region XML Serialization
        //public string GetAsSerializedXml()
        //{
        //    string xml;

        //    var questList = GetAsQuestType();

        //    XmlSerializer ser = new XmlSerializer(questList.GetType());

        //    using (StringWriter textWriter = new StringWriter())
        //    {
        //        ser.Serialize(textWriter, questList);
        //        xml = textWriter.ToString();
        //    }

        //    return xml;
        //}

        public List<Quest> GetAsQuestType()
        {
            var a = this.Select(q => q as Quest);
            return a.ToList();
        }

        //public void DeserializeFromXml(string xml)
        //{
        //    XmlSerializer ser = new XmlSerializer(typeof(List<Quest>));

        //    List<Quest> questList;

        //    using (StringReader textReader = new StringReader(xml))
        //    {
        //        questList = (List<Quest>)ser.Deserialize(textReader);
        //    }

        //    AddQuests(questList);
        //}

        public void AddQuests(IEnumerable<Quest> quests)
        {
            foreach (var quest in quests)
                Add(quest);
        }
        #endregion
    }
}
