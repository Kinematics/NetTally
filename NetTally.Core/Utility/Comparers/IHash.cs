using System.Globalization;

namespace NetTally.Utility.Comparers
{
    public interface IHash
    {
        int HashFunction(string str, CompareInfo info, CompareOptions options);
    }
}
