using Microsoft.Extensions.Logging;
using NetTally.Enums;
using NetTally.Models;

namespace NetTally.Debugging.Logging;

public static partial class DebugLoggerMessages
{
    // Application

    [LoggerMessage(
        Level = LogLevel.Debug,
        Message = "Application constructor completed.")]
    public static partial void ApplicationConstructed(this ILogger logger);

    [LoggerMessage(
        Level = LogLevel.Debug,
        Message = "Configuration saved to {Path}")]
    public static partial void ConfigurationSaved(this ILogger logger, string path);

    [LoggerMessage(
        Level = LogLevel.Debug,
        Message = "Failed to save configuration to {Path}")]
    public static partial void ConfigurationSaveFailed(this ILogger logger, string path);

    [LoggerMessage(
        Level = LogLevel.Debug,
        Message = "Showing Window {Type}")]
    public static partial void ShowingWindow(this ILogger logger, Type type);

    [LoggerMessage(
        Level = LogLevel.Debug,
        Message = "Showing Dialog Window {Type}")]
    public static partial void ShowingDialog(this ILogger logger, Type type);

    [LoggerMessage(
        Level = LogLevel.Debug,
        Message = "Requested a change in theme from {Before} to {After}")]
    public static partial void ThemeChangeRequest(this ILogger logger, AvaloniaTheme before, AvaloniaTheme after);

    [LoggerMessage(
        Level = LogLevel.Debug,
        Message = "Requested a change in theme from {Before} to {After}")]
    public static partial void ThemeChangeRequest(this ILogger logger, WPFTheme before, WPFTheme after);

    [LoggerMessage(
        Level = LogLevel.Debug,
        Message = "Loaded {Count} legacy quests")]
    public static partial void LoadedLegacyQuests(this ILogger logger, int count);

    [LoggerMessage(
        Level = LogLevel.Debug,
        Message = "Loaded {Count} user quests")]
    public static partial void LoadedUserQuests(this ILogger logger, int count);

    [LoggerMessage(
        Level = LogLevel.Debug,
        Message = "Moved quest {Name} from position {Before} to position {After}.")]
    public static partial void MovedQuest(this ILogger logger, string name, int before, int after);

    [LoggerMessage(
        Level = LogLevel.Debug,
        Message = "Removing quest {Name}")]
    public static partial void RemovingQuest(this ILogger logger, string name);

    [LoggerMessage(
        Level = LogLevel.Debug,
        Message = "Received notification of property change from {Sender}: {PropertyName}.")]
    public static partial void PropertyChangeNotification(this ILogger logger, Type? sender, string? propertyName);

    [LoggerMessage(
        Level = LogLevel.Debug,
        Message = "Clearing cache. Current count: {Count}")]
    public static partial void ClearingCache(this ILogger logger, int count);

    [LoggerMessage(
        Level = LogLevel.Debug,
        Message = "Global options were saved.")]
    public static partial void GlobalOptionsSaved(this ILogger logger);

    [LoggerMessage(
        Level = LogLevel.Debug,
        Message = "Quest options were saved.")]
    public static partial void QuestOptionsSaved(this ILogger logger);

    [LoggerMessage(
        Level = LogLevel.Debug,
        Message = "Vote counter was reset.")]
    public static partial void VoteCounterReset(this ILogger logger);

    [LoggerMessage(
        Level = LogLevel.Debug,
        Message = "Global options were reset.")]
    public static partial void GlobalOptionsReset(this ILogger logger);

    [LoggerMessage(
        Level = LogLevel.Debug,
        Message = "Reordered tasks were saved.")]
    public static partial void ReorderedTasksSaved(this ILogger logger);

    [LoggerMessage(
        Level = LogLevel.Debug,
        Message = "Reading Quest {ThreadName}")]
    public static partial void ReadingQuest(this ILogger logger, string threadName);

    [LoggerMessage(
        Level = LogLevel.Debug,
        Message = "Thread information acquired for {QuestDisplayName}.\n({ThreadData})")]
    public static partial void ThreadInformationGot(this ILogger logger, string questDisplayName, ThreadInfo threadData);

    [LoggerMessage(
        Level = LogLevel.Debug,
        Message = "No thread information acquired for {QuestDisplayName}.")]
    public static partial void ThreadInformationFailed(this ILogger logger, string questDisplayName);

    // HTTP:

    [LoggerMessage(
        Level = LogLevel.Debug,
        Message = "Requested URL redirect for \"{Description}\"")]
    public static partial void RedirectRequested(this ILogger logger, string description);

    [LoggerMessage(
        Level = LogLevel.Debug,
        Message = "Redirect request succeeded. Using {ResponseUri}")]
    public static partial void RedirectedSucceeded(this ILogger logger, string responseUri);

    [LoggerMessage(
        Level = LogLevel.Debug,
        Message = "Redirect request failed for \"{Description}\".")]
    public static partial void RedirectFailed(this ILogger logger, string description);

    [LoggerMessage(
        Level = LogLevel.Debug,
        Message = "{Message}")]
    public static partial void StatusChangeMessage(this ILogger logger, string message);

    [LoggerMessage(
        Level = LogLevel.Debug,
        Message = "Tried: {Description} - Attempt {Count} - Retrying")]
    public static partial void RetryAgain(this ILogger logger, string description, int count);

    [LoggerMessage(
        Level = LogLevel.Debug,
        Message = "Tried: {Description} - Attempt {Count} - Failed")]
    public static partial void RetryFailed(this ILogger logger, string description, int count);


}
