using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NetTally.Avalonia.Config.Xml;
using NetTally.Global;
using NetTally.SystemInfo;

namespace NetTally.Avalonia.Config.Json
{
    internal class JsonConfiguration(
        ILogger<JsonConfiguration> logger,
        IQuestsInfo questsInfo,
        IOptions<GlobalSettings> options)
    {
        private readonly ILogger<JsonConfiguration> logger = logger;
        private readonly IQuestsInfo questsInfo = questsInfo;
        private readonly GlobalSettings globalSettings = options.Value;

        readonly JsonSerializerOptions jsonOptions = new()
        {
            WriteIndented = true,
            DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingDefault,
            IgnoreReadOnlyProperties = true
        };

        /// <summary>
        /// Saves the user configuration information to the user config file(s).
        /// </summary>
        public void SaveJsonConfiguration()
        {
            try
            {
                ConfigInfo config = new(questsInfo.Quests, questsInfo.SelectedQuest?.ThreadName, globalSettings);

                foreach (var path in GetConfigurationPaths())
                {
                    using var stream = File.Create(path);

                    // Async can fail on large saves when exiting. Use sync.
                    JsonSerializer.Serialize(stream, config, jsonOptions);

                    logger.LogDebug("Configuration saved to {path}", path);
                }
            }
            catch (Exception e)
            {
                logger.LogWarning(e, "Unable to save configuration.");
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

                    yield return Path.Combine(path, ConfigStrings.UserConfigJsonFile);
                }
            }

            // After that, supply the file for the local directory.
            // This will take precedence over the AppSettings version of the file, if it exists.
            yield return ConfigStrings.UserConfigJsonFile;
        }
    }
}
