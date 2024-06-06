using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using NetTally.CustomEventArgs;
using NetTally.Tally.Components.Posts;

namespace NetTally.Input.Forums.ReadingF;
public interface IForumReader
{
    Task<(IEnumerable<string> Titles, IEnumerable<PostType> Posts)>
        ReadQuestAsync(Quest quest, CancellationToken cancellationToken);

    public event EventHandler<MessageEventArgs>? StatusChanged;
}
