using NetTally.Models;
using NetTally.Utility.Events;

namespace NetTally.Input.Forums.Reading;

public interface IForumReader
{
    Task<QuestData> ReadQuestAsync(Quest quest, CancellationToken cancellationToken);

    public event EventHandler<MessageEventArgs>? StatusChanged;
}
