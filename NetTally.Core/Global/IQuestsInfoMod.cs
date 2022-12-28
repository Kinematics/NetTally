using System.Collections.ObjectModel;

namespace NetTally.Global
{
    public interface IQuestsInfoMod
    {
        ObservableCollection<Quest> Quests { get; }
        Quest? SelectedQuest { get; set; }
    }
}
