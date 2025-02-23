using NetTally.CustomEventArgs;
using NetTally.Tally.Components.Posts;

namespace NetTally.Input.Forums.Reading;
public interface IForumReader
{
    Task<(IEnumerable<string> Titles, IEnumerable<Post> Posts)>
        ReadQuestAsync(Quest quest, CancellationToken cancellationToken);

    public event EventHandler<MessageEventArgs>? StatusChanged;
}
