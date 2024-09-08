using System.Collections.ObjectModel;
using System.Text.RegularExpressions;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;
using NetTally.Configure;
using NetTally.Enums;
using NetTally.Utility;

namespace NetTally.ViewModels
{
    public partial class QuestOptionsViewModel : ObservableObject
    {
        private readonly ILogger<QuestOptionsViewModel> logger;
        private readonly Quest quest;

        public QuestOptionsViewModel(
            IQuestsInfo questsInfo,
            ILogger<QuestOptionsViewModel> logger)
        {
            ArgumentNullException.ThrowIfNull(questsInfo.SelectedQuest, nameof(questsInfo.SelectedQuest));

            this.logger = logger;

            quest = questsInfo.SelectedQuest;
            AvailableQuests = new ObservableCollection<Quest>(questsInfo.Quests);
            AvailableQuests.Remove(quest);

            SelectedAvailableQuest = AvailableQuests.FirstOrDefault();

            LoadQuestOptions();
        }

        public List<int> ValidPostsPerPage { get; } = [0, 5, 10, 15, 20, 25, 30, 35, 40, 45, 50];

        public List<ForumType> ValidForums { get; } = [.. Enum.GetValues<ForumType>()];

        public ObservableCollection<Quest> AvailableQuests { get; }

        public ObservableCollection<Quest> LinkedQuests { get; } = [];

        private void LoadQuestOptions()
        {
            ThreadUri = quest.ThreadUri;
            ThreadName = quest.ThreadName;
            DisplayName = quest.DisplayName;
            ForumType = quest.ForumType;
            PostsPerPage = quest.PostsPerPage;
            StartPost = quest.StartPost;
            EndPost = quest.EndPost;
            CheckForLastThreadmark = quest.CheckForLastThreadmark;
            UseRSSThreadmarks = quest.UseRSSThreadmarks;
            PartitionMode = quest.PartitionMode;
            UseCustomThreadmarkFilters = quest.UseCustomThreadmarkFilters;
            UseCustomTaskFilters = quest.UseCustomTaskFilters;
            UseCustomUsernameFilters = quest.UseCustomUsernameFilters;
            UseCustomPostFilters = quest.UseCustomPostFilters;
            CustomThreadmarkFilters = quest.CustomThreadmarkFilters;
            CustomTaskFilters = quest.CustomTaskFilters;
            CustomUsernameFilters = quest.CustomUsernameFilters;
            CustomPostFilters = quest.CustomPostFilters;
            WhitespaceAndPunctuationIsSignificant = quest.WhitespaceAndPunctuationIsSignificant;
            CaseIsSignificant = quest.CaseIsSignificant;
            ForbidVoteLabelPlanNames = quest.ForbidVoteLabelPlanNames;
            ForcePlanReferencesToBeLabeled = quest.ForcePlanReferencesToBeLabeled;
            AllowUsersToUpdatePlans = quest.AllowUsersToUpdatePlans;
            DisableProxyVotes = quest.DisableProxyVotes;
            ForcePinnedProxyVotes = quest.ForcePinnedProxyVotes;
            IgnoreSpoilers = quest.IgnoreSpoilers;
            TrimExtendedText = quest.TrimExtendedText;

            LinkedQuests.Clear();

            var linkedQuests = AvailableQuests.Where(quest.HasLinkedQuest);

            foreach (var linkedQuest in linkedQuests)
            {
                LinkedQuests.Add(linkedQuest);
            }

            logger.LogInformation("Quest information loaded into view model.");
        }

