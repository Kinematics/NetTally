﻿using System;
using System.Text.RegularExpressions;
using NetTally.Forums;
using NetTally.Utility;
using NetTally.Votes;
using System.Collections.Generic;
using NetTally.Input.Utility;
using NetTally.Collections;
using System.Linq;
using System.Runtime.CompilerServices;
using NetTally.Types.Enums;

namespace NetTally
{
    /// <summary>
    /// A Quest is a named forum thread, along with all the configuration properties that
    /// are used to determine how to go about tallying that thread.
    /// </summary>
    public partial class Quest : IQuest
    {
        public const string OmakeFilter = @"\bomake\b";

        public Quest()
        {
            questHash = indexer.Next();
            ThreadName = NewThreadEntry;

            CustomThreadmarkFilters = string.Empty;
            CustomTaskFilters = string.Empty;
            CustomUsernameFilters = string.Empty;
        }

        #region Hashing
        // Quest hash is used to set the hash code for this object.
        // Since all other intrinsic values are mutable, it is set to 
        // an immutable random value.
        static readonly Random indexer = new Random();
        private readonly int questHash;
        #endregion

        #region Linked Quests
        /// <summary>
        /// A collection of linked quests that should be tallied alongside this one.
        /// </summary>
        public QuestCollection LinkedQuests { get; } = new QuestCollection();

        /// <summary>
        /// Check if the given quest is one of the linked quests.
        /// </summary>
        /// <param name="quest">The quest to check on.</param>
        /// <returns>Returns true if this quest has the given quest in its links.</returns>
        public bool HasLinkedQuest(IQuest quest)
        {
            foreach (var linkedQuest in LinkedQuests)
            {
                if (linkedQuest == quest)
                    return true;
            }

            return false;
        }

        /// <summary>
        /// Check if the quest with the given name is one of the linked quests.
        /// </summary>
        /// <param name="questName">The name of the quest to check on.</param>
        /// <returns>Returns true if this quest has the given quest in its links.</returns>
        public bool HasLinkedQuest(string questName)
        {
            foreach (var quest in LinkedQuests)
            {
                if (quest.DisplayName == questName)
                    return true;
            }

            return false;
        }

        /// <summary>
        /// Add the provided quest to the list of links this quest has.
        /// </summary>
        /// <param name="quest">The quest to add.</param>
        public void AddLinkedQuest(IQuest quest)
        {
            if ((quest as Quest) == this)
                return;

            LinkedQuests.Add(quest);
            quest.LinkedQuests.Add(this);
            OnPropertyChanged(nameof(LinkedQuests));
        }

        /// <summary>
        /// Remove the provided quest from the list of quests this quest is linked to.
        /// </summary>
        /// <param name="quest">The quest to remove.</param>
        /// <returns>Returns true if the quest was found and removed.</returns>
        public bool RemoveLinkedQuest(IQuest quest)
        {
            return RemoveLinkedQuestImpl(quest as Quest);
        }

        /// <summary>
        /// Implementation of RemoveLinkedQuest, to allow this quest to tell the
        /// linked quest to also remove itself from the other quest.
        /// </summary>
        /// <param name="quest">The quest to remove.</param>
        /// <param name="callerName">The name of the function that called this one.</param>
        /// <returns>Returns true if the quest was found and removed.</returns>
        private bool RemoveLinkedQuestImpl(Quest? quest, [CallerMemberName] string callerName = "")
        {
            if (quest != null)
            {
                if (LinkedQuests.Remove(quest))
                {
                    if (callerName != nameof(RemoveLinkedQuestImpl))
                    {
                        quest.RemoveLinkedQuestImpl(this);
                        OnPropertyChanged(nameof(LinkedQuests));
                    }

                    return true;
                }
            }

            return false;
        }
        #endregion Linked Quests


