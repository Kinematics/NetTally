namespace NetTally.Models.Mapping;

public static class SourceMapping
{
    extension(Source source)
    {
        public T Map<T>(
            Func<NoSource, T> noSourceFunc,
            Func<SourceLocation, T> sourceLocationFunc) =>
            source switch
            {
                NoSource noSource => noSourceFunc(noSource),
                SourceLocation sourceLocation => sourceLocationFunc(sourceLocation),
                _ => throw new NotImplementedException($"Unknown Source type: {source.GetType()}")
            };
    }
}

