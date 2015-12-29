﻿using System;
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


    /// <summary>
    /// Class to handle the section for storing quests in the user config file.
    /// Also stores the 'current' quest value.
    /// </summary>
    public class QuestsSection : ConfigurationSection
    {
        public const string DefinedName = "NetTally.Quests";

        [ConfigurationProperty("Quests", IsDefaultCollection = false)]
        [ConfigurationCollection(typeof(QuestElement))]
        public QuestElementCollection Quests
        {
            get { return (QuestElementCollection)this["Quests"]; }
            set { this["Quests"] = value; }
        }

        [ConfigurationProperty("CurrentQuest", DefaultValue=null)]
        public string CurrentQuest
        {
            get { return (string)this["CurrentQuest"]; }
            set { this["CurrentQuest"] = value; }
        }

        [ConfigurationProperty("AllowRankedVotes", DefaultValue = true)]
        public bool AllowRankedVotes
        {
            get { return (bool)this["AllowRankedVotes"]; }
            set { this["AllowRankedVotes"] = value; }
        }

        [ConfigurationProperty("IgnoreSymbols", DefaultValue = true)]
        public bool IgnoreSymbols
        {
            get { return (bool)this["IgnoreSymbols"]; }
            set { this["IgnoreSymbols"] = value; }
        }

        [ConfigurationProperty("TrimExtendedText", DefaultValue = false)]
        public bool TrimExtendedText
        {
            get { return (bool)this["TrimExtendedText"]; }
            set { this["TrimExtendedText"] = value; }
        }

        [ConfigurationProperty("DisplayMode", DefaultValue = DisplayMode.Normal)]
        public DisplayMode DisplayMode
        {
            get {
                try
                {
                    return (DisplayMode)this["DisplayMode"];
                }
                catch (ConfigurationException)
                {
                    return DisplayMode.Normal;
                }
            }
            set { this["DisplayMode"] = value; }
        }

        public void Load(QuestCollectionWrapper questWrapper)
        {
            if (questWrapper.QuestCollection == null)
                questWrapper.QuestCollection = new QuestCollection();

            questWrapper.CurrentQuest = CurrentQuest;
            questWrapper.DisplayMode = DisplayMode;
            questWrapper.AllowRankedVotes = AllowRankedVotes;
            questWrapper.IgnoreSymbols = IgnoreSymbols;
            questWrapper.TrimExtendedText = TrimExtendedText;

            foreach (QuestElement questElement in Quests)
            {
                try
                {
                    IQuest q = new Quest
                    {
                        DisplayName = questElement.DisplayName,
                        ThreadName = questElement.ThreadName,
                        PostsPerPage = questElement.PostsPerPage,
                        StartPost = questElement.StartPost,
                        EndPost = questElement.EndPost,
                        CheckForLastThreadmark = questElement.CheckForLastThreadmark,
                        PartitionMode = questElement.PartitionMode,
                        UseCustomThreadmarkFilters = questElement.UseCustomThreadmarkFilters,
                        CustomThreadmarkFilters = questElement.CustomThreadmarkFilters,
                    };

                    if (questElement.UseVotePartitions && q.PartitionMode == PartitionMode.None)
                    {
                        if (questElement.PartitionByLine)
                            q.PartitionMode = PartitionMode.ByLine;
                        else
                            q.PartitionMode = PartitionMode.ByBlock;
                    }

                    questWrapper.QuestCollection.Add(q);
                }
                catch (Exception)
                {
                    continue;
                }
            }
        }

        public void Save(QuestCollectionWrapper questWrapper)
        {
            CurrentQuest = questWrapper.CurrentQuest;
            DisplayMode = questWrapper.DisplayMode;
            AllowRankedVotes = questWrapper.AllowRankedVotes;
            IgnoreSymbols = questWrapper.IgnoreSymbols;
            TrimExtendedText = questWrapper.TrimExtendedText;

            Quests.Clear();
            foreach (var quest in questWrapper.QuestCollection)
                Quests.Add(quest);
        }
    }

    /// <summary>
    /// Wrapper class for a collection of quest elements to be added to the user config file.
    /// </summary>
    public class QuestElementCollection : ConfigurationElementCollection
    {
        protected override ConfigurationElement CreateNewElement() => new QuestElement();

        protected override object GetElementKey(ConfigurationElement element)
        {
            QuestElement qe = element as QuestElement;
            if (qe.ThreadName != "")
                return qe.ThreadName;
            return qe.DisplayName;
        }

        public new QuestElement this[string name] => (QuestElement)BaseGet(name);

        public QuestElement this[int index]
        {
            get
            {
                return (QuestElement)BaseGet(index);
            }
            set
            {
                if (BaseGet(index) != null)
                {
                    BaseRemoveAt(index);
                }
                BaseAdd(index, value);
            }
        }

        public void Add(IQuest quest)
        {
            var questElement = new QuestElement(quest.ThreadName, quest.DisplayName, quest.PostsPerPage, quest.StartPost, quest.EndPost,
                quest.CheckForLastThreadmark, quest.PartitionMode, quest.UseCustomThreadmarkFilters, quest.CustomThreadmarkFilters);
            BaseAdd(questElement, false);
        }

        public void Clear()
        {
            BaseClear();
        }

    }

    /// <summary>
    /// Class for individual quest entries to be added to the user config file.
    /// </summary>
    public class QuestElement : ConfigurationElement
    {
        public QuestElement(string threadName, string displayName, int postsPerPage, int startPost, int endPost, bool checkForLastThreadmark,
            PartitionMode partitionMode, bool useCustomThreadmarkFilters, string customThreadmarkFilters)
        {
            ThreadName = threadName;
            DisplayName = displayName;
            PostsPerPage = postsPerPage;
            StartPost = startPost;
            EndPost = endPost;
            CheckForLastThreadmark = checkForLastThreadmark;
            PartitionMode = partitionMode;
            UseCustomThreadmarkFilters = useCustomThreadmarkFilters;
            CustomThreadmarkFilters = customThreadmarkFilters;

            // Set it to false to make it disappear.
            AllowRankedVotes = false;
        }

        public QuestElement()
        { }


        [ConfigurationProperty("ThreadName", DefaultValue = "", IsKey = true)]
        public string ThreadName
        {
            get { return (string)this["ThreadName"]; }
            set { this["ThreadName"] = value; }
        }

        [ConfigurationProperty("DisplayName", DefaultValue = "")]
        public string DisplayName
        {
            get { return (string)this["DisplayName"]; }
            set { this["DisplayName"] = value; }
        }

        [ConfigurationProperty("PostsPerPage", DefaultValue = 0)]
        public int PostsPerPage
        {
            get { return (int)this["PostsPerPage"]; }
            set { this["PostsPerPage"] = value; }
        }

        [ConfigurationProperty("StartPost", DefaultValue = 1, IsRequired = true)]
        public int StartPost
        {
            get { return (int)this["StartPost"]; }
            set { this["StartPost"] = value; }
        }

        [ConfigurationProperty("EndPost", DefaultValue = 0, IsRequired = true)]
        public int EndPost
        {
            get { return (int)this["EndPost"]; }
            set { this["EndPost"] = value; }
        }

        [ConfigurationProperty("CheckForLastThreadmark", DefaultValue = false)]
        public bool CheckForLastThreadmark
        {
            get { return (bool)this["CheckForLastThreadmark"]; }
            set { this["CheckForLastThreadmark"] = value; }
        }

        [ConfigurationProperty("PartitionMode", DefaultValue = PartitionMode.None)]
        public PartitionMode PartitionMode
        {
            get
            {
                try
                {
                    return (PartitionMode)this["PartitionMode"];
                }
                catch (ConfigurationException)
                {
                    return PartitionMode.None;
                }
            }
            set { this["PartitionMode"] = value; }
        }

        [ConfigurationProperty("AllowRankedVotes", DefaultValue = false)]
        public bool AllowRankedVotes
        {
            get { return (bool)this["AllowRankedVotes"]; }
            set { this["AllowRankedVotes"] = value; }
        }

        [ConfigurationProperty("UseCustomThreadmarkFilters", DefaultValue = false)]
        public bool UseCustomThreadmarkFilters
        {
            get { return (bool)this["UseCustomThreadmarkFilters"]; }
            set { this["UseCustomThreadmarkFilters"] = value; }
        }

        [ConfigurationProperty("CustomThreadmarkFilters", DefaultValue = "")]
        public string CustomThreadmarkFilters
        {
            get { return (string)this["CustomThreadmarkFilters"]; }
            set { this["CustomThreadmarkFilters"] = value; }
        }


        // Obsolete configuration properties.  Left in place solely so that if they
        // exist, they don't cause load errors.  We do not save the results when
        // saving a new config instance.

        [ConfigurationProperty("UseVotePartitions", DefaultValue = false)]
        public bool UseVotePartitions
        {
            get { return (bool)this["UseVotePartitions"]; }
            set { this["UseVotePartitions"] = value; }
        }

        [ConfigurationProperty("PartitionByLine", DefaultValue = true)]
        public bool PartitionByLine
        {
            get { return (bool)this["PartitionByLine"]; }
            set { this["PartitionByLine"] = value; }
        }

    }

}