        #region URL and Display Name
        string threadName = string.Empty;
        string displayName = string.Empty;
        static readonly Regex pageNumberRegex = new Regex(@"^(?<base>.+?)(&?page[-=]?\d+)?(&p=?\d+)?(#[^/]*)?(unread)?$");
        static readonly Regex displayNameRegex = new Regex(@"(?<displayName>[^/]+)(/|#[^/]*)?$");
        public const string NewThreadEntry = "https://www.example.com/threads/fake-thread.00000";
        public static readonly Uri InvalidThreadUri = new Uri(NewThreadEntry);

        /// <summary>
        /// The URI that represents the thread URL string.
        /// </summary>
        public Uri ThreadUri { get; private set; } = InvalidThreadUri;

        /// <summary>
        /// Gets the type of forum used by this quest.
        /// Resets to unknown if the URL changes.
        /// Is set when a forum adapter is created/identified.
        /// </summary>
        public ForumType ForumType { get; set; } = ForumType.Unknown;

        /// <summary>
        /// The URL of the quest.
        /// Cannot be set to null or an empty string, and must be a well-formed URL.
        /// Automatically removes unsafe characters, and navigation elements from the URL.
        /// </summary>
        public string ThreadName
        {
            get { return threadName; }
            set
            {
                if (string.IsNullOrWhiteSpace(value))
                    throw new ArgumentException("URL cannot be null or empty.", nameof(value));
                if (!Uri.IsWellFormedUriString(value, UriKind.Absolute))
                    throw new ArgumentException($"URL ({value}) is not well formed.", nameof(value));

                string cleanValue = CleanupThreadName(value);
                cleanValue = Uri.UnescapeDataString(cleanValue);

                Uri newUri = new Uri(cleanValue);

                if (ThreadUri == InvalidThreadUri || ThreadUri.Host != newUri.Host)
                {
                    ForumType = ForumType.Unknown;
                }

                string oldThreadName = threadName;

                threadName = cleanValue;
                ThreadUri = newUri;

                OnPropertyChanged();

                // Reset the display name if it's based on the URL.
                if (string.IsNullOrEmpty(displayName))
                {
                    OnPropertyChanged(nameof(DisplayName));
                }
                else
                {
                    if (displayName == GetDisplayNameFromUrl(oldThreadName))
                        DisplayName = "";
                    else if (displayName == GetDisplayNameFromUrl(threadName))
                        displayName = "";
                }
            }
        }

        /// <summary>
        /// The friendly display name to show for the quest.
        /// If the backing var is empty, or if an attempt is made to set it to an empty value,
        /// automatically generates a value based on the thread URL.
        /// </summary>
        public string DisplayName
        {
            get
            {
                if (string.IsNullOrEmpty(displayName))
                {
                    return GetDisplayNameFromThreadName();
                }

                return displayName;
            }
            set
            {
                if (string.IsNullOrEmpty(value))
                    displayName = "";
                else
                    displayName = value.RemoveUnsafeCharacters();

                OnPropertyChanged();
            }
        }

        /// <summary>
        /// Shorthand function to get the display name for the current thread name.
        /// </summary>
        /// <returns>Returns a display name based on the current thread name.</returns>
        private string GetDisplayNameFromThreadName()
        {
            return GetDisplayNameFromUrl(ThreadName);
        }

        /// <summary>
        /// Function to extract a display name from the provided thread name, if possible.
        /// If it fails, just returns the entire URL.
        /// </summary>
        /// <param name="url">The URL to extract a display name out of.</param>
        /// <returns>Returns a name based on the provided URL.</returns>
        private string GetDisplayNameFromUrl(string url)
        {
            if (string.IsNullOrEmpty(url))
                return string.Empty;

            Match m = displayNameRegex.Match(url);
            if (m.Success)
                return m.Groups["displayName"].Value;
            else
                return url;
        }

