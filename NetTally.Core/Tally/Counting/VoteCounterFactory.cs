using Microsoft.Extensions.DependencyInjection;
using NetTally.Models;

namespace NetTally.Tally.Counting;

public class VoteCounterFactory(IServiceProvider serviceProvider)
{
    private readonly IServiceProvider serviceProvider = serviceProvider;

    public IVoteCounter GetVoteCounter(Quest quest)
    {
        return ActivatorUtilities.CreateInstance<VoteCounter>(serviceProvider, quest);
    }
}
