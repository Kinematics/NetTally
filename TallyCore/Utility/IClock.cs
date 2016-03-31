using System;

namespace NetTally.Utility
{
    public interface IClock
    {
        DateTime Now { get; }
    }
}
