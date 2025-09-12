using System.Collections.Immutable;
using NetTally.Tally.Posts.Component;

namespace NetTally.Input.Forums.Reading;
public record QuestData(ImmutableList<string> Titles, ImmutableList<Post> Posts);

public static class PredefinedQuestData
{
    extension(QuestData)
    {
        public static QuestData Empty => _empty;
    }

    private static readonly QuestData _empty = new([], []);
}

public static class QuestDataCreation
{
    extension(QuestData)
    {
        public static QuestData Create(string title, IEnumerable<Post> posts)
        {
            return new QuestData([title], [.. posts]);
        }

        public static QuestData Create(IEnumerable<string> titles, IEnumerable<Post> posts)
        {
            return new QuestData([.. titles], [.. posts]);
        }
    }
}

public static class QuestDataExtensions
{
    extension(QuestData data)
    {
        public QuestData CombineWith(QuestData other)
        {
            return new QuestData(
                [.. data.Titles, .. other.Titles],
                [.. data.Posts, .. other.Posts]);
        }

        public QuestData CombineWith(string title, IEnumerable<Post> posts)
        {
            return new QuestData(
                data.Titles.Add(title),
                [.. data.Posts, .. posts]);
        }

        public QuestData CombineWith(IEnumerable<string> titles, IEnumerable<Post> posts)
        {
            return new QuestData(
                [.. data.Titles, .. titles],
                [.. data.Posts, .. posts]);
        }

        public QuestData CombineWith(string title)
        {
            return new QuestData(
                data.Titles.Add(title),
                data.Posts);
        }

        public QuestData CombineWith(IEnumerable<Post> posts)
        {
            return new QuestData(
                data.Titles,
                [.. data.Posts, .. posts]);
        }
    }
}