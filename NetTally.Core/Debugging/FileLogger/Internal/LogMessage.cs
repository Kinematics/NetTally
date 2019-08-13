using System;

namespace NetTally.Debugging.FileLogger.Internal
{
    public struct LogMessage
    {
        public DateTimeOffset Timestamp { get; set; }
        public string Message { get; set; }
    }
}
