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
    public class SystemClock : IClock
    {
        public DateTime Now => DateTime.Now;
    }

    public class StaticClock : IClock
    {
        readonly DateTime _now = new DateTime(2017, 1, 1, 12, 1, 0, 0, DateTimeKind.Utc);

        public StaticClock() { }

        public StaticClock(DateTime _static)
        {
            _now = _static;
        }

        public DateTime Now => _now;
    }
}
