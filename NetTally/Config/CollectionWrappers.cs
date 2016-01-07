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
        public DisplayMode DisplayMode { get; set; }
        public bool AllowRankedVotes { get; set; }
        public bool IgnoreSymbols { get; set; }
        public bool TrimExtendedText { get; set; }
        public bool IgnoreSpoilers { get; set; }

        public QuestCollectionWrapper(QuestCollection questCollection, string currentQuest)
        {
            QuestCollection = questCollection;
            CurrentQuest = currentQuest;

            DisplayMode = AdvancedOptions.Instance.DisplayMode;
            AllowRankedVotes = AdvancedOptions.Instance.AllowRankedVotes;
            IgnoreSymbols = AdvancedOptions.Instance.IgnoreSymbols;
            TrimExtendedText = AdvancedOptions.Instance.TrimExtendedText;
            IgnoreSpoilers = AdvancedOptions.Instance.IgnoreSpoilers;
        }
    }

}
