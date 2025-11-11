namespace NetTally.Models.Defaults;

public static class QuestIdDefaults
{
    extension(QuestId)
    {
        public static QuestId Empty => _empty;
    }

    private static readonly QuestId _empty = new(Guid.Empty);
}
