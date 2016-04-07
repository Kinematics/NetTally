using System;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Windows;

namespace NetTally
{
    /// <summary>
    /// Class for loading and saving program configuration information.
    /// </summary>
    public static class NetTallyConfig
    {
        #region Loading and Saving        
        /// <summary>
        /// Loads the program user data config and returns a collection of
        /// quests.
        /// </summary>
        /// <returns>Returns the quests wrapper to store data in.</returns>
        public static QuestCollectionWrapper Load()
        {
            QuestCollectionWrapper questsWrapper = new QuestCollectionWrapper();

            Configuration config = GetConfigToLoadFrom();

            ReadConfigInformation(config, questsWrapper);

            return questsWrapper;
        }

        /// <summary>
        /// Reads the configuration information from the provided configuration object into
        /// the provided quests collection wrapper.
        /// </summary>
        /// <param name="config">The configuration object to read.</param>
        /// <param name="questsWrapper">The quests wrapper to store data in.</param>
        private static void ReadConfigInformation(Configuration config, QuestCollectionWrapper questsWrapper)
        {
            QuestsSection questsSection = config.Sections[QuestsSection.SectionName] as QuestsSection;

            questsSection?.Load(questsWrapper);
        }

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
            if (questsWrapper == null)
                return;
            if (questsWrapper.QuestCollection == null)
                return;

            try
            {
                // Write the config to the portable location.
                Configuration config = GetPortableConfig();
                WriteConfigInformation(questsWrapper, config);
            }
            catch (ConfigurationErrorsException)
            {
                // Don't have permission to write the portable config output.
            }
            catch (Exception e)
            {
                string file = ErrorLog.Log(e);
                MessageBox.Show($"Log saved to:\n{file ?? "(unable to write log file)"}", "Error saving configuration file", MessageBoxButton.OK, MessageBoxImage.Error);
            }

