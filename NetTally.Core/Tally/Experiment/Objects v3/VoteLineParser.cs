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
        static readonly char[] markerChars = new char[] { 'x', 'X', '#', '%', '0', '1', '2', '3', '4', '5', '6', '7', '8', '9', '√', '✓', '✔', '×', '☒', '☑', '+', '-' };

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

            StringBuilder prefixSB = new StringBuilder();
            StringBuilder markerSB = new StringBuilder();
            StringBuilder taskSB = new StringBuilder();
            StringBuilder contentSB = new StringBuilder();
            StringBuilder tempContent = new StringBuilder();

            MarkerType markerType = MarkerType.None;
            int markerValue = 0;

            Stack<TokenState> state = new Stack<TokenState>();
            TokenState currentState = TokenState.None;

            foreach (var ch in line)
            {
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
                            markerType = MarkerType.Vote;
                            markerValue = 100;
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
                            markerType = MarkerType.Vote;
                            markerValue = 100;
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
                        contentSB.Append(ch);
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
                    default:
                        throw new InvalidOperationException($"Unknown token state value: {currentState}.");
                }
            }

        doneExamining:

            if (currentState == TokenState.Content)
                return new VoteLine(prefixSB.ToString(), markerSB.ToString(), taskSB.ToString(), contentSB.ToString(), markerType, markerValue);

            return null;
        }


        static readonly Regex markerRegex = new Regex(@"(?<marker>(?<vote>[xX×√✓✔☒☑])|(?<rank>#)?(?<value>[0-9]{1,3})(?<score>%)?|(?<approval>[-+]))");

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
                        markerType = MarkerType.Rank; // Default value type here.
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
                            markerValue = 100;
                        else
                            markerValue = 0;
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
