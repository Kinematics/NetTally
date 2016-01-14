using System;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Windows;

namespace NetTally
{
    /// <summary>
    /// Wrapper class for creating/loading/saving user config sections.
    /// </summary>
    public static class NetTallyConfig
    {
        // Keep the configuration file for the duration of the program run.
        static Configuration config = null;

        public static void Load(Tally tally, QuestCollectionWrapper questsWrapper)
        {
            Upgrade();

            config = ConfigurationManager.OpenExeConfiguration(ConfigurationUserLevel.PerUserRoaming);

            QuestsSection questConfig = config.Sections[QuestsSection.DefinedName] as QuestsSection;

            questConfig?.Load(questsWrapper);
        }
        
        private static void Upgrade()
        {
            var conf = ConfigurationManager.OpenExeConfiguration(ConfigurationUserLevel.PerUserRoaming);
            if (conf.HasFile)
                return;

            var map = GetUpgradeMap();

            if (map == null)
                return;

            var upgradeConfig = ConfigurationManager.OpenMappedExeConfiguration(map, ConfigurationUserLevel.PerUserRoaming);

            QuestsSection questConfig = upgradeConfig.Sections[QuestsSection.DefinedName] as QuestsSection;

            if (questConfig == null)
                return;

            QuestCollectionWrapper questWrapper = new QuestCollectionWrapper(null, null);
            questConfig.Load(questWrapper);

            upgradeConfig.SaveAs(conf.FilePath, ConfigurationSaveMode.Full);
        }


        private static ExeConfigurationFileMap GetUpgradeMap()
        {
            var conf = ConfigurationManager.OpenExeConfiguration(ConfigurationUserLevel.PerUserRoaming);
            FileInfo defaultFile = new FileInfo(conf.FilePath);

            var dir = defaultFile.Directory;
            var parent = dir.Parent;

            if (!parent.Exists)
                return null;

            var versionDirectories = parent.EnumerateDirectories("*.*.*.*", SearchOption.TopDirectoryOnly);

            // Get 'newest' directory that is not the one we expect to use
            var latestDir = versionDirectories
                .Where(d => d.Name != dir.Name)
                .Where(d => d.EnumerateFiles().Any(de => de.Name == "user.config"))
                .OrderBy(d => NumSort(d))
                .LastOrDefault();

            if (latestDir == null)
                return null;

            var upgradeFile = Path.Combine(latestDir.FullName, defaultFile.Name);

            ExeConfigurationFileMap map = new ExeConfigurationFileMap();
            map.RoamingUserConfigFilename = upgradeFile;


            conf = ConfigurationManager.OpenExeConfiguration(ConfigurationUserLevel.None);
            map.MachineConfigFilename = conf.FilePath;
            map.ExeConfigFilename = conf.FilePath;

            conf = ConfigurationManager.OpenExeConfiguration(ConfigurationUserLevel.PerUserRoamingAndLocal);
            map.LocalUserConfigFilename = conf.FilePath;

            return map;
        }

        /// <summary>
        /// Provide a sortable number based on the version number of the provided directory.
        /// </summary>
        /// <param name="d">The name of a config directory: 1.2.3.4.</param>
        /// <returns>Returns a numeric value evaluted as the combined numbers of the directory
        /// name (up to a max of 256 per segment).</returns>
        private static int NumSort(DirectoryInfo d)
        {
            // 1.2.3.4
            Regex r = new Regex(@"(?<p1>\d+)\.(?<p2>\d+)\.(?<p3>\d+)\.(?<p4>\d+)");

            Match m = r.Match(d.Name);
            if (m.Success)
            {
                byte p1, p2, p3, p4;
                if (byte.TryParse(m.Groups["p1"].Value, out p1) &&
                    byte.TryParse(m.Groups["p2"].Value, out p2) &&
                    byte.TryParse(m.Groups["p3"].Value, out p3) &&
                    byte.TryParse(m.Groups["p4"].Value, out p4))
                {
                    int sortNumber = p1 << 24 | p2 << 16 | p3 << 8 | p4;
                    return sortNumber;
                }
            }

            return 0;
        }

        public static void Save(Tally tally, QuestCollectionWrapper questsWrapper)
        {
            if (config == null)
                return;
            if (questsWrapper == null)
                return;
            if (questsWrapper.QuestCollection == null)
                return;

            try
            {
                QuestsSection questConfig = config.Sections[QuestsSection.DefinedName] as QuestsSection;
                questConfig.Save(questsWrapper);

                config.Save(ConfigurationSaveMode.Minimal);
            }
            catch (Exception e)
            {
                string file = ErrorLog.Log(e);
                MessageBox.Show($"Log saved to:\n{file ?? "(unable to write log file)"}", "Error saving configuration file", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }

}
