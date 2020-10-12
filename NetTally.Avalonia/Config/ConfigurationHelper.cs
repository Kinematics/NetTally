using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Linq;
using NetTally.SystemInfo;

namespace NetTally.Avalonia.Config
{
    /// <summary>
    /// Helper class to get the configs to use for loading and saving configuration information.
    /// </summary>
    public static class NetTallyConfigHelper
    {
        #region Public methods
        /// <summary>
        /// Gets the configuration object with program config data to load on startup.
        /// First tries the local directory, for portable use.
        /// Next tries the roaming directory, without the program hash.
        /// Next tries the roaming directory, with the program hash.
        /// If none are found, returns the local directory configuration.
        /// </summary>
        /// <returns>Returns the Configuration object for the program.</returns>
        public static List<Configuration> GetConfigsToLoadFrom()
        {
            List<Configuration> configs = new List<Configuration>();

            Configuration portableConfig = GetPortableConfig();

            if (portableConfig.HasFile)
                configs.Add(portableConfig);

            Configuration currentConfig = GetCurrentRoamingConfig();

            if (currentConfig.HasFile)
            {
                configs.Add(currentConfig);
            }
            else
            {
                Configuration roamingConfig = GetRecentRoamingConfig();

                if (roamingConfig.HasFile)
                    configs.Add(roamingConfig);
            }

            return configs;
        }

        /// <summary>
        /// Gets a list of configs that can be written to.
        /// </summary>
        /// <returns>Returns a list of Configurations to be written to.</returns>
        public static List<Configuration> GetConfigsToWriteTo()
        {
            return new List<Configuration> { GetPortableConfig(), GetCurrentRoamingConfig() };
        }
        #endregion

        #region Configs which can be used.
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
        /// Gets the current roaming configuration.
        /// </summary>
        /// <returns>Returns the currrent roaming config object from the default location.</returns>
        private static Configuration GetCurrentRoamingConfig()
        {
            ExeConfigurationFileMap map = GetCurrentRoamingMap();

            Configuration config = ConfigurationManager.OpenMappedExeConfiguration(map, ConfigurationUserLevel.PerUserRoaming);

            return config;
        }

        /// <summary>
        /// Gets the recent roaming configuration, for the sake of upgrades.
        /// </summary>
        /// <returns>Returns the most recent roaming config it can find.</returns>
        private static Configuration GetRecentRoamingConfig()
        {
            ExeConfigurationFileMap? map = GetRecentRoamingMap();

            Configuration config = ConfigurationManager.OpenMappedExeConfiguration(map, ConfigurationUserLevel.PerUserRoaming);

            return config;
        }
        #endregion

        #region Getting Config File Maps        
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
        /// Gets the map for the current (unhashed) roaming configuration directory.
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
        /// Searches the unhashed location, and then searches the hash directory location.
        /// Searches for a directory version no higher than the current assembly version.
        /// </summary>
        /// <returns>Returns the most recent findable roaming config file.</returns>
        private static ExeConfigurationFileMap? GetRecentRoamingMap()
        {
            Configuration defaultConfig = ConfigurationManager.OpenExeConfiguration(ConfigurationUserLevel.PerUserRoaming);
            FileInfo defaultFile = new FileInfo(defaultConfig.FilePath);

            var defaultDir = defaultFile.Directory;
            var hashParent = defaultDir.Parent;
            var noHashParent = hashParent.Parent;

            // If no product directory exists, NetTally has never been run before, and there's nothing to look for.
            if (!noHashParent.Exists)
                return null;

            string product = GetProductDirectory();

            DirectoryInfo? dir = null;

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

        /// <summary>
        /// Gets a configuration file map that uses the provided user path for the local and roaming paths.
        /// </summary>
        /// <param name="userPath">The path to use for user config file locations.</param>
        /// <returns>Returns a configuration file map using the provided user path.</returns>
        private static ExeConfigurationFileMap GetMapWithUserPath(string userPath)
        {
            ExeConfigurationFileMap map = new ExeConfigurationFileMap();

            ConfigurationFileMap machineMap = new ConfigurationFileMap();
            map.MachineConfigFilename = machineMap.MachineConfigFilename;

            Configuration config = ConfigurationManager.OpenExeConfiguration(ConfigurationUserLevel.None);
            map.ExeConfigFilename = config.FilePath;

            map.LocalUserConfigFilename = userPath;
            map.RoamingUserConfigFilename = userPath;

            return map;
        }
        #endregion

        #region Directory utility functions
        /// <summary>
        /// Gets the directory with the highest version number that is not higher than
        /// the current assembly version, and contains a user.config file in it.
        /// </summary>
        /// <param name="parent">The parent directory being searched.</param>
        /// <returns>Returns the best directory match it can find, or null.</returns>
        private static DirectoryInfo? GetLatestVersionDirectory(DirectoryInfo parent)
        {
            if (!parent.Exists)
                return null;

            var versionDirectories = parent.EnumerateDirectories("*.*.*.*", SearchOption.TopDirectoryOnly);

            var dirs = from dir in versionDirectories
                       where dir.EnumerateFiles().Any(de => de.Name == "user.config")
                       let v = GetDirectoryVersion(dir)
                       where v.Major > 0 && v <= ProductInfo.AssemblyVersion
                       orderby v
                       select dir;

            return dirs.LastOrDefault();
        }

        /// <summary>
        /// Returns a Version object based on the name of the provided directory.
        /// If the directory is not in a version format (eg: 1.2.3.4), returns the default version.
        /// </summary>
        /// <param name="dir">The directory.</param>
        /// <returns>Returns a version based on the directory name, or the default.</returns>
        private static Version GetDirectoryVersion(DirectoryInfo dir)
        {
            try
            {
                return new Version(dir.Name);
            }
            catch (Exception)
            {
                return new Version();
            }
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
        #endregion

    }
}
