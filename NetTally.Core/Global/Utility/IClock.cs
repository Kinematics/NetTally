using System;

namespace NetTally.Utility
{
    /// <summary>
    /// An interface to get a date/time value.
    /// </summary>
    public interface IClock
    {
        DateTime Now { get; }
    }

    /// <summary>
    /// Default implementation of IClock, which uses the system clock.
    /// </summary>
    public class DefaultClock : IClock
    {
        public DateTime Now => DateTime.Now;
    }
}
