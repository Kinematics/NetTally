using System;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Windows;
using NetTally.Collections;
using NetTally.Utility;

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
            if (config.Sections[QuestsSection.SectionName] is QuestsSection questsSection)
            {
                questsSection.Load(questsWrapper);
            }
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
                Configuration config = GetCurrentRoamingConfig();
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
            if (config.Sections[QuestsSection.SectionName] is QuestsSection questsSection)
            {
                questsSection.Save(questsWrapper);
            }

            config.Save(ConfigurationSaveMode.Minimal);
        }
        #endregion

        #region Getting Configs        
        /// <summary>
        /// Gets the configuration object with program config data to load on startup.
        /// First tries the local directory, for portable use.
        /// Next tries the roaming directory, without the program hash.
        /// Next tries the roaming directory, with the program hash.
        /// If none are found, returns the local directory configuration.
        /// </summary>
        /// <returns>Returns the Configuration object for the program.</returns>
        private static Configuration GetConfigToLoadFrom()
        {
            Configuration portableConfig = GetPortableConfig();

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
        /// Gets the roaming configuration to save to.
        /// </summary>
        /// <returns>Returns the roaming config object from the default location.</returns>
        private static Configuration GetCurrentRoamingConfig()
        {
            ExeConfigurationFileMap map = GetCurrentRoamingMap();

            Configuration config = ConfigurationManager.OpenMappedExeConfiguration(map, ConfigurationUserLevel.PerUserRoaming);

            return config;
        }

        /// <summary>
        /// Gets the recent roaming configuration.
        /// </summary>
        /// <returns>Returns the current roaming config if it has a file.
        /// Returns the most recent roaming config it can find, if the current one does not exist.</returns>
        private static Configuration GetRecentRoamingConfig()
        {
            Configuration config = GetCurrentRoamingConfig();

            if (config.HasFile)
                return config;

            ExeConfigurationFileMap map = GetRecentRoamingMap();

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
        /// Gets the map for the current (fixed) roaming configuration directory.
        /// </summary>
        /// <returns>Returns the config map for the roaming config file.</returns>
        private static ExeConfigurationFileMap GetCurrentRoamingMap()
        {
            Configuration defaultConfig = ConfigurationManager.OpenExeConfiguration(ConfigurationUserLevel.PerUserRoaming);
            FileInfo defaultFile = new FileInfo(defaultConfig.FilePath);
            // Default: Roaming\Wayward_Gamers\NetTally.exe_Url_<hash>\1.7.0.0\user.config
            // Change to: Roaming\Wayward_Gamers\NetTally\1.7.0.0\user.config

            var companyDirectory = defaultFile.Directory.Parent.Parent;

            string product = GetProductDirectory();

            var configFile = Path.Combine(companyDirectory.FullName, product, ProductInfo.AssemblyVersion.ToString(), "user.config");

            return GetMapWithUserPath(configFile);
        }

        /// <summary>
        /// Gets the map for the most recent findable roaming configuration directory.
        /// Searches the fixed location, and then searches the hash directory location.
        /// Searches for a directory version no higher than the current assembly version.
        /// </summary>
        /// <returns>Returns the most recent findable roaming config file.</returns>
        private static ExeConfigurationFileMap GetRecentRoamingMap()
        {
            Configuration defaultConfig = ConfigurationManager.OpenExeConfiguration(ConfigurationUserLevel.PerUserRoaming);
            FileInfo defaultFile = new FileInfo(defaultConfig.FilePath);

            var defaultDir = defaultFile.Directory;
            var hashParent = defaultDir.Parent;
            var noHashParent = hashParent.Parent;

            string product = GetProductDirectory();

            DirectoryInfo dir = null;

            var productDir = noHashParent.EnumerateDirectories().FirstOrDefault(d => d.Name == product);

            if (productDir != null)
            {
                dir = GetLatestVersionDirectory(productDir);
            }

            if (dir == null)
            {
                dir = GetLatestVersionDirectory(hashParent);
            }

            if (dir != null)
            {
                var mostRecentFile = Path.Combine(dir.FullName, "user.config");

                return GetMapWithUserPath(mostRecentFile);
            }

            return null;
        }
        #endregion

        #region Mapping utility functions.
        /// <summary>
        /// Gets the directory with the highest version number that is not higher than
        /// the current assembly version, and contains a user.config file in it.
        /// </summary>
        /// <param name="parent">The parent directory.</param>
        /// <returns>Returns the best directory match it can find, or null.</returns>
        private static DirectoryInfo GetLatestVersionDirectory(DirectoryInfo parent)
        {
            if (!parent.Exists)
                return null;

            var versionDirectories = parent.EnumerateDirectories("*.*.*.*", SearchOption.TopDirectoryOnly);

            var dirs = from dir in versionDirectories
                       where dir.EnumerateFiles().Any(de => de.Name == "user.config")
                       let v = DirectoryVersion(dir)
                       where v.Major > 0 && v <= ProductInfo.AssemblyVersion
                       orderby v
                       select dir;

            return dirs.LastOrDefault();
        }

        /// <summary>
        /// Gets the product directory name, with adjustments if we're running in debug mode.
        /// </summary>
        /// <returns>The product directory name.</returns>
        private static string GetProductDirectory()
        {
            string product = ProductInfo.Name;
#if DEBUG
            product = $"{product}.Debug";
#endif
            return product;
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
