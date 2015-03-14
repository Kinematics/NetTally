using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NetTally
{
    /// <summary>
    /// Wrapper class for creating/loading/saving user config sections.
    /// </summary>
    public static class NetTallyConfig
    {
        static Configuration config = null;
        public static void Load(Tally tally, QuestCollectionWrapper questsWrapper)
        {
            config = ConfigurationManager.OpenExeConfiguration(ConfigurationUserLevel.PerUserRoaming);

            QuestsSection questConfig = config.Sections[QuestsSection.DefinedName] as QuestsSection;
            if (questConfig == null)
            {
                questConfig = new QuestsSection();
                questConfig.SectionInformation.AllowExeDefinition = ConfigurationAllowExeDefinition.MachineToLocalUser;
                config.Sections.Add(QuestsSection.DefinedName, questConfig);
            }

            questConfig.Load(questsWrapper);
        }

        public static void Save(Tally tally, QuestCollectionWrapper questsWrapper)
        {
            if (config == null)
                return;

            QuestsSection questConfig = config.Sections[QuestsSection.DefinedName] as QuestsSection;
            questConfig.Save(questsWrapper);

            config.Save(ConfigurationSaveMode.Minimal);
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

        public void Load(QuestCollectionWrapper questWrapper)
        {
            if (questWrapper.QuestCollection == null)
                questWrapper.QuestCollection = new QuestCollection();

            questWrapper.CurrentQuest = CurrentQuest;

            foreach (QuestElement quest in Quests)
            {
                IQuest q = new Quest()
                {
                    Site = quest.Site,
                    Name = quest.Name,
                    StartPost = quest.StartPost,
                    EndPost = quest.EndPost,
                    CheckForLastThreadmark = quest.CheckForLastThreadmark,
                    UseVotePartitions = quest.UseVotePartitions,
                    PartitionByLine = quest.PartitionByLine
                };
                questWrapper.QuestCollection.Add(q);
            }
        }

        public void Save(QuestCollectionWrapper questWrapper)
        {
            CurrentQuest = questWrapper.CurrentQuest;

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
        public QuestElementCollection()
        {

        }

        protected override ConfigurationElement CreateNewElement()
        {
            return new QuestElement();
        }

        protected override object GetElementKey(ConfigurationElement element)
        {
            return ((QuestElement)element).Name;
        }

        public new QuestElement this[string name]
        {
            get { return (QuestElement)base.BaseGet(name); }
        }

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
            var questElement = new QuestElement(quest.Site, quest.Name, quest.StartPost, quest.EndPost,
                quest.CheckForLastThreadmark, quest.UseVotePartitions, quest.PartitionByLine);
            BaseAdd(questElement);
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
        public QuestElement(string site, string name, int startPost, int endPost, bool checkForLastThreadmark,
            bool useVotePartitions, bool partitionByLine)
        {
            Site = site;
            Name = name;
            StartPost = startPost;
            EndPost = endPost;
            CheckForLastThreadmark = checkForLastThreadmark;
            UseVotePartitions = useVotePartitions;
            PartitionByLine = partitionByLine;
        }

        public QuestElement()
        { }


        [ConfigurationProperty("Site", DefaultValue = "", IsKey = true)]
        public string Site
        {
            get { return (string)this["Site"]; }
            set { this["Site"] = value; }
        }

        [ConfigurationProperty("Name", DefaultValue = "Name", IsRequired = true, IsKey = true)]
        public string Name
        {
            get { return (string)this["Name"]; }
            set { this["Name"] = value; }
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
