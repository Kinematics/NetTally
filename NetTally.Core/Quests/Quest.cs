using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;
using CommunityToolkit.Mvvm.ComponentModel;
using NetTally.Input.Utility;
using NetTally.Types.Components;
using NetTally.Types.Enums;
using NetTally.Utility;
using NetTally.VoteCounting;

namespace NetTally
{
    /// <summary>
    /// A Quest is a named forum thread, along with all the configuration properties that
    /// are used to determine how to go about tallying that thread.
    /// </summary>
    public partial class Quest : ObservableValidator
    {
        public Quest()
        {
        }

        private IVoteCounter voteCounter = null!;

        [JsonIgnore]
        public IVoteCounter VoteCounter
        {
            get { return voteCounter; }
            set
            {
                if (voteCounter == null &&
                    value != null)
                {
                    voteCounter = value;
                    voteCounter.SetQuest(this);
                }
            }
        }

        #region Static class data
        public const string OmakeFilter = @"\bomake\b";
        public const string NewThreadEntry = "https://www.example.com/threads/fake-thread.00000";
        public static readonly Uri InvalidThreadUri = new(NewThreadEntry);

        [GeneratedRegex("(?<range>(?<r1>\\d+)\\s*-\\s*(?<r2>\\d+))|(?<num>\\d+)", RegexOptions.None, 50)]
        private static partial Regex postFilterRegex();
        [GeneratedRegex("^(?<base>.+?)(&?page[-=]?\\d+)?(&p=?\\d+)?(#[^/]*)?(unread)?$", RegexOptions.None, 50)]
        private static partial Regex pageNumberRegex();
        [GeneratedRegex("(?<displayName>[^/]+)(/|#[^/]*)?$", RegexOptions.None, 50)]
        private static partial Regex displayNameRegex();
        #endregion

        #region Quest Identification
        public Guid QuestId { get; init; } = Guid.NewGuid();
        string threadName = NewThreadEntry;
        string displayName = string.Empty;
        public override string ToString() => DisplayName;

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

                Uri newUri = new(cleanValue);

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
                if (!string.IsNullOrEmpty(displayName))
                {
                    return displayName;
                }

                return GetDisplayNameFromThreadName();
            }
            set
            {
                if (displayName != value)
                {
                    if (string.IsNullOrEmpty(value))
                        displayName = "";
                    else
                        displayName = value.RemoveUnsafeCharacters();

                    OnPropertyChanged(nameof(DisplayName));
                }
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
        private static string GetDisplayNameFromUrl(string url)
        {
            if (string.IsNullOrEmpty(url))
                return string.Empty;

            Match m = displayNameRegex().Match(url);
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
        private static string CleanupThreadName(string url)
        {
            url = url.RemoveUnsafeCharacters();

            Match m = pageNumberRegex().Match(url);
            if (m.Success)
                url = m.Groups["base"].Value;

            return url;
        }
        #endregion

        #region Quest Configuration Properties
        #region Quest configuration properties: Post numbers
        [ObservableProperty]
        int postsPerPage = 0;

        [ObservableProperty]
        [Range(1, 1_000_000, ErrorMessage = "Starting post number must be at least 1")]
        int startPost = 1;

        partial void OnStartPostChanging(int value)
        {
            ValidateProperty(value, nameof(StartPost));
        }

        [ObservableProperty]
        [Range(0, 1_000_000, ErrorMessage = "Ending post number must be at least 0")]
        [NotifyPropertyChangedFor(nameof(ReadToEndOfThread))]
        int endPost = 0;

        partial void OnEndPostChanging(int value)
        {
            ValidateProperty(value, nameof(EndPost));
        }

        [ObservableProperty]
        bool checkForLastThreadmark = true;

        [ObservableProperty]
        BoolEx useRSSThreadmarks = BoolEx.Unknown;

        /// <summary>
        /// Boolean value indicating if the tally system should read to the end
        /// of the thread.  This is done when the EndPost is 0.
        /// </summary>
        public bool ReadToEndOfThread => EndPost == 0;
        #endregion

        #region Quest configuration properties: Filtering
        /// <summary>
        /// Flag for whether to use custom threadmark filters to exclude threadmarks
        /// from the list of valid 'last threadmark found' checks.
        /// </summary>
        [ObservableProperty]
        bool useCustomThreadmarkFilters = false;
        /// <summary>
        /// Custom threadmark filters to exclude threadmarks from the list of valid
        /// 'last threadmark found' checks.
        /// </summary>
        [ObservableProperty]
        string customThreadmarkFilters = string.Empty;
        /// <summary>
        /// Gets or sets the threadmark filter, based on current threadmark filter settings.
        /// </summary>
        public Filter ThreadmarkFilter { get; private set; } = new Filter("", OmakeFilter);

        partial void OnCustomThreadmarkFiltersChanged(string value)
        {
            ThreadmarkFilter = new Filter(value, OmakeFilter);
        }

        /// <summary>
        /// Flag for whether to use custom threadmark filters to exclude threadmarks
        /// from the list of valid 'last threadmark found' checks.
        /// </summary>
        [ObservableProperty]
        bool useCustomTaskFilters = false;
        /// <summary>
        /// Custom threadmark filters to exclude threadmarks from the list of valid
        /// 'last threadmark found' checks.
        /// </summary>
        [ObservableProperty]
        string customTaskFilters = string.Empty;
        /// <summary>
        /// Gets or sets the task filter, based on current task filter settings.
        /// </summary>
        public Filter TaskFilter { get; private set; } = Filter.Empty;

        partial void OnCustomTaskFiltersChanged(string value)
        {
            TaskFilter = new Filter(value, null);
        }

        /// <summary>
        /// Flag for whether to use custom filters to exclude specified users from the tally.
        /// </summary>
        [ObservableProperty]
        bool useCustomUsernameFilters = false;
        /// <summary>
        /// List of custom users to filter.
        /// </summary>
        [ObservableProperty]
        string customUsernameFilters = string.Empty;
        /// <summary>
        /// Gets or sets the user filter, based on current user filter settings.
        /// </summary>
        public Filter UsernameFilter { get; private set; } = Filter.Empty;

        partial void OnCustomUsernameFiltersChanged(string value)
        {
            UsernameFilter = new Filter(value, null);
        }

        /// <summary>
        /// Flag for whether to use custom filters to exclude specified posts from the tally.
        /// </summary>
        [ObservableProperty]
        bool useCustomPostFilters = false;
        /// <summary>
        /// List of custom posts to filter.
        /// </summary>
        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(PostsToFilter))]
        string customPostFilters = string.Empty;
        /// <summary>
        /// Collection of post numbers to filter from the tally.
        /// </summary>
        [JsonIgnore]
        public HashSet<long> PostsToFilter { get; } = new();

