using System.Text.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NetTally.Debugging.Logging;
using NetTally.Product;
using NetTally.Utility.Json;

namespace NetTally.Configure.Json;

internal class JsonConfiguration(
    ILogger<JsonConfiguration> logger,
    IQuestsInfo questsInfo,
    IOptions<GlobalSettings> options)
{
    private readonly ILogger<JsonConfiguration> logger = logger;
    private readonly IQuestsInfo questsInfo = questsInfo;
    private readonly GlobalSettings globalSettings = options.Value;

    /// <summary>
    /// Saves the user configuration information to the user config file(s).
    /// </summary>
    public void SaveJsonConfiguration()
    {
        ConfigInfo config = new(questsInfo.Quests, questsInfo.SelectedQuest?.ThreadName, globalSettings);

        foreach (var path in GetConfigurationPaths())
        {
            try
            {
                using var stream = File.Create(path);

                // Async can fail on large saves when exiting. Use sync.
                JsonSerializer.Serialize(stream, config,
                    ConfigInfoSourceGeneratorContext.Default.ConfigInfo);

                logger.ConfigurationSaved(path);
            }
            catch (Exception)
            {
                logger.ConfigurationSaveFailed(path);
            }
        }
    }

    /// <summary>
    /// Get the available paths to load or save user configuration.
    /// This may vary depending on OS and directory permissions.
    /// </summary>
    /// <returns>An enumeration of configuration file paths.</returns>
    public static IEnumerable<string> GetConfigurationPaths()
    {
        // Try to find the AppSettings path on Windows, and use it
        // first when trying to load or save user config info.
        if (OperatingSystem.IsWindows())
        {
            string path = Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData);

            if (Path.Exists(path))
            {
                path = Path.Combine(path, ProductInfo.Name);
                Directory.CreateDirectory(path);

                yield return Path.Combine(path, ConfigValues.UserConfigJsonFileName);
            }
        }

        // After that, supply the file for the local directory.
        // This will take precedence over the AppSettings version of the file, if it exists.
        yield return ConfigValues.UserConfigJsonFileName;
    }
}
