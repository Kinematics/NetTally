namespace NetTally.Configure;

internal static class ConfigValues
{
    /// <summary>
    /// The filename of the JSON config file.
    /// </summary>
    public const string UserConfigJsonFileName = "userconfig.json";

    /// <summary>
    /// The name for the keyed singleton for any legacy config info.
    /// </summary>
    public const string LegacyKey = "Legacy config";

    /// <summary>
    /// Defined name of the config section, as saved in the legacy XML config file.
    /// </summary>
    public const string SectionName = "NetTally.Quests";

    /// <summary>
    /// The name of the HTTP client that does not use a proxy.
    /// </summary>
    public const string NoProxy = "NoProxy";
    /// <summary>
    /// The name of the HTTP client that does use a proxy.
    /// </summary>
    public const string WithProxy = "WithProxy";
    /// <summary>
    /// The name of the HTTP client designed to read from the Github JSON API.
    /// </summary>
    public const string Github = "Github";
    /// <summary>
    /// The maximum number of retries for web requests.
    /// </summary>
    public const int MaxRetries = 3;
}
