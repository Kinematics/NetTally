using System;
using Microsoft.Extensions.DependencyInjection;

namespace NetTally.Tally.Components.Counting;
public class VoteCounterFactory(IServiceProvider serviceProvider)
{
    private readonly IServiceProvider serviceProvider = serviceProvider;

    public IVoteCounter GetVoteCounter(Quest quest)
    {
        return ActivatorUtilities.CreateInstance<VoteCounter>(serviceProvider, quest);
    }
}
