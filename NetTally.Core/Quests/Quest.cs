using System.Collections.ObjectModel;
using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;
using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.Extensions.DependencyInjection;
using NetTally.Enums;
using NetTally.Quests;
using NetTally.Tally.Components.Counting;
using NetTally.Tally.Components.Posts;
using NetTally.Utility;
using NetTally.Utility.Comparers;
using NetTally.Utility.Filtering;

namespace NetTally;

/// <summary>
/// A Quest is a named forum thread, along with all the configuration properties that
/// are used to determine how to go about tallying that thread.
/// </summary>
public partial class Quest : ObservableValidator
{
    public Quest() { }

    [JsonIgnore]
    public IVoteCounter VoteCounter { get; set; } = null!;
    
    public static readonly Uri InvalidThreadUri = new(Strings.NewThreadEntry);

    #region Quest Identification
    public QuestId QuestId { get; init; } = QuestId.NewQuestId();

    [ObservableProperty]
    string threadName = Strings.NewThreadEntry;

    partial void OnThreadNameChanged(string? oldValue, string newValue)
    {
#pragma warning disable MVVMTK0034 // Direct field reference to [ObservableProperty] backing field
        if (string.IsNullOrWhiteSpace(newValue) ||
            !Uri.IsWellFormedUriString(newValue, UriKind.Absolute))
        {
            this.threadName = oldValue!;
            throw new ArgumentException(nameof(ThreadName));
        }

        this.threadName = newValue.RemoveUnsafeCharacters();

        ThreadUri = new Uri(threadName);
#pragma warning restore MVVMTK0034 // Direct field reference to [ObservableProperty] backing field
    }


    [ObservableProperty]
    string displayName = Strings.NewThreadDisplayName;

    /// <summary>
    /// Ensure the display name is not null, nor has unsafe characters.
    /// </summary>
    /// <param name="value">The new DisplayName value.</param>
    partial void OnDisplayNameChanged(string value)
    {
#pragma warning disable MVVMTK0034 // Direct field reference to [ObservableProperty] backing field
        if (value is null)
        {
            displayName = string.Empty;
            return;
        }

        displayName = value.RemoveUnsafeCharacters().Trim();
#pragma warning restore MVVMTK0034 // Direct field reference to [ObservableProperty] backing field
    }

    public override string ToString() => DisplayName;

    /// <summary>
    /// The URI that represents the thread URL string.
    /// </summary>
    public Uri ThreadUri { get; set; } = InvalidThreadUri;

    /// <summary>
    /// Gets the type of forum used by this quest.
    /// Resets to unknown if the URL changes.
    /// Is set when a forum adapter is created/identified.
    /// </summary>
    public ForumType ForumType { get; set; } = ForumType.Unknown;
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
    bool checkForLastThreadmark;

    [ObservableProperty]
    BoolEx useRSSThreadmarks = BoolEx.Unknown;

    /// <summary>
    /// Boolean value indicating if the tally system should read to the end
    /// of the thread.  This is done when the EndPost is 0.
    /// </summary>
    public bool ReadToEndOfThread => EndPost == 0;
    #endregion Quest configuration properties: Post numbers

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
    public TextFilter ThreadmarkFilter { get; private set; } = RegexFilter.DefaultThreadmarkFilter;

    partial void OnCustomThreadmarkFiltersChanged(string value)
    {
        ThreadmarkFilter = string.IsNullOrEmpty(value)
            ? RegexFilter.DefaultThreadmarkFilter
            : RegexFilter.Block(Strings.OmakeFilter, value);
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
    public TextFilter TaskFilter { get; private set; } = RegexFilter.AlwaysAllow;

    partial void OnCustomTaskFiltersChanged(string value)
    {
        TaskFilter = string.IsNullOrEmpty(value)
            ? RegexFilter.AlwaysAllow
            : RegexFilter.Allow(value);
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
    public TextFilter UsernameFilter { get; private set; } = RegexFilter.AlwaysAllow;

    partial void OnCustomUsernameFiltersChanged(string value)
    {
        UsernameFilter = string.IsNullOrEmpty(value)
            ? RegexFilter.AlwaysAllow
            : RegexFilter.Block(value);
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
    [NotifyPropertyChangedFor(nameof(PostsFilter))]
    string customPostFilters = string.Empty;
    /// <summary>
    /// Gets or sets the posts filter.
    /// </summary>
    [JsonIgnore]
    public PostNumFilter PostsFilter { get; private set; } =
        PostNumberFilter.AlwaysAllow;

    partial void OnCustomPostFiltersChanged(string value)
    {
        PostsFilter = PostNumberFilter.Create(value);
    }
    #endregion Quest configuration properties: Filtering

    #region Quest configuration properties: Tally processing
    [ObservableProperty]
    PartitionMode partitionMode = PartitionMode.None;
    [ObservableProperty]
    DisplayMode displayMode = DisplayMode.Normal;
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
    #endregion Quest configuration properties: Tally processing

    #region Quest configuration properties: String Comparison
    Agnostic? agnostic = null;

    [JsonIgnore]
    public AgnosticStringComparer CurrentComparer
    {
        get
        {
            agnostic ??= AppX.Services.GetRequiredService<Agnostic>();

            return CaseIsSignificant switch
            {
                true => WhitespaceAndPunctuationIsSignificant switch
                {
                    true => agnostic.StringComparerCaseSymbol,
                    false => agnostic.StringComparerCaseNoSymbol
                },
                false => WhitespaceAndPunctuationIsSignificant switch
                {
                    true => agnostic.StringComparerNoCaseSymbol,
                    false => agnostic.StringComparerNoCaseNoSymbol
                }
            };

        }
    }
    #endregion Quest configuration properties: String Comparison

    #endregion Quest Configuration Properties

    #region Linked Quests
    /// <summary>
    /// A collection of the IDs of any quests that should be tallied together
    /// with this one.
    /// </summary>
    public ObservableCollection<QuestId> LinkedQuestIds = [];

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
    /// Adds the provided quest to this quest's list of linked quests.
    /// </summary>
    /// <param name="quest">The quest to add.</param>
    public void AddLinkedQuest(Quest quest)
    {
        if (quest == this)
            return;

        if (!LinkedQuestIds.Contains(quest.QuestId))
        {
            LinkedQuestIds.Add(quest.QuestId);
        }
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

    #region Vote Counter Pass-Through
    /// <summary>
    /// Construct votes from the provided posts.
    /// Does a clean build of votes from the data.
    /// </summary>
    /// <param name="titles">Titles to show for the quest.</param>
    /// <param name="posts">Posts to use to construct the votes.</param>
    public void ConstructVotes(IEnumerable<string> titles, IEnumerable<PostType> posts)
    {
        VoteCounter.ConstructVotes(titles, posts);
    }

    /// <summary>
    /// Construct votes based on the currently held posts.
    /// </summary>
    public void ConstructVotes()
    {
        VoteCounter.ConstructVotes();
    }
    #endregion Vote Counter Pass-Through
}
