using NetTally.Models.Defaults;
using NetTally.Models.Votes;
using NetTally.Utility;

namespace NetTally.Models.Creation;

/// <summary>
/// Extension class for creating <see cref="VoteTask"/> objects.
/// </summary>
public static class VoteTaskCreation
{
    extension(VoteTask)
    {
        /// <summary>
        /// Create a new <see cref="VoteTask"/> based on the provided input.
        /// </summary>
        /// <param name="task">The text for the task.</param>
        /// <returns>A new <see cref="VoteTask"/></returns>
        public static VoteTask Create(string task)
        {
            if (string.IsNullOrWhiteSpace(task))
                return VoteTask.None;

            task = task.RemoveUnsafeCharacters().Trim();

            return new VoteTask(task);
        }
    }
}
