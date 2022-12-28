using System.Collections.ObjectModel;

namespace NetTally.Global
{
    public interface IQuestsInfo
    {
        ObservableCollection<Quest> Quests { get; }
        Quest? SelectedQuest { get; }
    }
}
