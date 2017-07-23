using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using NetTally.Utility;

namespace NetTally.Votes.Experiment2
{
    class MessageVoteContent
    {
        #region Public
        public bool Valid => VoteLines.Count > 0;
        public List<VoteLine> VoteLines { get; } = new List<VoteLine>();

        public MessageVoteContent(string message)
        {
            if (string.IsNullOrEmpty(message))
                return;

            BreakMessageIntoVoteLines(message);
        }
        #endregion

        #region Private utility
        private void BreakMessageIntoVoteLines(string message)
        {
            var messageLines = message.GetStringLines();

            foreach (var line in messageLines)
            {
                var (isVoteLine, flagIgnore, voteLine) = AnalyzeLine(line);

                if (flagIgnore)
                {
                    VoteLines.Clear();
                    break;
                }

                if (isVoteLine)
                {
                    VoteLines.Add(voteLine);
                }
            }
        }
        #endregion

        #region Line parsing and analysis

        const char openBBCode = '『';
        const char closeBBCode = '』';
        const char openBracket = '[';
        const char closeBracket = ']';
        const char ignoreChar = '#';
        const char prefixChar = '-';
        const char whitespace = ' ';

        static char[] markerChars = new char[] { 'x', 'X', '+', '-', '#', '0', '1', '2', '3', '4', '5', '6', '7', '8', '9', '✓', '✔' };


        /// <summary>
        /// State machine analysis of the provided message line, to minimize processing cost overhead.
        /// </summary>
        /// <param name="line">The message line.</param>
        /// <returns>Returns a tuple indicating if this was a vote line, whether this line indicated
        /// that this post should be ignored, and the encapsulated vote line, if applicable.</returns>
        /// <exception cref="InvalidOperationException">Throws if it gets into an unknown state.</exception>
        internal static (bool isVoteLine, bool flagIgnore, VoteLine voteLine) AnalyzeLine(string line)
        {
            bool isVoteLine = false;
            bool flagIgnore = false;
            VoteLine voteLine = null;

            if (string.IsNullOrEmpty(line))
                return (isVoteLine, flagIgnore, voteLine);

            StringBuilder prefixSB = new StringBuilder();
            StringBuilder markerSB = new StringBuilder();
            StringBuilder taskSB = new StringBuilder();
            StringBuilder contentSB = new StringBuilder();
            StringBuilder tempContent = new StringBuilder();

            MarkerType markerType = MarkerType.None;
            int markerValue = 0;

            int ignoreCharCount = 0;

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
                        else if (ch == openBBCode)
                        {
                            state.Push(currentState);
                            currentState = TokenState.BBCode;
                        }
                        else if (ch == prefixChar)
                        {
                            state.Push(currentState);
                            currentState = TokenState.Prefix;
                            prefixSB.Append(ch);
                        }
                        else if (ch == ignoreChar)
                        {
                            state.Push(currentState);
                            currentState = TokenState.Ignore;
                            ignoreCharCount = 1;
                        }
                        else if (ch == openBracket)
                        {
                            state.Push(currentState);
                            currentState = TokenState.Marker;
                        }
                        else
                        {
                            goto doneExamining;
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
                    case TokenState.Ignore:
                        if (ch == ignoreChar)
                        {
                            ignoreCharCount++;
                            if (ignoreCharCount == 5)
                            {
                                flagIgnore = true;
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
                    case TokenState.Prefix:
                        if (ch == whitespace)
                        {
                            continue;
                        }
                        else if (ch == prefixChar)
                        {
                            prefixSB.Append(ch);
                        }
                        else if (ch == openBBCode)
                        {
                            state.Push(currentState);
                            currentState = TokenState.BBCode;
                        }
                        else if (ch == openBracket)
                        {
                            currentState = TokenState.Marker;
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
                        else if (ch == openBBCode)
                        {
                            state.Push(currentState);
                            currentState = TokenState.BBCode;
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
                                isVoteLine = true;
                                currentState = TokenState.PostMarker;
                            }
                            else
                            {
                                goto doneExamining;
                            }
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
                    default:
                        throw new InvalidOperationException($"Unknown token state value: {currentState}.");
                }
            }

            doneExamining:

            if (isVoteLine)
                voteLine = new VoteLine(prefixSB.ToString(), markerSB.ToString(), taskSB.ToString(), contentSB.ToString(), markerType, markerValue);

            return (isVoteLine, flagIgnore, voteLine);
        }


        static readonly Regex markerRegex = new Regex(@"(?<marker>(?<vote>[xX✓✔])|(?:(?<rank>[#])|(?<score>[+]))?(?<value>[1-9])|(?<approval>[-+])|(?<continuation>[*]))");

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
                        markerType = MarkerType.Basic;
                    else if (m.Groups["rank"].Success)
                        markerType = MarkerType.Rank;
                    else if (m.Groups["score"].Success)
                        markerType = MarkerType.Score;
                    else if (m.Groups["value"].Success)
                        markerType = MarkerType.Rank; // Default value type here.
                    else if (m.Groups["approval"].Success)
                    {
                        markerType = MarkerType.Approval;
                        if (m.Groups["approval"].Value == "+")
                            markerValue = 1;
                        else
                            markerValue = -1;
                    }
                    else if (m.Groups["continuation"].Success)
                        markerType = MarkerType.Continuation;

                    if (m.Groups["value"].Success)
                    {
                        markerValue = int.Parse(m.Groups["value"].Value);
                    }

                }
            }

            return (markerType, markerValue);
        }

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

        #endregion
    }
}
