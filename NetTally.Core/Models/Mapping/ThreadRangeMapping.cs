using NetTally.Models.Threads;

namespace NetTally.Models.Mapping;

/// <summary>
/// Extension class to handle mapping of different behavior to each
/// type of <see cref="ThreadRange"/>.
/// </summary>
internal static class ThreadRangeMapping
{
    extension(ThreadRange threadRange)
    {
        public T Map<T>(
            Func<ThreadRangeById, T> idMap,
            Func<ThreadRangeByPosts, T> postMap)
        {
            return threadRange switch
            {
                ThreadRangeById idRange => idMap(idRange),
                ThreadRangeByPosts postRange => postMap(postRange),
                _ => throw new InvalidOperationException("Unknown Thread Range type.")
            };
        }
    }
}
