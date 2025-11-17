using Microsoft.Extensions.Logging;

namespace NetTally.Debugging.Logging;

public static partial class InformationLoggerMessages
{
    [LoggerMessage(
        Level = LogLevel.Information,
        Message = "Starting application. Version: {Version}")]
    public static partial void AppStartup(this ILogger logger, string version);

    [LoggerMessage(
        Level = LogLevel.Information,
        Message = "Requested {DocType} document \"{Description}\" ({Url})")]
    public static partial void DocumentRequested(this ILogger logger,
        string docType, string description, string url);
}
