﻿using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;
using NetTally.Global;
using NetTally.Types.Enums;

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
            this.logger = logger;

            ArgumentNullException.ThrowIfNull(questsInfo.SelectedQuest, nameof(questsInfo.SelectedQuest));

            quest = questsInfo.SelectedQuest;
            AvailableQuests = questsInfo.Quests;

            LoadQuestOptions();
        }

        public List<int> ValidPostsPerPage { get; } = new List<int> { 0, 5, 10, 15, 20, 25, 30, 35, 40, 45, 50 };

        public ObservableCollection<Quest> LinkedQuests { get; } = new();

        public ObservableCollection<Quest> AvailableQuests { get; }



        private void LoadQuestOptions()
        {
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
            foreach (var questId in quest.LinkedQuestIds)
            {
                var linkedQuest = AvailableQuests.FirstOrDefault(q => q.QuestId == questId);
                if (linkedQuest != null)
                {
                    LinkedQuests.Add(linkedQuest);
                }
            }

            logger.LogInformation("Quest information loaded into view model.");
        }

        private void SaveQuestOptions()
        {
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
        private void Save()
        {
            SaveQuestOptions();
            SaveCompleted?.Invoke();
        }

        public event Action? SaveCompleted;

        [RelayCommand]
        private void Reset()
        {
            LoadQuestOptions();
        }

        [RelayCommand]
        private void AddLinkedQuest(Quest? quest)
        {
            if (quest is not null)
            {
                if (!LinkedQuests.Contains(quest))
                {
                    LinkedQuests.Add(quest);
                }
            }
        }

        [RelayCommand]
        private void RemoveLinkedQuest(Quest? quest)
        {
            if (quest is not null)
            {
                if (LinkedQuests.Contains(quest))
                {
                    LinkedQuests.Remove(quest);
                }
            }
        }


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
    }
}
