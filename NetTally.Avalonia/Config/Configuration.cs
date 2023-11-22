using System.Collections.Generic;
using System.Configuration;
using System.Diagnostics.CodeAnalysis;
using NetTally.Collections;
using NetTally.Options;

namespace NetTally.Avalonia.Config
{
    /// <summary>
    /// Class for loading and saving program configuration information.
    /// </summary>
    public class LegacyNetTallyConfig
    {
        #region Loading
        /// <summary>
        /// Loads the program user data config and returns a collection of
        /// quests.
        /// </summary>
        /// <returns>Returns the quests wrapper to store data in.</returns>
        public static bool Load(
            [NotNullWhen(true)] out QuestCollection? quests,
            out string? currentQuest,
            AdvancedOptions? options)
        {
            List<Configuration> configs = NetTallyConfigHelper.GetConfigsToLoadFrom();

            if (configs.Count == 0)
            {
                quests = null;
                currentQuest = null;
                return false;
            }

            return ReadConfigInformation(configs, out quests, out currentQuest, options);
        }

        /// <summary>
        /// Reads the configuration information from the provided configuration object into
        /// the provided quests collection wrapper.
        /// </summary>
        /// <param name="configs">The list of configuration objects to attempt to read.</param>
        /// <param name="quests">The quests wrapper to store data in.</param>
        private static bool ReadConfigInformation(
            List<Configuration> configs,
            out QuestCollection? quests,
            out string? currentQuest,
            AdvancedOptions? options)
        {
            if (configs.Count > 0)
            {
                ConfigurationException? failure = null;

                bool[] toggles = [true, false];

                foreach (var toggle in toggles)
                {
                    // Try both strict and not strict.
                    ConfigPrefs.Strict = toggle;

                    foreach (var config in configs)
                    {
                        try
                        {
                            if (config.Sections[ConfigStrings.SectionName] is QuestsSection questsSection)
                            {
                                questsSection.Load(out quests, out currentQuest, options);
                                return true;
                            }
                        }
                        catch (ConfigurationException e)
                        {
                            failure = e;
                        }
                    }
                }

                // If all config files failed to load, and there were any errors,
                // throw the last error we got.
                if (failure != null)
                    throw failure;
            }

            // If nothing was loaded, just provide default values.
            quests = null;
            currentQuest = null;

            return false;
        }
        #endregion
    }
}
