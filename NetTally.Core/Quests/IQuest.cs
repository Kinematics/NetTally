﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using NetTally.Collections;
using NetTally.Forums;
using NetTally.Input.Utility;
using NetTally.Votes;
using NetTally.Types.Enums;

namespace NetTally
{
    public interface IQuest : INotifyPropertyChanged, IComparable
    {
        /// <summary>
        /// The name of the thread to be queried.
        /// </summary>
        string ThreadName { get; set; }
        /// <summary>
        /// The display name that the user can select.
        /// </summary>
        string DisplayName { get; set; }
        /// <summary>
        /// The URI that represents the thread URL string.
        /// </summary>
        Uri ThreadUri { get; }
        /// <summary>
        /// The type of forum this quest is hosted on.
        /// </summary>
        ForumType ForumType { get; set; }

        /// <summary>
        /// The number of posts per page for this forum thread.
        /// </summary>
        int PostsPerPage { get; set; }
        /// <summary>
        /// Starting post to start tallying from.
        /// </summary>
        int StartPost { get; set; }
        /// <summary>
        /// Ending post for the tally to run to.
        /// </summary>
        int EndPost { get; set; }
        /// <summary>
        /// Flag for whether to try to override the provided starting post by
        /// looking for the last threadmark.
        /// </summary>
        bool CheckForLastThreadmark { get; set; }
        /// <summary>
        /// Flag for whether to attempt to use RSS threadmarks.
        /// </summary>
        BoolEx UseRSSThreadmarks { get; set; }
        /// <summary>
        /// Derived property.  Is true if EndPost is 0.
        /// </summary>
        bool ReadToEndOfThread { get; }

        /// <summary>
        /// Enum for the type of partitioning to use when performing a tally.
        /// </summary>
        PartitionMode PartitionMode { get; set; }

        /// <summary>
        /// Flag for whether to use custom threadmark filters to exclude threadmarks
        /// from the list of valid 'last threadmark found' checks.
        /// </summary>
        bool UseCustomThreadmarkFilters { get; set; }
        /// <summary>
        /// Custom threadmark filters to exclude threadmarks from the list of valid
        /// 'last threadmark found' checks.
        /// </summary>
        string CustomThreadmarkFilters { get; set; }
        /// <summary>
        /// Gets or sets the threadmark filter, based on current threadmark filter settings.
        /// </summary>
        Filter ThreadmarkFilter { get; }

        /// <summary>
        /// Flag for whether to use custom filters to only process specified tasks.
        /// </summary>
        bool UseCustomTaskFilters { get; set; }
        /// <summary>
        /// List of custom tasks to process.
        /// </summary>
        string CustomTaskFilters { get; set; }
        /// <summary>
        /// Gets or sets the task filter, based on current task filter settings.
        /// </summary>
        Filter TaskFilter { get; }

        /// <summary>
        /// Flag for whether to use custom filters to exclude specified users from the tally.
        /// </summary>
        bool UseCustomUsernameFilters { get; set; }
        /// <summary>
        /// List of custom users to filter.
        /// </summary>
        string CustomUsernameFilters { get; set; }
        /// <summary>
        /// Gets or sets the user filter, based on current user filter settings.
        /// </summary>
        Filter UsernameFilter { get; }

        /// <summary>
        /// Flag for whether to use custom filters to exclude specified users from the tally.
        /// </summary>
        bool UseCustomPostFilters { get; set; }
        /// <summary>
        /// List of custom users to filter.
        /// </summary>
        string CustomPostFilters { get; set; }
        /// <summary>
        /// Collection of post numbers to filter from the tally.
        /// </summary>
        HashSet<long> PostsToFilter { get; }

        // Formatting options for handling votes:

        /// <summary>
        /// Whether or not whitespace and punctuation is considered significant when
        /// doing vote and voter comparisons.
        /// </summary>
        bool WhitespaceAndPunctuationIsSignificant { get; set; }
        /// <summary>
        /// Whether or not case is consider significant when
        /// doing vote and voter comparisons.
        /// </summary>
        bool CaseIsSignificant { get; set; }
        /// <summary>
        /// Gets or sets a value indicating whether plans are required to have a label of 'plan' when being referenced.
        /// </summary>
        bool ForcePlanReferencesToBeLabeled { get; set; }
        /// <summary>
        /// Flag whether to allow label lines on votes to be plan names.
        /// </summary>
        bool ForbidVoteLabelPlanNames { get; set; }
        /// <summary>
        /// Indicate whether users are allowed to update plans that they wrote in later posts.
        /// </summary>
        bool AllowUsersToUpdatePlans { get; set; }
        /// <summary>
        /// Flag whether to disable proxy votes (voting for another user to import their vote to your own).
        /// </summary>
        bool DisableProxyVotes { get; set; }
        /// <summary>
        /// Flag whether to force all user proxy votes to be pinned.
        /// </summary>
        bool ForcePinnedProxyVotes { get; set; }
        /// <summary>
        /// Whether or not to ignore spoiler blocks when parsing.
        /// </summary>
        bool IgnoreSpoilers { get; set; }
        /// <summary>
        /// Whether or not to trim extended text from vote lines.
        /// </summary>
        bool TrimExtendedText { get; set; }


        /// <summary>
        /// A collection of linked quests that should be tallied alongside this one.
        /// </summary>
        QuestCollection LinkedQuests { get; }
        /// <summary>
        /// Check if the given quest is one of the linked quests.
        /// </summary>
        /// <param name="quest">The quest to check on.</param>
        /// <returns>Returns true if this quest has the given quest in its links.</returns>
        bool HasLinkedQuest(IQuest quest);
        /// <summary>
        /// Check if the quest with the given name is one of the linked quests.
        /// </summary>
        /// <param name="questName">The name of the quest to check on.</param>
        /// <returns>Returns true if this quest has the given quest in its links.</returns>
        bool HasLinkedQuest(string questName);
        /// <summary>
        /// Add the provided quest to the list of links this quest has.
        /// </summary>
        /// <param name="quest">The quest to add.</param>
        void AddLinkedQuest(IQuest quest);
        /// <summary>
        /// Remove the provided quest from the list of quests this quest is linked to.
        /// </summary>
        /// <param name="quest">The quest to remove.</param>
        /// <returns>Returns true if the quest was found and removed.</returns>
        bool RemoveLinkedQuest(IQuest quest);

        /// <summary>
        /// Get a shadow copy of the current quest so that any changes to it can be discarded.
        /// </summary>
        /// <returns>Returns a copy of the current quest.</returns>
        IQuest GetShadowCopy();
        /// <summary>
        /// Update the current quest from its shadow copy (if any).
        /// </summary>
        void UpdateFromShadowCopy();
    }
}