            try
            {
                // Write the config to the roaming location.
                Configuration config = GetRoamingConfig();
                WriteConfigInformation(questsWrapper, config);
            }
            catch (ConfigurationErrorsException)
            {
                // Don't have permission to write to the roaming location.
            }
            catch (Exception e)
            {
                string file = ErrorLog.Log(e);
                MessageBox.Show($"Log saved to:\n{file ?? "(unable to write log file)"}", "Error saving configuration file", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// Writes the data from the provided quests wrapper object into the specified configuration file.
        /// </summary>
        /// <param name="questsWrapper">The quests wrapper with program data.</param>
        /// <param name="config">The configuration file to save to.</param>
        private static void WriteConfigInformation(QuestCollectionWrapper questsWrapper, Configuration config)
        {
            QuestsSection questSection = config.Sections[QuestsSection.SectionName] as QuestsSection;
            questSection?.Save(questsWrapper);

            config.Save(ConfigurationSaveMode.Minimal);
        }
        #endregion

        #region Getting Configs        
        /// <summary>
        /// Gets the configuration object with program config data to load on startup.
        /// </summary>
        /// <returns>Returns the portable config, if available.
        /// Returns the roaming config if there is no portable config, and the roaming config exists.
        /// Returns the portable config object if no config file was found.</returns>
        private static Configuration GetConfigToLoadFrom()
        {
            var portableConfig = GetPortableConfig();

            if (portableConfig.HasFile)
                return portableConfig;

            Configuration roamingConfig = GetRecentRoamingConfig();

            if (roamingConfig.HasFile)
                return roamingConfig;

            return portableConfig;
        }

        /// <summary>
        /// Gets the portable configuration.
        /// </summary>
        /// <returns>Returns the portable config object after determining where it should be located.</returns>
        private static Configuration GetPortableConfig()
        {
            ExeConfigurationFileMap map = GetPortableMap();
            Configuration config = ConfigurationManager.OpenMappedExeConfiguration(map, ConfigurationUserLevel.PerUserRoaming);

            return config;
        }

        /// <summary>
        /// Gets the roaming configuration.
        /// </summary>
        /// <returns>Returns the roaming config object from the default location.</returns>
        private static Configuration GetRoamingConfig()
        {
            Configuration config = ConfigurationManager.OpenExeConfiguration(ConfigurationUserLevel.PerUserRoaming);

            return config;
        }

        /// <summary>
        /// Gets the recent roaming configuration.
        /// </summary>
        /// <returns>Returns the current roaming config if it has a file.
        /// Returns the most recent roaming config it can find, if the current one does not exist.</returns>
        private static Configuration GetRecentRoamingConfig()
        {
            Configuration config = GetRoamingConfig();

            if (config.HasFile)
                return config;

            ExeConfigurationFileMap map = GetMapToMostRecentRoamingConfig(config);

            if (map != null)
                config = ConfigurationManager.OpenMappedExeConfiguration(map, ConfigurationUserLevel.PerUserRoaming);

            return config;
        }

        #endregion

        #region Getting Config Maps        
        /// <summary>
        /// Gets the config map for the portable config file.
        /// </summary>
        /// <returns>Returns the config map for the portable config file.</returns>
        private static ExeConfigurationFileMap GetPortableMap()
        {
            string portableConfigPath = Path.Combine(Environment.CurrentDirectory, "user.config");
            return GetMapWithUserPath(portableConfigPath);
        }

        /// <summary>
        /// Gets the map to the most recent roaming configuration file that can be located.
        /// </summary>
        /// <param name="config">The default configuration location for the current program version.</param>
        /// <returns>Returns a configuration map file if a recent file can be located.
        /// If no file can be located, returns null.</returns>
        private static ExeConfigurationFileMap GetMapToMostRecentRoamingConfig(Configuration config)
        {
            FileInfo defaultFile = new FileInfo(config.FilePath);

            var defaultDir = defaultFile.Directory;
            var parent = defaultDir.Parent;

            if (!parent.Exists)
                return null;

            var versionDirectories = parent.EnumerateDirectories("*.*.*.*", SearchOption.TopDirectoryOnly);

            var dirs = from dir in versionDirectories
                       where dir.Name != defaultDir.Name &&
                          dir.EnumerateFiles().Any(de => de.Name == "user.config")
                       let v = DirectoryVersion(dir)
                       where v.Major > 0 && v <= ProductInfo.AssemblyVersion
                       orderby v
                       select dir;

            var latestDir = dirs.LastOrDefault();
            if (latestDir == null)
                return null;

            var mostRecentFile = Path.Combine(latestDir.FullName, "user.config");

            return GetMapWithUserPath(mostRecentFile);
        }

        /// <summary>
        /// Gets a configuration file map that uses the provided user path for the local and roaming paths.
        /// </summary>
        /// <param name="userPath">The path to use for user config file locations.</param>
        /// <returns>Returns a configuration file map using the provided user path.</returns>
        private static ExeConfigurationFileMap GetMapWithUserPath(string userPath)
        {
            ExeConfigurationFileMap map = new ExeConfigurationFileMap();

            ConfigurationFileMap machineMap = new ConfigurationFileMap();
            Configuration config = ConfigurationManager.OpenExeConfiguration(ConfigurationUserLevel.None);

            map.MachineConfigFilename = machineMap.MachineConfigFilename;
            map.ExeConfigFilename = config.FilePath;
            map.LocalUserConfigFilename = userPath;
            map.RoamingUserConfigFilename = userPath;

            return map;
        }

        /// <summary>
        /// Returns a Version object based on the name of the provided directory.
        /// </summary>
        /// <param name="d">The directory.</param>
        /// <returns>Returns a version based on the directory name, or the default.</returns>
        private static Version DirectoryVersion(DirectoryInfo d)
        {
            try
            {
                return new Version(d.Name);
            }
            catch (Exception)
            {
                return new Version();
            }
        }
        #endregion
    }
}