        private void SaveQuestOptions()
        {
            quest.ThreadUri = ThreadUri;
            quest.ThreadName = ThreadName;
            quest.DisplayName = DisplayName;
            quest.ForumType = ForumType;
            quest.PostsPerPage = PostsPerPage;
            quest.StartPost = StartPost;
            quest.EndPost = EndPost;
            quest.CheckForLastThreadmark = CheckForLastThreadmark;
            quest.UseRSSThreadmarks = UseRSSThreadmarks;
            quest.PartitionMode = PartitionMode;
            quest.UseCustomThreadmarkFilters = UseCustomThreadmarkFilters;
            quest.UseCustomTaskFilters = UseCustomTaskFilters;
            quest.UseCustomUsernameFilters = UseCustomUsernameFilters;
            quest.UseCustomPostFilters = UseCustomPostFilters;
            quest.CustomThreadmarkFilters = CustomThreadmarkFilters;
            quest.CustomTaskFilters = CustomTaskFilters;
            quest.CustomUsernameFilters = CustomUsernameFilters;
            quest.CustomPostFilters = CustomPostFilters;
            quest.WhitespaceAndPunctuationIsSignificant = WhitespaceAndPunctuationIsSignificant;
            quest.CaseIsSignificant = CaseIsSignificant;
            quest.ForbidVoteLabelPlanNames = ForbidVoteLabelPlanNames;
            quest.ForcePlanReferencesToBeLabeled = ForcePlanReferencesToBeLabeled;
            quest.AllowUsersToUpdatePlans = AllowUsersToUpdatePlans;
            quest.DisableProxyVotes = DisableProxyVotes;
            quest.ForcePinnedProxyVotes = ForcePinnedProxyVotes;
            quest.IgnoreSpoilers = IgnoreSpoilers;
            quest.TrimExtendedText = TrimExtendedText;

            quest.LinkedQuestIds.Clear();
            foreach (var linkedQuest in LinkedQuests)
            {
                quest.AddLinkedQuest(linkedQuest);
            }

            logger.LogInformation("View model information saved to quest.");
        }

        [RelayCommand]
        private void ClearFilters()
        {
            CustomThreadmarkFilters = string.Empty;
            CustomTaskFilters = string.Empty;
            CustomUsernameFilters = string.Empty;
            CustomPostFilters = string.Empty;

            UseCustomThreadmarkFilters = false;
            UseCustomTaskFilters = false;
            UseCustomUsernameFilters = false;
            UseCustomPostFilters = false;
        }

        [RelayCommand]
        private void ClearOptions()
        {
            UseRSSThreadmarks = BoolEx.Unknown;
            WhitespaceAndPunctuationIsSignificant = false;
            CaseIsSignificant = false;
            ForbidVoteLabelPlanNames = false;
            ForcePlanReferencesToBeLabeled = false;
            AllowUsersToUpdatePlans = false;
            DisableProxyVotes = false;
            ForcePinnedProxyVotes = false;
            IgnoreSpoilers = false;
            TrimExtendedText = false;
        }

        [RelayCommand]
        private void Reset()
        {
            LoadQuestOptions();
            OnPropertyChanged(nameof(ResetCommand));
        }

        [RelayCommand]
        private void Save()
        {
            SaveQuestOptions();
            OnPropertyChanged(nameof(SaveCommand));
        }

        [RelayCommand]
        private void Cancel()
        {
            OnPropertyChanged(nameof(CancelCommand));
        }

        [ObservableProperty]
        [NotifyCanExecuteChangedFor(nameof(AddLinkedQuestCommand))]
        private Quest? selectedAvailableQuest;

        [RelayCommand(CanExecute = nameof(CanAddLinkedQuest))]
        private void AddLinkedQuest(Quest? quest)
        {
            if (quest is not null &&
                !LinkedQuests.Contains(quest))
            {
                LinkedQuests.Add(quest);
            }
        }

        private static bool CanAddLinkedQuest(Quest? quest)
        {
            return quest is not null;
        }

        [ObservableProperty]
        [NotifyCanExecuteChangedFor(nameof(RemoveLinkedQuestCommand))]
        private Quest? selectedLinkedQuest;

        [RelayCommand(CanExecute = nameof(CanRemoveLinkedQuest))]
        private void RemoveLinkedQuest(Quest? quest)
        {
            if (quest is not null)
            {
                LinkedQuests.Remove(quest);
            }
        }

        private static bool CanRemoveLinkedQuest(Quest? quest)
        {
            return quest is not null;
        }

