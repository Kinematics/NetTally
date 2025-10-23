namespace NetTally.Models.Mapping;

public static class OriginDetailMapping
{
    extension(OriginDetail detail)
    {
        public T Map<T>(
            Func<NoOriginDetail, T> noOriginDetailFunc,
            Func<OriginSource, T> originSourceFunc) =>
            detail switch
            {
                NoOriginDetail noOriginDetail => noOriginDetailFunc(noOriginDetail),
                OriginSource originSource => originSourceFunc(originSource),
                _ => throw new NotImplementedException($"Unknown OriginDetail type: {detail.GetType()}")
            };
    }
}

