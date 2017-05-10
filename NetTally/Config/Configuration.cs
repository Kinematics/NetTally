using System.Collections.Generic;
using System.Configuration;
using NetTally.Collections;
using NetTally.Config;

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
        public static QuestCollectionWrapper Load()
        {
            QuestCollectionWrapper questsWrapper = new QuestCollectionWrapper();

            List<Configuration> configs = NetTallyConfigHelper.GetConfigsToLoadFrom();

            ReadConfigInformation(configs, questsWrapper);

            return questsWrapper;
        }

        /// <summary>
        /// Reads the configuration information from the provided configuration object into
        /// the provided quests collection wrapper.
        /// </summary>
        /// <param name="configs">The list of configuration objects to attempt to read.</param>
        /// <param name="questsWrapper">The quests wrapper to store data in.</param>
        private static void ReadConfigInformation(List<Configuration> configs, QuestCollectionWrapper questsWrapper)
        {
            if (configs.Count == 0)
                return;

            ConfigurationException failure = null;

            ConfigPrefs.Strict = true;

            while (true)
            {
                foreach (var config in configs)
                {
                    try
                    {
                        if (config.Sections[QuestsSection.SectionName] is QuestsSection questsSection)
                        {
                            questsSection.Load(questsWrapper);
                        }

                        // End as soon as done successfully
                        return;
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
        /// <param name="questsWrapper">The quests wrapper.</param>
        public static void Save(QuestCollectionWrapper questsWrapper)
        {
            // If there's nothing to save, don't do anything.
            if (questsWrapper == null)
                return;
            if (questsWrapper.QuestCollection == null)
                return;

            // Write to each config location (portable and roaming)
            List<Configuration> configs = NetTallyConfigHelper.GetConfigsToWriteTo();

            foreach (var config in configs)
            {
                WriteConfigInformation(questsWrapper, config);
            }
        }

        /// <summary>
        /// Writes the data from the provided quests wrapper object into the specified configuration file.
        /// </summary>
        /// <param name="questsWrapper">The quests wrapper with program data.</param>
        /// <param name="config">The configuration file to save to.</param>
        private static void WriteConfigInformation(QuestCollectionWrapper questsWrapper, Configuration config)
        {
            try
            {
                ConfigPrefs.Strict = false;

                if (config.Sections[QuestsSection.SectionName] is QuestsSection questsSection)
                {
                    questsSection.Save(questsWrapper);
                }

                config.Save(ConfigurationSaveMode.Minimal);
            }
            catch (ConfigurationErrorsException)
            {
                // May not have permission to write, or the original config may have errors.
            }
        }
        #endregion
    }


}
