namespace NetTally.Configure;

internal static class ConfigStrings
{
    /// <summary>
    /// The filename of the JSON config file.
    /// </summary>
    public const string UserConfigJsonFile = "userconfig.json";

    /// <summary>
    /// The name for the keyed singleton for any legacy config info.
    /// </summary>
    public const string LegacyKey = "Legacy config";

    /// <summary>
    /// Defined name of the config section, as saved in the legacy XML config file.
    /// </summary>
    public const string SectionName = "NetTally.Quests";

    public const string NoProxy = "NoProxy";
    public const string WithProxy = "WithProxy";
    public const string Github = "Github";

    public const int MaxRetries = 3;
}
