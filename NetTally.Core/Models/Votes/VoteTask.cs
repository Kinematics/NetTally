namespace NetTally.Models.Votes;

/// <summary>
/// Data type to store a vote task.
/// </summary>
/// <param name="Name">The name of the task.</param>
public sealed record VoteTask(string Name);

public static class VoteTaskPredefined
{
    extension(VoteTask)
    {
        public static VoteTask Empty => _empty;
    }

    private static readonly VoteTask _empty = new("");
}
