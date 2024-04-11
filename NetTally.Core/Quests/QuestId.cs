using System;

namespace NetTally.Quests;
public readonly record struct QuestId(Guid Id)
{
    public static readonly QuestId Empty = new(Guid.Empty);
    public static QuestId NewQuestId() => new(Guid.NewGuid());
}
