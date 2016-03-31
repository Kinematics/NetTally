using System;

namespace NetTally.Utility
{
    public class DefaultClock : IClock
    {
        public DateTime Now => DateTime.Now;
    }
}
