using NetTally.Models.Votes;

namespace NetTally.Models.Behavior;

public static class VoteBlockUtility
{
    extension(VoteBlock voteBlock)
    {
        public int LineCount => voteBlock.Lines.Length;

        public bool HasChildLines =>
            voteBlock.LineCount > 1 &&
            voteBlock.Lines.Skip(1).All(v => v.Depth > 0);
    }
}