        /// <summary>
        /// Remove unsafe characters from the provided URL, and strip navigation elements from the end.
        /// </summary>
        /// <param name="url">The URL to clean up.</param>
        /// <returns>Returns the base URL without navigation elements.</returns>
        private string CleanupThreadName(string url)
        {
            url = url.RemoveUnsafeCharacters();

            Match m = pageNumberRegex.Match(url);
            if (m.Success)
                url = m.Groups["base"].Value;

            return url;
        }
        #endregion

        #region Quest Configuration Properties
        #region Quest configuration properties: Post numbers
        int postsPerPage = 0;
        int startPost = 1;
        int endPost = 0;
        bool checkForLastThreadmark = true;
        BoolEx useRSSThreadmarks = BoolEx.Unknown;

        /// <summary>
        /// The number of the post to start looking for votes in.
        /// Must be a value greater than or equal to 1.
        /// </summary>
        public int StartPost
        {
            get { return startPost; }
            set
            {
                if (value < 1)
                    throw new ArgumentOutOfRangeException(nameof(StartPost), "Starting post number must be at least 1.");
                startPost = value;
                OnPropertyChanged();
            }
        }

        /// <summary>
        /// The number of the last post to look for votes in.
        /// Must be a value greater than or equal to 0.
        /// A value of 0 means it reads to the end of the thread.
        /// </summary>
        public int EndPost
        {
            get { return endPost; }
            set
            {
                if (value < 0)
                    throw new ArgumentOutOfRangeException(nameof(EndPost), "Ending post number must be at least 0.");
                endPost = value;
                OnPropertyChanged();
            }
        }

        /// <summary>
        /// Flag for whether to try to override the provided starting post by
        /// looking for the last threadmark.
        /// </summary>
        public bool CheckForLastThreadmark
        {
            get { return checkForLastThreadmark; }
            set
            {
                checkForLastThreadmark = value;
                OnPropertyChanged();
            }
        }

        /// <summary>
        /// Flag for whether to attempt to use RSS threadmarks.
        /// </summary>
        public BoolEx UseRSSThreadmarks
        {
            get { return useRSSThreadmarks; }
            set
            {
                useRSSThreadmarks = value;
                OnPropertyChanged();
            }
        }

        /// <summary>
        /// Boolean value indicating if the tally system should read to the end
        /// of the thread.  This is done when the EndPost is 0.
        /// </summary>
        public bool ReadToEndOfThread => EndPost == 0;

        /// <summary>
        /// The number of posts per page for this forum thread.
        /// Auto-detect value if current field value is 0.
        /// </summary>
        public int PostsPerPage
        {
            get
            {
                return postsPerPage;
            }
            set
            {
                postsPerPage = value;
                OnPropertyChanged();
            }
        }
        #endregion

        #region Quest configuration properties: Filtering
        bool useCustomThreadmarkFilters = false;
        string customThreadmarkFilters = string.Empty;

        bool useCustomTaskFilters = false;
        string customTaskFilters = string.Empty;

        bool useCustomUsernameFilters = false;
        string customUsernameFilters = string.Empty;

        bool useCustomPostFilters = false;
        string customPostFilters = string.Empty;
        readonly HashSet<long> postsToFilter = new HashSet<long>();
        static readonly Regex postFilterRegex = new Regex(@"(?<range>(?<r1>\d+)\s*-\s*(?<r2>\d+))|(?<num>\d+)");

        /// <summary>
        /// Flag for whether to use custom threadmark filters to exclude threadmarks
        /// from the list of valid 'last threadmark found' checks.
        /// </summary>
        public bool UseCustomThreadmarkFilters
        {
            get { return useCustomThreadmarkFilters; }
            set
            {
                useCustomThreadmarkFilters = value;
                OnPropertyChanged();
            }
        }
        /// <summary>
        /// Custom threadmark filters to exclude threadmarks from the list of valid
        /// 'last threadmark found' checks.
        /// </summary>
        public string CustomThreadmarkFilters
        {
            get { return customThreadmarkFilters; }
            set
            {
                customThreadmarkFilters = value;
                ThreadmarkFilter = new Filter(customThreadmarkFilters, OmakeFilter);
                OnPropertyChanged();
            }
        }

