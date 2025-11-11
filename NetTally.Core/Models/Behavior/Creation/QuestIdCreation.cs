namespace NetTally.Models;

public static class QuestIdCreation
{
    extension(QuestId)
    {
        public static QuestId Create()
        {
            return new(Guid.NewGuid());
        }
    }
}
