namespace NetTally.Models;

public static class OriginDetailDefaults
{
    extension(OriginDetail)
    {
        public static OriginDetail None => _noDetail;
    }

    private static readonly OriginDetail _noDetail = new NoOriginDetail();
}