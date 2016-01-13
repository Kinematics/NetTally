namespace NetTally
{
    /// <summary>
    /// Wrapper class to allow XML serialization of both the quest collection and the
    /// currently selected quest and options.
    /// </summary>
    public class QuestCollectionWrapper
    {
        public QuestCollection QuestCollection { get; set; }
        public string CurrentQuest { get; set; }

        public QuestCollectionWrapper(QuestCollection questCollection, string currentQuest)
        {
            QuestCollection = questCollection;
            CurrentQuest = currentQuest;
        }
    }
}
