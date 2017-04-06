namespace NetTally.Collections
{
    /// <summary>
    /// Wrapper class to allow XML serialization of both the quest collection and the
    /// currently selected quest and options.
    /// </summary>
    public class QuestCollectionWrapper
    {
        public QuestCollection QuestCollection { get; }
        public string CurrentQuest { get; set; }

        public QuestCollectionWrapper()
        {
            QuestCollection = new QuestCollection();
            CurrentQuest = null;
        }

        public QuestCollectionWrapper(QuestCollection questCollection, string currentQuest)
        {
            if (questCollection == null)
            {
                QuestCollection = new QuestCollection();
                CurrentQuest = null;
            }
            else
            {
                QuestCollection = questCollection;
                CurrentQuest = currentQuest;
            }
        }
    }
}
