using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using NetTally.Votes;

namespace NetTally.Experiment3
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
        }

        const char openBBCode = '『';
        const char closeBBCode = '』';
        const char openBracket = '[';
        const char closeBracket = ']';
        const char whitespace = ' ';
        const char xBox = '☒';
        const char checkBox = '☑';

        // Prefix chars: dash, en-dash, em-dash
        static readonly char[] prefixChars = new char[] { '-', '–', '—' };
        // Marker chars: X, check, numeric rank, rank marker, score marker, approval/disapproval
        static readonly char[] markerChars = new char[] { 'x', 'X', '#', '%', '0', '1', '2', '3', '4', '5', '6', '7', '8', '9', '✓', '✔', '✗', '✘', '☒', '☑', '+', '-' };
        // Newline chars
        static readonly char[] newlineChars = new char[] { '\r', '\n' };

        static readonly StringBuilder prefixSB = new StringBuilder();
        static readonly StringBuilder markerSB = new StringBuilder();
        static readonly StringBuilder taskSB = new StringBuilder();
        static readonly StringBuilder contentSB = new StringBuilder();
        static readonly StringBuilder tempContent = new StringBuilder();

        /// <summary>
        /// Takes a line of text and attempts to parse it, looking for a valid vote line.
        /// If it's a valid vote line, returns a VoteLine. Otherwise returns null.
        /// </summary>
        /// <param name="line">A line of text to parse.</param>
        /// <returns>Returns a VoteLine if the provided text is a valid vote.</returns>
        public static VoteLine? ParseLine(ReadOnlySpan<char> line)
        {
            if (line.Length == 0)
                return null;

            prefixSB.Clear();
            markerSB.Clear();
            taskSB.Clear();
            contentSB.Clear();
            tempContent.Clear();

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
                            currentState = TokenState.Prefix;
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
                        else
                        {
                            contentSB.Append(tempContent.ToString());
                            contentSB.Append(ch);
                            currentState = TokenState.Content;
                        }
                        break;
                    case TokenState.Task:
                        if (ch == closeBracket)
                        {
                            currentState = state.Pop();
                        }
                        else if (ch == openBBCode)
                        {
                            state.Push(currentState);
                            currentState = TokenState.BBCode;
                        }
                        else
                        {
                            taskSB.Append(ch);
                        }
                        break;
                    case TokenState.Content:
                        {
                            contentSB.Append(line[c..]);
                            goto doneExamining;
                        }
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

            contentSB.Clear();

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
                            state.Push(currentState);
                            currentState = TokenState.BBCode;
                        }
                        else
                        {
                            contentSB.Append(ch);
                        }
                        break;
                    case TokenState.BBCode:
                        if (ch == closeBBCode)
                        {
                            currentState = state.Pop();
                        }
                        break;
                    default:
                        throw new InvalidOperationException($"Unknown token state value: {currentState}.");
                }
            }

            return contentSB.ToString();
        }


        static readonly Regex markerRegex = new Regex(@"^(?<marker>(?<vote>[xX✓✔✗✘☒☑])|(?<rank>#)?(?<value>[0-9]{1,3})(?<score>%)?|(?<approval>[-+]))$");

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
                        if (m.Groups["approval"].Value == "+")
                            markerValue = 80;
                        else
                            markerValue = 20;
                    }
                    else if (m.Groups["value"].Success)
                    {
                        markerValue = int.Parse(m.Groups["value"].Value);

                        if (markerType == MarkerType.Rank)
                        {
                            if (markerValue < 1)
                                markerValue = 1;
                            if (markerValue > 9)
                                markerValue = 9; // Allow higher?
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
