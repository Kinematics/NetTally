using System;
using System.Collections.Specialized;
using System.Configuration;
using System.Linq;

namespace NetTally
{
    /// <summary>
    /// Wrapper class for a collection of quest elements to be added to the user config file.
    /// </summary>
    public class QuestElementCollection : ConfigurationElementCollection
    {
        protected override ConfigurationElement CreateNewElement() => new QuestElement();

        protected override object GetElementKey(ConfigurationElement element)
        {
            if (element is QuestElement qe)
            {
                if (!string.IsNullOrEmpty(qe.ThreadName))
                    return qe.ThreadName;
                return qe.DisplayName;
            }

            throw new ArgumentException("ConfigurationElement is not a QuestElement", nameof(element));
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

        public void Add(Quest quest)
        {
            if (quest == null)
                throw new ArgumentNullException(nameof(quest));

            var questElement = new QuestElement()
            {
                ThreadName = quest.ThreadName,
                DisplayName = quest.DisplayName,
                PostsPerPage = quest.PostsPerPage,
                StartPost = quest.StartPost,
                EndPost = quest.EndPost,
                CheckForLastThreadmark = quest.CheckForLastThreadmark,
                PartitionMode = quest.PartitionMode,
                UseCustomThreadmarkFilters = quest.UseCustomThreadmarkFilters,
                CustomThreadmarkFilters = quest.CustomThreadmarkFilters,
                UseCustomUsernameFilters = quest.UseCustomUsernameFilters,
                CustomUsernameFilters = quest.CustomUsernameFilters,
                UseCustomPostFilters = quest.UseCustomPostFilters,
                CustomPostFilters = quest.CustomPostFilters,
                WhitespaceAndPunctuationIsSignificant = quest.WhitespaceAndPunctuationIsSignificant,
                CaseIsSignificant = quest.CaseIsSignificant,
                ForcePlanReferencesToBeLabeled = quest.ForcePlanReferencesToBeLabeled,
                ForbidVoteLabelPlanNames = quest.ForbidVoteLabelPlanNames,
                AllowUsersToUpdatePlans = quest.AllowUsersToUpdatePlans,
                DisableProxyVotes = quest.DisableProxyVotes,
                ForcePinnedProxyVotes = quest.ForcePinnedProxyVotes,
                IgnoreSpoilers = quest.IgnoreSpoilers,
                TrimExtendedText = quest.TrimExtendedText,
                UseRSSThreadmarks = quest.UseRSSThreadmarks,
                LinkedQuests = quest.LinkedQuestIds.Select(q => q.ToString()).DefaultIfEmpty(string.Empty).Aggregate((p, q) => $"{p}⦂{q}"),
            };

            BaseAdd(questElement, false);
        }

        public void Clear()
        {
            BaseClear();
        }
    }
}
