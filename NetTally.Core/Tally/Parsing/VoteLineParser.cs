using System.Text;
using NetTally.Models.Creation;
using NetTally.Models.Defaults;
using NetTally.Models.Votes;

using static NetTally.Configure.Strings;

namespace NetTally.Tally.Parsing
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

        const char openBracket = '[';
        const char closeBracket = ']';
        const char openParen = '(';
        const char closeParen = ')';
        const char whitespace = ' ';
        const char xBox = '☒';
        const char checkBox = '☑';

        static readonly char[] apostrophes = ['‘', '’'];
        static readonly char[] quotations = ['“', '”', '‟', '„', '❝', '❞', '〝', '〞', '〟', '〃'];

        // Prefix chars: dash, en-dash, em-dash
        static readonly char[] prefixChars = ['-', '–', '—'];
        // Marker chars: X, check, numeric rank, rank marker, score marker, approval/disapproval
        static readonly char[] markerChars = ['x', 'X', '#', '%', '0', '1', '2', '3', '4', '5', '6', '7', '8', '9', '✓', '✔', '✗', '✘', 'Х', '☒', '☑', '+', '-'];
        // Newline chars
        static readonly char[] newlineChars = ['\r', '\n'];


        public static VoteLine ParseLineParts(ReadOnlySpan<char> line)
        {
            if (line.Length == 0)
                return VoteLine.Empty;

            StringBuilder prefixSB = new();
            StringBuilder markerSB = new();
            StringBuilder taskSB = new();
            StringBuilder contentSB = new();
            StringBuilder tempContent = new();

            Stack<TokenState> state = new();
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
                            currentState = TokenState.PostMarker;
                        }
                        else if (ch == OpenBBCode)
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
                            currentState = TokenState.PostMarker;
                        }
                        else if (ch == OpenBBCode)
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
                        else if (ch == closeBracket && markerSB.Length > 0)
                        {
                            currentState = TokenState.PostMarker;
                        }
                        else if (ch == OpenBBCode)
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
                        else if ((ch == openBracket || ch == openParen) && taskSB.Length == 0)
                        {
                            state.Push(currentState);
                            currentState = TokenState.Task;
                        }
                        else if (ch == OpenBBCode && taskSB.Length == 0)
                        {
                            state.Push(currentState);
                            currentState = TokenState.BBCode;
                            tempContent.Append(ch);
                        }
                        else if (ch == OpenStrike)
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
                        if (ch == closeBracket || ch == closeParen)
                        {
                            currentState = state.Pop();
                        }
                        else if (ch == OpenBBCode)
                        {
                            state.Push(currentState);
                            currentState = TokenState.BBCode;
                        }
                        else if (ch == OpenStrike)
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

                        if (ch == OpenStrike)
                        {
                            tempContent.Append("『s』");
                            state.Push(currentState);
                            currentState = TokenState.Strike;
                        }
                        else if (apostrophes.Contains(ch))
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
                        if (ch == CloseBBCode)
                        {
                            currentState = state.Pop();
                        }
                        break;
                    case TokenState.Strike:
                        // Strike-through text is only preserved in the content area
                        if (ch == CloseStrike)
                        {
                            tempContent.Append("『/s』");
                            currentState = state.Pop();
                        }
                        else if (ch == StrikeNewLine)
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

            if (currentState == TokenState.Content && tempContent.Length > 0)
                contentSB.Append(tempContent);

            Prefix prefix = Prefix.Create(prefixSB.Length);
            Marker marker = Marker.Create(markerSB.ToString());
            VoteTask voteTask = VoteTask.Create(taskSB.ToString());

            string content = VoteString.NormalizeContentBBCode(contentSB.ToString());
            VoteContent voteContent = VoteContent.Create(content);

            VoteLine voteLine = VoteLine.Create(prefix, marker, voteTask, voteContent);

            return voteLine;
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

            StringBuilder contentSB = new();
            bool bufferOn = true;
            int startBuffer = 0;

            // Use a stripped down version of the parsing state machine.
            Stack<TokenState> state = new();
            TokenState currentState = TokenState.None;

            for (int c = 0; c < input.Length; c++)
            {
                char ch = input[c];

                switch (currentState)
                {
                    case TokenState.None:
                        if (ch == OpenBBCode)
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
                        if (ch == CloseBBCode)
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
                contentSB.Append(input[startBuffer..]);
            }

            return contentSB.ToString();
        }
    }
}
