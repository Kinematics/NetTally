using System.Collections.Generic;
using System.Configuration;
using NetTally.Collections;
using NetTally.Config;
using NetTally.Options;

namespace NetTally
{
    public static class ConfigPrefs
    {
        public static bool Strict = false;
    }

    /// <summary>
    /// Class for loading and saving program configuration information.
    /// </summary>
    public static class NetTallyConfig
    {
        #region Loading
        /// <summary>
        /// Loads the program user data config and returns a collection of
        /// quests.
        /// </summary>
        /// <returns>Returns the quests wrapper to store data in.</returns>
        public static void Load(out QuestCollection quests, out string? currentQuest, AdvancedOptions? options)
        {
            List<Configuration> configs = NetTallyConfigHelper.GetConfigsToLoadFrom();

            ReadConfigInformation(configs, out quests, out currentQuest, options);
        }

        /// <summary>
        /// Reads the configuration information from the provided configuration object into
        /// the provided quests collection wrapper.
        /// </summary>
        /// <param name="configs">The list of configuration objects to attempt to read.</param>
        /// <param name="quests">The quests wrapper to store data in.</param>
        private static void ReadConfigInformation(List<Configuration> configs, out QuestCollection quests, out string? currentQuest, AdvancedOptions? options)
        {
            if (configs.Count > 0)
            {
                ConfigurationException? failure = null;

                ConfigPrefs.Strict = true;

                while (true)
                {
                    foreach (var config in configs)
                    {
                        try
                        {
                            if (config.Sections[QuestsSection.SectionName] is QuestsSection questsSection)
                            {
                                questsSection.Load(out quests, out currentQuest, options);
                                return;
                            }
                        }
                        catch (ConfigurationException e)
                        {
                            failure = e;
                        }
                    }

                    ConfigPrefs.Strict = !ConfigPrefs.Strict;

                    if (ConfigPrefs.Strict)
                        break;
                }

                // If all config files generated an error, throw the last one we got.
                if (failure != null)
                    throw failure;
            }

            // If nothing was loaded, just provide default values.
            quests = new QuestCollection();
            currentQuest = null;
        }
        #endregion

        #region Saving
        /// <summary>
        /// Saves the data from the specified quests wrapper into the config file.
        /// Tries to save in both the portable location (same directory as the executable),
        /// and to the roaming location (AppData).
        /// The portable location is transferable between computers without needing
        /// a corresponding user account, while the roaming location keeps data from
        /// being lost if you unzip a new version of the program to a differently-named
        /// folder.
        /// </summary>
        /// <param name="quests">The quests wrapper.</param>
        public static void Save(QuestCollection quests, string currentQuest, AdvancedOptions options)
        {
            // If there's nothing to save, don't do anything.
            if (quests == null)
                return;

            // Write to each config location (portable and roaming)
            List<Configuration> configs = NetTallyConfigHelper.GetConfigsToWriteTo();

            foreach (var config in configs)
            {
                WriteConfigInformation(config, quests, currentQuest, options);
            }
        }

        /// <summary>
        /// Writes the data from the provided quests wrapper object into the specified configuration file.
        /// </summary>
        /// <param name="quests">The quests wrapper with program data.</param>
        /// <param name="config">The configuration file to save to.</param>
        private static void WriteConfigInformation(Configuration config, QuestCollection quests, string currentQuest, AdvancedOptions options)
        {
            try
            {
                ConfigPrefs.Strict = false;

                if (config.Sections[QuestsSection.SectionName] is QuestsSection questsSection)
                {
                    questsSection.Save(quests, currentQuest, options);
                }

                config.Save(ConfigurationSaveMode.Minimal);
            }
            catch (ConfigurationErrorsException e)
            {
                // May not have permission to write, or the original config may have errors.
            }
        }
        #endregion
    }


}