        [ObservableProperty]
        private Uri threadUri = Quest.InvalidThreadUri;

        [ObservableProperty]
        private string threadName = string.Empty;

        [ObservableProperty]
        private string displayName = string.Empty;

        [ObservableProperty]
        private ForumType forumType;

        [ObservableProperty]
        private int postsPerPage;

        [ObservableProperty]
        private int startPost;

        [ObservableProperty]
        private int endPost;

        [ObservableProperty]
        private bool checkForLastThreadmark;

        [ObservableProperty]
        private BoolEx useRSSThreadmarks = BoolEx.Unknown;

        [ObservableProperty]
        private PartitionMode partitionMode;

        [ObservableProperty]
        private bool useCustomThreadmarkFilters;

        [ObservableProperty]
        private string customThreadmarkFilters = string.Empty;

        [ObservableProperty]
        private bool useCustomTaskFilters;

        [ObservableProperty]
        private string customTaskFilters = string.Empty;

        [ObservableProperty]
        private bool useCustomUsernameFilters;

        [ObservableProperty]
        private string customUsernameFilters = string.Empty;

        [ObservableProperty]
        private bool useCustomPostFilters;

        [ObservableProperty]
        private string customPostFilters = string.Empty;

        [ObservableProperty]
        private bool whitespaceAndPunctuationIsSignificant;

        [ObservableProperty]
        private bool caseIsSignificant;

        [ObservableProperty]
        private bool forcePlanReferencesToBeLabeled;

        [ObservableProperty]
        private bool forbidVoteLabelPlanNames;

        [ObservableProperty]
        private bool allowUsersToUpdatePlans;

        [ObservableProperty]
        private bool disableProxyVotes;

        [ObservableProperty]
        private bool forcePinnedProxyVotes;

        [ObservableProperty]
        private bool ignoreSpoilers;

        [ObservableProperty]
        private bool trimExtendedText;




        public void SetQuestThreadFromClipboard(string? url)
        {
            if (string.IsNullOrWhiteSpace(url))
                return;

            if (ThreadName != Strings.NewThreadEntry)
                return;

            if (Uri.IsWellFormedUriString(url, UriKind.Absolute))
                ThreadName = url;
        }


        partial void OnThreadNameChanged(string? oldValue, string newValue)
        {
            // cleanup newValue
            string cleanValue = CleanupThreadName(newValue);
            cleanValue = Uri.UnescapeDataString(cleanValue);

            // set thread name to cleaned up value
#pragma warning disable MVVMTK0034 // Direct field reference to [ObservableProperty] backing field
            threadName = cleanValue;
#pragma warning restore MVVMTK0034 // Direct field reference to [ObservableProperty] backing field

            Uri newUri = new(cleanValue);

            // if host changed, reset forum type and update the thread uri
            if (ThreadUri.Host != newUri.Host)
            {
                ForumType = ForumType.Unknown;
            }

            ThreadUri = newUri;

            DisplayName = GetDisplayNameFromUrl(cleanValue);
        }


        [GeneratedRegex(@"^(?<base>.+?)(?:&?page[-=]?\d+)?(?:&p=?\d+)?(?:(?<!showthread\.php)\?[^#]*)?(?:#[^/]*)?(?:unread)?$",
            RegexOptions.None, 50)]
        private static partial Regex PageNumberRegex();

        private static string CleanupThreadName(string url)
        {
            url = url.RemoveUnsafeCharacters();

            Match m = PageNumberRegex().Match(url);
            if (m.Success)
                url = m.Groups["base"].Value;

            return url;
        }

        [GeneratedRegex(@"(?:showthread\.php\?)?(?:t=)?(?<displayName>[^/]+)(/|#[^/]*)?$", RegexOptions.None, 50)]
        private static partial Regex DisplayNameRegex();

        private static string GetDisplayNameFromUrl(string url)
        {
            if (string.IsNullOrEmpty(url))
                return string.Empty;

            Match m = DisplayNameRegex().Match(url);
            if (m.Success)
                return m.Groups["displayName"].Value;
            else
                return url;
        }
    }
}
