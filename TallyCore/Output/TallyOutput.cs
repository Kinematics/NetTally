using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace NetTally.Output
{
    /// <summary>
    /// Class that can be put in a using() block to place a spoiler string in
    /// the provided string builder at construction and disposal.
    /// </summary>
    public struct Spoiler : IDisposable
    {
        StringBuilder SB;
        bool Display;

        /// <summary>
        /// Constructor.  Initialize required values so we know what and whether
        /// to display the spoiler tags.
        /// </summary>
        /// <param name="sb">The string builder that the text will be added to.</param>
        /// <param name="label">The label for the spoiler.  No spoiler will be displayed if it's null.</param>
        /// <param name="display">Whether we should display the text.</param>
        internal Spoiler(StringBuilder sb, string label, bool display)
        {
            SB = sb;
            Display = display && label != null;

            if (Display)
            {
                sb?.AppendLine($"[spoiler={label}]");
            }
        }

        /// <summary>
        /// Called when the using() is completed.  Close the tag if we're displaying text.
        /// </summary>
        public void Dispose()
        {
            if (Display)
            {
                SB?.AppendLine($"[/spoiler]");
            }
            SB = null;
        }
    }



    public class TallyOutput : ITextResultsProvider
    {
        IQuest Quest { get; set; }
        IVoteCounter VoteCounter { get; set; }
        DisplayMode DisplayMode { get; set; }

        StringBuilder sb { get; set; }


        public string BuildOutput(IQuest quest, IVoteCounter voteCounter, DisplayMode displayMode)
        {
            Quest = quest;
            VoteCounter = voteCounter;
            DisplayMode = displayMode;

            sb = new StringBuilder();

            BuildGlobal();

            return sb.ToString();
        }

        private void BuildGlobal()
        {
            using (var tallyResults = new Spoiler(sb, "Tally Results", DisplayMode == DisplayMode.SpoilerAll))
            {

            }

        }
    }
}
