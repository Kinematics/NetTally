using System.Collections.ObjectModel;
using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;
using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.Extensions.DependencyInjection;
using NetTally.Enums;
using NetTally.Input.Forums.ForumAdapters;
using NetTally.Quests;
using NetTally.Tally.Components.Counting;
using NetTally.Tally.Posts.Component;
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

    public string ThreadName
    {
        get => field;
        set
        {
            if (string.IsNullOrWhiteSpace(value) ||
                string.Compare(field, value) == 0 ||
                !Uri.IsWellFormedUriString(value, UriKind.Absolute))
            {
                return;
            }

            SetProperty(ref field, value.RemoveUnsafeCharacters(), nameof(ThreadName));
            ThreadUri = new Uri(ThreadName);
        }
    } = Strings.NewThreadEntry;

    public string DisplayName
    {
        get => field;
        set => SetProperty(ref field, value.RemoveUnsafeCharacters().Trim(), nameof(DisplayName));
    } = Strings.NewThreadDisplayName;

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

    /// <summary>
    /// Get the forum adapter for this quest.
    /// Updates the quest as necessary.
    /// </summary>
    /// <param name="adapterFactory">The adapter factory used to create forum adapters.</param>
    /// <param name="token">A cancellation token.</param>
    /// <returns>A <see cref="IForumAdapter"/> for this quest.</returns>
    public async Task<IForumAdapter> GetForumAdapter(ForumAdapterFactory adapterFactory,
        CancellationToken token)
    {
        IForumAdapter adapter = await adapterFactory
            .CreateForumAdapterAsync(this, token)
            .ConfigureAwait(ConfigureAwaitOptions.None);

        if (PostsPerPage == 0)
            PostsPerPage = adapter.GetDefaultPostsPerPage(ThreadUri);

        if (adapter.HasRssThreadmarksFeed(ThreadUri) == BoolEx.True && UseRSSThreadmarks == BoolEx.Unknown)
            UseRSSThreadmarks = BoolEx.True;

        return adapter;
    }
    #endregion

    #region Quest Configuration Properties

    #region Quest configuration properties: Post numbers
    [ObservableProperty]
    public partial int PostsPerPage { get; set; } = 0;

    [ObservableProperty]
    [Range(1, 1_000_000, ErrorMessage = "Starting post number must be at least 1")]
    public partial int StartPost { get; set; } = 1;

    partial void OnStartPostChanging(int value)
    {
        ValidateProperty(value, nameof(StartPost));
    }

    [ObservableProperty]
    [Range(0, 1_000_000, ErrorMessage = "Ending post number must be at least 0")]
    [NotifyPropertyChangedFor(nameof(ReadToEndOfThread))]
    public partial int EndPost { get; set; } = 0;

    partial void OnEndPostChanging(int value)
    {
        ValidateProperty(value, nameof(EndPost));
    }

    [ObservableProperty]
    public partial bool CheckForLastThreadmark { get; set; }

    [ObservableProperty]
    public partial BoolEx UseRSSThreadmarks { get; set; } = BoolEx.Unknown;

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
    public partial bool UseCustomThreadmarkFilters { get; set; } = false;

    /// <summary>
    /// Custom threadmark filters to exclude threadmarks from the list of valid
    /// 'last threadmark found' checks.
    /// </summary>
    [ObservableProperty]
    public partial string CustomThreadmarkFilters { get; set; } = string.Empty;

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
    public partial bool UseCustomTaskFilters { get; set; } = false;

    /// <summary>
    /// Custom threadmark filters to exclude threadmarks from the list of valid
    /// 'last threadmark found' checks.
    /// </summary>
    [ObservableProperty]
    public partial string CustomTaskFilters { get; set; } = string.Empty;

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
    public partial bool UseCustomUsernameFilters { get; set; } = false;

    /// <summary>
    /// List of custom users to filter.
    /// </summary>
    [ObservableProperty]
    public partial string CustomUsernameFilters { get; set; } = string.Empty;

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
    public partial bool UseCustomPostFilters { get; set; } = false;

    /// <summary>
    /// List of custom posts to filter.
    /// </summary>
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(PostsFilter))]
    public partial string CustomPostFilters { get; set; } = string.Empty;

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
    public partial PartitionMode PartitionMode { get; set; } = PartitionMode.None;

    [ObservableProperty]
    public partial DisplayMode DisplayMode { get; set; } = DisplayMode.Normal;

    [ObservableProperty]
    public partial bool WhitespaceAndPunctuationIsSignificant { get; set; } = false;

    [ObservableProperty]
    public partial bool CaseIsSignificant { get; set; } = false;

    [ObservableProperty]
    public partial bool ForcePlanReferencesToBeLabeled { get; set; } = false;

    [ObservableProperty]
    public partial bool ForbidVoteLabelPlanNames { get; set; } = false;

    [ObservableProperty]
    public partial bool AllowUsersToUpdatePlans { get; set; } = false;

    [ObservableProperty]
    public partial bool DisableProxyVotes { get; set; } = false;

    [ObservableProperty]
    public partial bool ForcePinnedProxyVotes { get; set; } = false;

    [ObservableProperty]
    public partial bool IgnoreSpoilers { get; set; } = false;

    [ObservableProperty]
    public partial bool TrimExtendedText { get; set; } = false;
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
    [ObservableProperty]
    public partial ObservableCollection<QuestId> LinkedQuestIds { get; set; } = [];

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
    public void ConstructVotes(IEnumerable<string> titles, IEnumerable<Post> posts)
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
