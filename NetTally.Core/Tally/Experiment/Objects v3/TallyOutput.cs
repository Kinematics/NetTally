using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using NetTally.Options;
using NetTally.Output;
using NetTally.SystemInfo;
using NetTally.VoteCounting;
using NetTally.Votes;

namespace NetTally.Experiment3
{
    public class TallyOutput : ITextResultsProvider
    {
        #region Constructor and private fields
        readonly IVoteCounter voteCounter;
        readonly IGeneralOutputOptions outputOptions;
        readonly VoteInfo voteInfo;

        DisplayMode DisplayMode { get; set; }
        IQuest quest = new Quest();

        StringBuilder sb = new StringBuilder();
        const string cancelled = "Cancelled!";

        static readonly string[] rankWinnerLabels = { "Winner", "First Runner Up", "Second Runner Up", "Third Runner Up", "Honorable Mention" };

        public TallyOutput(IVoteCounter counter, VoteInfo info, IGeneralOutputOptions options)
        {
            voteCounter = counter;
            voteInfo = info;
            outputOptions = options;
        }
        #endregion


        public async Task<string> BuildOutputAsync(DisplayMode displayMode, CancellationToken token)
        {
            if (voteCounter.Quest == null)
                return string.Empty;
            if (voteCounter.TallyWasCanceled)
                return cancelled;

            quest = voteCounter.Quest;
            DisplayMode = displayMode;

            sb = new StringBuilder();

            try
            {
                await Task.Run(() => BuildGlobal(token)).ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
                return cancelled;
            }

            return sb.ToString();
        }

        /// <summary>
        /// General construction.  Add the header and any vote output.
        /// Surround by spoiler tags if requested by the display mode.
        /// </summary>
        /// <param name="token">Cancellation token so that processing can be cancelled.</param>
        private void BuildGlobal(CancellationToken token)
        {
            token.ThrowIfCancellationRequested();

            using (new Spoiler(sb, "Tally Results", DisplayMode == DisplayMode.SpoilerAll || outputOptions.GlobalSpoilers))
            {
                AddHeader(token);

                ConstructNormalOutput(token);
            }
        }

        /// <summary>
        /// Add the header indicating the title of the thread that was tallied,
        /// and the marker that this is a tally result (along with the program version number).
        /// </summary>
        private void AddHeader(CancellationToken token)
        {
            token.ThrowIfCancellationRequested();

            if (voteCounter.Quest is null)
                return;

            sb.Append("[b]Vote Tally");
            if (outputOptions.DebugMode)
                sb.Append(" (DEBUG)");
            sb.Append("[/b] : ");
            sb.Append(voteCounter.Title);

            (int first, int last) = GetPostRange();
            if (last > 0)
                sb.Append($" [Posts: {first}-{last}]");

            sb.AppendLine();

            sb.AppendLine($"[color=transparent]##### {ProductInfo.Name} {ProductInfo.Version}[/color]");

            if (voteCounter.Quest.UseCustomUsernameFilters && !string.IsNullOrEmpty(quest.CustomUsernameFilters))
            {
                sb.AppendLine($"[color=transparent]Username Filters: {quest.CustomUsernameFilters}[/color]");
            }

            if (voteCounter.Quest.UseCustomPostFilters && !string.IsNullOrEmpty(quest.CustomPostFilters))
            {
                sb.AppendLine($"[color=transparent]Post Filters: {quest.CustomPostFilters}[/color]");
            }

            sb.AppendLine();
        }

        /// <summary>
        /// Gets the post range of the votes that the VoteCounter tallied.
        /// Returns values of 0 if no valid posts are available.
        /// </summary>
        /// <param name="first">The first post number.</param>
        /// <param name="last">The last post number.</param>
        private (int first, int last) GetPostRange()
        {
            int first = 0;
            int last = 0;

            foreach (var post in voteCounter.PostsList)
            {
                if (first == 0 || post.Number < first)
                    first = post.Number;
                if (post.Number > last)
                    last = post.Number;
            }

            return (first, last);
        }

        /// <summary>
        /// Handle general organization of outputting the tally results,
        /// grouped by task.  Use VoteNodes if displaying in a compact
        /// mode, or just use the original votes if displaying in a normal
        /// mode.
        /// Display the vote, the count, and the voters, as appropriate.
        /// </summary>
        private void ConstructNormalOutput(CancellationToken token)
        {
            token.ThrowIfCancellationRequested();

            var votes = voteCounter.VoteBlockSupporters;

            var orderedVotes = votes.OrderByDescending(v => v.Value.Count);

            foreach (var vote in orderedVotes)
            {
                foreach (var line in vote.Key)
                {
                    sb.AppendLine(line.ToComparableString());
                }

                sb.AppendLine();

                foreach (var voter in vote.Value.Keys)
                {
                    sb.AppendLine(voter);
                }

                sb.AppendLine();
            }

        }


        /// <summary>
        /// Add a line break (for between tasks).
        /// Gets the line break text from the quest's forum adapter, since some
        /// can show hard rules, and some need to just use manual text.
        /// </summary>
        private void AddLineBreak()
        {
            if (DisplayMode == DisplayMode.Compact || DisplayMode == DisplayMode.CompactNoVoters)
                sb.AppendLine();

            sb.AppendLine(voteInfo.LineBreak);
            sb.AppendLine();
        }

    }
}