        /// <summary>
        /// Gets or sets the threadmark filter, based on current threadmark filter settings.
        /// </summary>
        public Filter ThreadmarkFilter { get; private set; } = Filter.Empty;

        /// <summary>
        /// Flag for whether to use custom threadmark filters to exclude threadmarks
        /// from the list of valid 'last threadmark found' checks.
        /// </summary>
        public bool UseCustomTaskFilters
        {
            get { return useCustomTaskFilters; }
            set
            {
                useCustomTaskFilters = value;
                OnPropertyChanged();
            }
        }

        /// <summary>
        /// Custom threadmark filters to exclude threadmarks from the list of valid
        /// 'last threadmark found' checks.
        /// </summary>
        public string CustomTaskFilters
        {
            get { return customTaskFilters; }
            set
            {
                customTaskFilters = value;
                TaskFilter = new Filter(customTaskFilters, null);
                OnPropertyChanged();
            }
        }

        /// <summary>
        /// Gets or sets the task filter, based on current task filter settings.
        /// </summary>
        public Filter TaskFilter { get; private set; } = Filter.Empty;


        /// <summary>
        /// Flag for whether to use custom filters to exclude specified users from the tally.
        /// </summary>
        public bool UseCustomUsernameFilters
        {
            get { return useCustomUsernameFilters; }
            set
            {
                useCustomUsernameFilters = value;
                OnPropertyChanged();
            }
        }

        /// <summary>
        /// List of custom users to filter.
        /// </summary>
        public string CustomUsernameFilters
        {
            get { return customUsernameFilters; }
            set
            {
                customUsernameFilters = value;
                UsernameFilter = new Filter(customUsernameFilters, null);
                OnPropertyChanged();
            }
        }

        /// <summary>
        /// Gets or sets the user filter, based on current user filter settings.
        /// </summary>
        public Filter UsernameFilter { get; private set; } = Filter.Empty;


        /// <summary>
        /// Flag for whether to use custom filters to exclude specified posts from the tally.
        /// </summary>
        public bool UseCustomPostFilters
        {
            get { return useCustomPostFilters; }
            set
            {
                useCustomPostFilters = value;
                OnPropertyChanged();
            }
        }

        /// <summary>
        /// List of custom posts to filter.
        /// </summary>
        public string CustomPostFilters
        {
            get { return customPostFilters; }
            set
            {
                customPostFilters = value;

                UpdatePostsToFilter();
                OnPropertyChanged();
            }
        }

        /// <summary>
        /// Collection of post numbers to filter from the tally.
        /// </summary>
        public HashSet<long> PostsToFilter => postsToFilter;

        /// <summary>
        /// Function to convert the CustomPostFilters string to a hashset of post
        /// numbers to filter.
        /// </summary>
        private void UpdatePostsToFilter()
        {
            postsToFilter.Clear();

            if (!string.IsNullOrEmpty(customPostFilters))
            {
                MatchCollection ms = postFilterRegex.Matches(customPostFilters);

                for (int i = 0; i < ms.Count; i++)
                {
                    Match m = ms[i];
                    
                    if (m.Groups[1].Success)
                    {
                        if (int.TryParse(m.Groups[2].Value, out int startRange) && int.TryParse(m.Groups[3].Value, out int endRange))
                        {
                            for (int j = startRange; j <= endRange; j++)
                            {
                                postsToFilter.Add(j);
                            }
                        }
                    }
                    else if (m.Groups[4].Success)
                    {
                        if (int.TryParse(m.Groups[4].Value, out int parseResult))
                        {
                            postsToFilter.Add(parseResult);
                        }
                    }
                }
            }
        }

        #endregion

        #region Quest configuration properties: Tally processing

