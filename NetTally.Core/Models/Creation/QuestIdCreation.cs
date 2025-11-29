namespace NetTally.Models;

/// <summary>
/// Class for creating new <see cref="QuestId"/> instances.
/// </summary>
public static class QuestIdCreation
{
    extension(QuestId)
    {
        /// <summary>
        /// Creates a new instance of the <see cref="QuestId"/> type with a unique identifier.
        /// </summary>
        /// <returns>A <see cref="QuestId"/> containing a newly generated unique identifier.</returns>
        public static QuestId Create()
        {
            return new(Guid.NewGuid());
        }
    }
}