        /// <summary>
        /// Convert the CustomPostFilters string to a hashset of post
        /// numbers to filter.
        /// </summary>
        partial void OnCustomPostFiltersChanged(string value)
        {
            PostsToFilter.Clear();

            if (!string.IsNullOrEmpty(value))
            {
                MatchCollection ms = postFilterRegex().Matches(value);

                for (int i = 0; i < ms.Count; i++)
                {
                    Match m = ms[i];

                    if (m.Groups[1].Success)
                    {
                        if (int.TryParse(m.Groups[2].Value, out int startRange) &&
                            int.TryParse(m.Groups[3].Value, out int endRange))
                        {
                            for (int j = startRange; j <= endRange; j++)
                            {
                                PostsToFilter.Add(j);
                            }
                        }
                    }
                    else if (m.Groups[4].Success)
                    {
                        if (int.TryParse(m.Groups[4].Value, out int parseResult))
                        {
                            PostsToFilter.Add(parseResult);
                        }
                    }
                }
            }
        }

        #endregion

        #region Quest configuration properties: Tally processing

        [ObservableProperty]
        PartitionMode partitionMode = Types.Enums.PartitionMode.None;
        [ObservableProperty]
        DisplayMode displayMode = Types.Enums.DisplayMode.Normal;
        [ObservableProperty]
        bool whitespaceAndPunctuationIsSignificant = false;
        [ObservableProperty]
        bool caseIsSignificant = false;
        [ObservableProperty]
        bool forcePlanReferencesToBeLabeled = false;
        [ObservableProperty]
        bool forbidVoteLabelPlanNames = false;
        [ObservableProperty]
        bool allowUsersToUpdatePlans = false;
        [ObservableProperty]
        bool disableProxyVotes = false;
        [ObservableProperty]
        bool forcePinnedProxyVotes = false;
        [ObservableProperty]
        bool ignoreSpoilers = false;
        [ObservableProperty]
        bool trimExtendedText = false;
        #endregion
        #endregion

        #region Linked Quests
        /// <summary>
        /// A collection of the IDs of any quests that should be tallied together
        /// with this one.
        /// </summary>
        public ObservableCollection<Guid> LinkedQuestIds = new();

        /// <summary>
        /// Determine whether this quest is linked to the provided quest ID.
        /// </summary>
        /// <param name="questId">The ID of the quest to check for.</param>
        /// <returns>Returns true if the quest is linked, or false if not.</returns>
        public bool HasLinkedQuest(Guid questId)
        {
            return LinkedQuestIds.Contains(questId);
        }

        /// <summary>
        /// Determine whether this quest is linked to the provided quest.
        /// </summary>
        /// <param name="quest">The quest to check for.</param>
        /// <returns>Returns true if the quest is linked, or false if not.</returns>
        public bool HasLinkedQuest(Quest quest)
        {
            return LinkedQuestIds.Contains(quest.QuestId);
        }

        /// <summary>
        /// Adds the provided quest ID to this quest's list of linked quests.
        /// </summary>
        /// <param name="questId">The quest ID to add.</param>
        public void AddLinkedQuest(Guid questId)
        {
            if (!LinkedQuestIds.Contains(questId))
            {
                LinkedQuestIds.Add(questId);
            }
        }

        /// <summary>
        /// Adds the provided quest to this quest's list of linked quests.
        /// </summary>
        /// <param name="quest">The quest to add.</param>
        public void AddLinkedQuest(Quest quest)
        {
            if (quest == this)
                return;

            AddLinkedQuest(quest.QuestId);
        }

        /// <summary>
        /// Remove the provided quest ID from this quest's list of linked quests.
        /// </summary>
        /// <param name="questId">The quest ID to remove.</param>
        /// <returns>Returns true if the quest was removed, or false if not.</returns>
        public bool RemoveLinkedQuest(Guid questId)
        {
            return LinkedQuestIds.Remove(questId);
        }

        /// <summary>
        /// Remove the provided quest from this quest's list of linked quests.
        /// </summary>
        /// <param name="quest">The quest to remove.</param>
        /// <returns>Returns true if the quest was found and removed.</returns>
        public bool RemoveLinkedQuest(Quest quest)
        {
            return LinkedQuestIds.Remove(quest.QuestId);
        }
        #endregion Linked Quests

        #region Shadow Copy
        Quest? shadowCopy;

        public Quest GetShadowCopy()
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

            foreach (var q in LinkedQuestIds)
                shadowCopy.AddLinkedQuest(q);

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

                LinkedQuestIds.Clear();

                foreach (var q in shadowCopy.LinkedQuestIds)
                    AddLinkedQuest(q);
            }
        }



        #endregion
    }
}
