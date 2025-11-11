namespace NetTally.Models;

public static class VoteLineProperties
{
    extension(VoteLine voteLine)
    {
        public bool HasTask => voteLine.Task != VoteTask.None;

        public int Depth => voteLine.Prefix.Depth;
    }
}
