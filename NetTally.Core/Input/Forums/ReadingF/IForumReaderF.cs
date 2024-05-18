using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using NetTally.CustomEventArgs;
using NetTally.Tally.ComponentsF.Posts;

namespace NetTally.Input.Forums.ReadingF;
public interface IForumReaderF
{
    Task<(List<string> Titles, List<PostType> Posts)>
        ReadQuestAsync(Quest quest, CancellationToken cancellationToken);

    public event EventHandler<MessageEventArgs>? StatusChanged;
}
