using NetTally.Models;

namespace NetTally.Tally.Processing;

public sealed record VoteBlockRef(VoteBlock VoteBlock, bool IsReference);