        PartitionMode partitionMode = PartitionMode.None;
        bool whitespaceAndPunctuationIsSignificant = false;
        bool caseIsSignificant = false;
        bool forcePlanReferencesToBeLabeled = false;
        bool forbidVoteLabelPlanNames = false;
        bool allowUsersToUpdatePlans = false;
        bool disableProxyVotes = false;
        bool forcePinnedProxyVotes = false;
        bool ignoreSpoilers = false;
        bool trimExtendedText = false;


        /// <summary>
        /// Enum for the type of partitioning to use when performing a tally.
        /// </summary>
        public PartitionMode PartitionMode
        {
            get { return partitionMode; }
            set
            {
                partitionMode = value;
                OnPropertyChanged();
            }
        }


        /// <summary>
        /// Whether or not whitespace and punctuation is considered significant when
        /// doing vote and voter comparisons.
        /// </summary>
        public bool WhitespaceAndPunctuationIsSignificant
        {
            get { return whitespaceAndPunctuationIsSignificant; }
            set
            {
                whitespaceAndPunctuationIsSignificant = value;
                OnPropertyChanged();
            }
        }

        /// <summary>
        /// Whether or not case is considered significant when
        /// doing vote and voter comparisons.
        /// </summary>
        public bool CaseIsSignificant
        {
            get { return caseIsSignificant; }
            set
            {
                caseIsSignificant = value;
                OnPropertyChanged();
            }
        }

        /// <summary>
        /// Flag whether to allow label lines on votes to be plan names.
        /// </summary>
        public bool ForcePlanReferencesToBeLabeled
        {
            get { return forcePlanReferencesToBeLabeled; }
            set
            {
                forcePlanReferencesToBeLabeled = value;
                OnPropertyChanged();
            }
        }

        /// <summary>
        /// Flag whether to allow label lines on votes to be plan names.
        /// </summary>
        public bool ForbidVoteLabelPlanNames
        {
            get { return forbidVoteLabelPlanNames; }
            set
            {
                forbidVoteLabelPlanNames = value;
                OnPropertyChanged();
            }
        }

        /// <summary>
        /// Indicate whether users are allowed to update plans that they wrote in later posts.
        /// </summary>
        public bool AllowUsersToUpdatePlans
        {
            get { return allowUsersToUpdatePlans; }
            set
            {
                allowUsersToUpdatePlans = value;
                OnPropertyChanged();
            }
        }

        /// <summary>
        /// Flag whether to disable proxy votes (voting for another user to import their vote to your own).
        /// </summary>
        public bool DisableProxyVotes
        {
            get { return disableProxyVotes; }
            set
            {
                disableProxyVotes = value;
                OnPropertyChanged();
            }
        }

        /// <summary>
        /// Flag whether to force all user proxy votes to be pinned.
        /// </summary>
        public bool ForcePinnedProxyVotes
        {
            get { return forcePinnedProxyVotes; }
            set
            {
                forcePinnedProxyVotes = value;
                OnPropertyChanged();
            }
        }

        /// <summary>
        /// Whether or not to ignore spoiler blocks when parsing.
        /// </summary>
        public bool IgnoreSpoilers
        {
            get { return ignoreSpoilers; }
            set
            {
                ignoreSpoilers = value;
                OnPropertyChanged();
            }
        }

        /// <summary>
        /// Whether or not to trim extended text from vote lines.
        /// </summary>
        public bool TrimExtendedText
        {
            get { return trimExtendedText; }
            set
            {
                trimExtendedText = value;
                OnPropertyChanged();
            }
        }


        #endregion
        #endregion

        #region Object overrides
        public override string ToString()
        {
            return DisplayName;
        }
        #endregion

        #region Shadow Copy
        IQuest? shadowCopy;

