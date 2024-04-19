using System.ComponentModel;

namespace NetTally.Utility.Comparers
{
    public interface IAgnostic
    {
        void ComparisonPropertyChanged(Quest quest, PropertyChangedEventArgs e);
    }
}
