using NetTally.Tally.Vote.Component;
using NetTally.Tally.Vote.Component.Creation;
using NetTally.Utility;

namespace NetTally.Tally.Vote.Component.Creation;

/// <summary>
/// Extension class for creating <see cref="VoteTask"/> objects.
/// </summary>
public static class VoteTaskCreation
{
    extension(VoteTask)
    {
        /// <summary>
        /// The default, empty, <see cref="VoteTask"/>
        /// </summary>
        public static VoteTask Empty => _empty;

        /// <summary>
        /// Create a new <see cref="VoteTask"/> based on the provided input.
        /// </summary>
        /// <param name="task">The text for the task.</param>
        /// <returns>A new <see cref="VoteTask"/></returns>
        public static VoteTask Create(string task)
        {
            if (string.IsNullOrWhiteSpace(task))
                return VoteTask.Empty;

            task = task.RemoveUnsafeCharacters().Trim();

            return new VoteTask(task);
        }
    }

    private static readonly VoteTask _empty = new("");
}