        public IQuest GetShadowCopy()
        {
            shadowCopy = new Quest
            {
                ThreadName = ThreadName,
                DisplayName = DisplayName,
                ForumType = ForumType,
                StartPost = StartPost,
                EndPost = EndPost,
                CheckForLastThreadmark = CheckForLastThreadmark,
                UseRSSThreadmarks = UseRSSThreadmarks,
                PostsPerPage = PostsPerPage,
                UseCustomThreadmarkFilters = UseCustomThreadmarkFilters,
                CustomThreadmarkFilters = CustomThreadmarkFilters,
                UseCustomPostFilters = UseCustomPostFilters,
                CustomPostFilters = CustomPostFilters,
                UseCustomTaskFilters = UseCustomTaskFilters,
                CustomTaskFilters = CustomTaskFilters,
                UseCustomUsernameFilters = UseCustomUsernameFilters,
                CustomUsernameFilters = CustomUsernameFilters,
                PartitionMode = PartitionMode,
                WhitespaceAndPunctuationIsSignificant = WhitespaceAndPunctuationIsSignificant,
                CaseIsSignificant = CaseIsSignificant,
                ForcePlanReferencesToBeLabeled = ForcePlanReferencesToBeLabeled,
                ForbidVoteLabelPlanNames = ForbidVoteLabelPlanNames,
                AllowUsersToUpdatePlans = AllowUsersToUpdatePlans,
                DisableProxyVotes = DisableProxyVotes,
                ForcePinnedProxyVotes = ForcePinnedProxyVotes,
                IgnoreSpoilers = IgnoreSpoilers,
                TrimExtendedText = TrimExtendedText
            };
            
            foreach (IQuest q in LinkedQuests)
                shadowCopy.LinkedQuests.Add(q);

            return shadowCopy;
        }

        public void UpdateFromShadowCopy()
        {
            if (shadowCopy is not null)
            {
                ThreadName = shadowCopy.ThreadName;
                DisplayName = shadowCopy.DisplayName;
                ForumType = shadowCopy.ForumType;
                StartPost = shadowCopy.StartPost;
                EndPost = shadowCopy.EndPost;
                CheckForLastThreadmark = shadowCopy.CheckForLastThreadmark;
                UseRSSThreadmarks = shadowCopy.UseRSSThreadmarks;
                PostsPerPage = shadowCopy.PostsPerPage;
                UseCustomThreadmarkFilters = shadowCopy.UseCustomThreadmarkFilters;
                CustomThreadmarkFilters = shadowCopy.CustomThreadmarkFilters;
                UseCustomPostFilters = shadowCopy.UseCustomPostFilters;
                CustomPostFilters = shadowCopy.CustomPostFilters;
                UseCustomTaskFilters = shadowCopy.UseCustomTaskFilters;
                CustomTaskFilters = shadowCopy.CustomTaskFilters;
                UseCustomUsernameFilters = shadowCopy.UseCustomUsernameFilters;
                CustomUsernameFilters = shadowCopy.CustomUsernameFilters;
                PartitionMode = shadowCopy.PartitionMode;
                WhitespaceAndPunctuationIsSignificant = shadowCopy.WhitespaceAndPunctuationIsSignificant;
                CaseIsSignificant = shadowCopy.CaseIsSignificant;
                ForcePlanReferencesToBeLabeled = shadowCopy.ForcePlanReferencesToBeLabeled;
                ForbidVoteLabelPlanNames = shadowCopy.ForbidVoteLabelPlanNames;
                AllowUsersToUpdatePlans = shadowCopy.AllowUsersToUpdatePlans;
                DisableProxyVotes = shadowCopy.DisableProxyVotes;
                ForcePinnedProxyVotes = shadowCopy.ForcePinnedProxyVotes;
                IgnoreSpoilers = shadowCopy.IgnoreSpoilers;
                TrimExtendedText = shadowCopy.TrimExtendedText;

                LinkedQuests.Clear();

                foreach (IQuest q in shadowCopy.LinkedQuests)
                    LinkedQuests.Add(q);
            }
        }

        #endregion
    }
}
