using NetTally.Models.Votes;

namespace NetTally.Tally.Processing;

public sealed record VoteBlockRef(VoteBlock VoteBlock, bool IsReference);
