using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using NetTally.Tally.ComponentsF.Posts;

namespace NetTally.Input.Forums.Reading;
public interface IForumReader
{
    Task<(List<string> Titles, List<PostType> Posts)>
        ReadQuestAsync(Quest quest, CancellationToken cancellationToken);
}
