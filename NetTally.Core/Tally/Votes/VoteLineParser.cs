using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace NetTally.Votes
{
    public static class VoteLineParser
    {
        private enum TokenState
        {
            None,
            BBCode,
            Ignore,
            Prefix,
            Marker,
            PostMarker,
            Task,
            Content,
            Strike,
        }

        const char openBBCode = '『';
        const char closeBBCode = '』';
        const char openBracket = '[';
        const char closeBracket = ']';
        const char whitespace = ' ';
        const char xBox = '☒';
        const char checkBox = '☑';
        const char openStrike = '❰';
        const char closeStrike = '❱';
        const char strikeNewline = '⦂';

        static readonly char[] apostraphes = new char[] { '‘', '’' };
        static readonly char[] quotations = new char[] { '“', '〃', '”' };

        // Prefix chars: dash, en-dash, em-dash
        static readonly char[] prefixChars = new char[] { '-', '–', '—' };
        // Marker chars: X, check, numeric rank, rank marker, score marker, approval/disapproval
        static readonly char[] markerChars = new char[] { 'x', 'X', '#', '%', '0', '1', '2', '3', '4', '5', '6', '7', '8', '9', '✓', '✔', '✗', '✘', 'Х', '☒', '☑', '+', '-' };
        // Newline chars
        static readonly char[] newlineChars = new char[] { '\r', '\n' };

        /// <summary>
        /// Takes a line of text and attempts to parse it, looking for a valid vote line.
        /// If it's a valid vote line, returns a VoteLine. Otherwise returns null.
        /// </summary>
        /// <param name="line">A line of text to parse.</param>
        /// <returns>Returns a VoteLine if the provided text is a valid vote.</returns>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Design", "MA0051:Method is too long", Justification = "State machine")]
        public static VoteLine? ParseLine(ReadOnlySpan<char> line)
        {
            if (line.Length == 0)
                return null;

            StringBuilder prefixSB = new StringBuilder();
            StringBuilder markerSB = new StringBuilder();
            StringBuilder taskSB = new StringBuilder();
            StringBuilder contentSB = new StringBuilder();
            StringBuilder tempContent = new StringBuilder();

            MarkerType markerType = MarkerType.None;
            int markerValue = 0;

            Stack<TokenState> state = new Stack<TokenState>();
            TokenState currentState = TokenState.None;

            for (int c = 0; c < line.Length; c++)
            {
                char ch = line[c];

                // Skip newlines entirely, if they somehow get into the line we're parsing.
                if (newlineChars.Contains(ch))
                    continue;

                switch (currentState)
                {
                    case TokenState.None:
                        if (ch == whitespace)
                        {
                            continue;
                        }
                        else if (prefixChars.Contains(ch))
                        {
                            prefixSB.Append(ch);
                            currentState = TokenState.Prefix;
                        }
                        else if (ch == openBracket)
                        {
                            currentState = TokenState.Marker;
                        }
                        else if (ch == xBox || ch == checkBox)
                        {
                            // Shortcut for a complete marker
                            markerSB.Append(ch);
                            (markerType, markerValue) = GetMarkerType(markerSB.ToString());
                            currentState = TokenState.PostMarker;
                        }
                        else if (ch == openBBCode)
                        {
                            state.Push(currentState);
                            currentState = TokenState.BBCode;
                        }
                        else
                        {
                            goto doneExamining;
                        }
                        break;
                    case TokenState.Prefix:
                        if (ch == whitespace)
                        {
                            continue;
                        }
                        else if (prefixChars.Contains(ch))
                        {
                            prefixSB.Append(ch);
                        }
                        else if (ch == openBracket)
                        {
                            currentState = TokenState.Marker;
                        }
                        else if (ch == xBox || ch == checkBox)
                        {
                            // Shortcut for a complete marker
                            markerSB.Append(ch);
                            (markerType, markerValue) = GetMarkerType(markerSB.ToString());
                            currentState = TokenState.PostMarker;
                        }
                        else if (ch == openBBCode)
                        {
                            state.Push(currentState);
                            currentState = TokenState.BBCode;
                        }
                        else
                        {
                            goto doneExamining;
                        }
                        break;
                    case TokenState.Marker:
                        if (ch == whitespace)
                        {
                            continue;
                        }
                        else if (markerChars.Contains(ch))
                        {
                            markerSB.Append(ch);
                        }
                        else if (ch == closeBracket)
                        {
                            (markerType, markerValue) = GetMarkerType(markerSB.ToString());
                            if (markerType != MarkerType.None)
                            {
                                currentState = TokenState.PostMarker;
                            }
                            else
                            {
                                goto doneExamining;
                            }
                        }
                        else if (ch == openBBCode)
                        {
                            state.Push(currentState);
                            currentState = TokenState.BBCode;
                        }
                        else
                        {
                            goto doneExamining;
                        }
                        break;
                    case TokenState.PostMarker:
                        if (ch == whitespace)
                        {
                            if (tempContent.Length > 0)
                                tempContent.Append(ch);

                            continue;
                        }
                        else if (ch == openBracket && taskSB.Length == 0)
                        {
                            state.Push(currentState);
                            currentState = TokenState.Task;
                        }
                        else if (ch == openBBCode && taskSB.Length == 0)
                        {
                            state.Push(currentState);
                            currentState = TokenState.BBCode;
                            tempContent.Append(ch);
                        }
                        else if (ch == openStrike)
                        {
                            tempContent.Append("『s』");
                            state.Push(currentState);
                            currentState = TokenState.Strike;
                        }
                        else
                        {
                            contentSB.Append(tempContent);
                            tempContent.Clear();
                            contentSB.Append(ch);
                            currentState = TokenState.Content;
                        }
                        break;
                    case TokenState.Task:
                        tempContent.Clear();
                        if (ch == closeBracket)
                        {
                            currentState = state.Pop();
                        }
                        else if (ch == openBBCode)
                        {
                            state.Push(currentState);
                            currentState = TokenState.BBCode;
                        }
                        else if (ch == openStrike)
                        {
                            state.Push(currentState);
                            currentState = TokenState.Strike;
                        }
                        else
                        {
                            taskSB.Append(ch);
                        }
                        break;
                    case TokenState.Content:
                        if (tempContent.Length > 0)
                        {
                            contentSB.Append(tempContent);
                            tempContent.Clear();
                        }

                        if (ch == openStrike)
                        {
                            tempContent.Append("『s』");
                            state.Push(currentState);
                            currentState = TokenState.Strike;
                        }
                        else if (apostraphes.Contains(ch))
                        {
                            contentSB.Append('\'');
                        }
                        else if (quotations.Contains(ch))
                        {
                            contentSB.Append('"');
                        }
                        else
                        {
                            contentSB.Append(ch);
                        }
                        break;
                    case TokenState.BBCode:
                        if (state.Peek() == TokenState.PostMarker)
                        {
                            tempContent.Append(ch);
                        }
                        if (ch == closeBBCode)
                        {
                            currentState = state.Pop();
                        }
                        break;
                    case TokenState.Strike:
                        // Strike-through text is only preserved in the content area
                        if (ch == closeStrike)
                        {
                            tempContent.Append("『/s』");
                            currentState = state.Pop();
                        }
                        else if (ch == strikeNewline)
                        {
                            // If we hit embedded newlines, bail out entirely.
                            // Take whatever's been done up to that point.
                            tempContent.Clear();
                            currentState = state.Pop();
                            goto doneExamining;
                        }
                        else
                        {
                            tempContent.Append(ch);
                        }
                        break;
                    default:
                        throw new InvalidOperationException($"Unknown token state value: {currentState}.");
                }
            }

        doneExamining:

            if (currentState == TokenState.Content)
            {
                string content = VoteString.NormalizeContentBBCode(contentSB.ToString());
                return new VoteLine(prefixSB.ToString(), markerSB.ToString(), taskSB.ToString(), content, markerType, markerValue);
            }

            return null;
        }

        /// <summary>
        /// Function to strip all BBCode from the provided input string.
        /// </summary>
        /// <param name="input">The input string to strip BBCode from.</param>
        /// <returns>Returns the string without any BBCode.</returns>
        public static string StripBBCode(ReadOnlySpan<char> input)
        {
            if (input.Length == 0)
                return "";

            StringBuilder contentSB = new StringBuilder();
            bool bufferOn = true;
            int startBuffer = 0;

            // Use a stripped down version of the parsing state machine.
            Stack<TokenState> state = new Stack<TokenState>();
            TokenState currentState = TokenState.None;

            for (int c = 0; c < input.Length; c++)
            {
                char ch = input[c];

                switch (currentState)
                {
                    case TokenState.None:
                        if (ch == openBBCode)
                        {
                            if (bufferOn)
                            {
                                contentSB.Append(input[startBuffer..c]);
                                bufferOn = false;
                            }
                            state.Push(currentState);
                            currentState = TokenState.BBCode;
                        }
                        break;
                    case TokenState.BBCode:
                        if (ch == closeBBCode)
                        {
                            currentState = state.Pop();
                            startBuffer = c + 1;
                            bufferOn = true;
                        }
                        break;
                    default:
                        throw new InvalidOperationException($"Unknown token state value: {currentState}.");
                }
            }

            if (bufferOn)
            {
                contentSB.Append(input.Slice(startBuffer));
            }

            return contentSB.ToString();
        }


        static readonly Regex markerRegex = new Regex(@"^(?<marker>(?<vote>[xX✓✔✗✘Х☒☑])|(?<rank>#)?(?<value>[0-9]{1,3})(?<score>%)?|(?<approval>[-+]))$",
            RegexOptions.None, TimeSpan.FromSeconds(1));

        /// <summary>
        /// Examines a provided vote marker string and determines what type of marker it is.
        /// Marker values are on a scale of 0-100 for all but rankings (which may change).
        /// A standard X vote is given the same value as a 100% score — 100.
        /// Approval votes are given 100, while disapproval votes are given 0.
        /// This allows different markers to be somewhat interchangeable, or at least comparable,
        /// without too much extra effort.
        /// </summary>
        /// <param name="marker">The marker to examine.</param>
        /// <returns>Returns the type of marker found, and its intrinsic value.</returns>
        private static (MarkerType markerType, int markerValue) GetMarkerType(string marker)
        {
            MarkerType markerType = MarkerType.None;
            int markerValue = 0;

            if (!string.IsNullOrEmpty(marker))
            {
                Match m = markerRegex.Match(marker);
                if (m.Success)
                {
                    if (m.Groups["vote"].Success)
                        markerType = MarkerType.Vote;
                    else if (m.Groups["rank"].Success)
                        markerType = MarkerType.Rank;
                    else if (m.Groups["score"].Success)
                        markerType = MarkerType.Score;
                    else if (m.Groups["value"].Success)
                        markerType = MarkerType.Rank; // Default value type if no # or % used.
                    else if (m.Groups["approval"].Success)
                        markerType = MarkerType.Approval;
                    else
                        markerType = MarkerType.None;

                    if (markerType == MarkerType.Vote)
                    {
                        markerValue = 100;
                    }
                    else if (markerType == MarkerType.Approval)
                    {
                        if (string.Equals(m.Groups["approval"].Value, "+", StringComparison.Ordinal))
                            markerValue = 80;
                        else
                            markerValue = 20;
                    }
                    else if (m.Groups["value"].Success)
                    {
                        markerValue = int.Parse(m.Groups["value"].Value, System.Globalization.CultureInfo.CurrentCulture);

                        if (markerType == MarkerType.Rank)
                        {
                            if (markerValue < 1)
                                markerValue = 1;
                            if (markerValue > 99)
                                markerValue = 99;
                        }
                        else if (markerType == MarkerType.Score)
                        {
                            if (markerValue > 100)
                                markerValue = 100;
                        }
                    }

                }
            }

            return (markerType, markerValue);
        }

    }
}
