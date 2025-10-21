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
            Func<ThreadRangeByStartingId, T> startIdMap,
            Func<ThreadRangeByPostRange, T> postRangeMap,
            Func<ThreadRangeByStartingPost, T> startPostMap) => threadRange switch
            {
                ThreadRangeByStartingId idRange => startIdMap(idRange),
                ThreadRangeByPostRange postRange => postRangeMap(postRange),
                ThreadRangeByStartingPost postRange => startPostMap(postRange),
                _ => throw new NotImplementedException($"Unknown Thread Range type: {threadRange.GetType()}")
            };
    }
}
