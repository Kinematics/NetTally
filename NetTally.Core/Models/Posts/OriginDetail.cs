namespace NetTally.Models;

public abstract record OriginDetail();

public sealed record NoOriginDetail() : OriginDetail;

public sealed record OriginSource(Uri Thread, Uri Permalink, PostId PostId, PostNumber PostNumber) : OriginDetail;
