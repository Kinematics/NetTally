using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;

namespace NetTally.Votes
{
    public static class VoteString
    {
        #region BBCode regexes
        // Regex for any opening or closing BBCode tag.
        static readonly Regex allBBCodeRegex = new Regex(@"(『(?:/)?(?:b|i|u|color)(?(?<=『color)=[^』]+)』)");
        // Regex for any opening BBCode tag.
        static readonly Regex openBBCodeRegex = new Regex(@"^『(b|i|u|color)(?(?<=『color)=[^』]+)』");
        // Regex for any closing BBCode tag.
        static readonly Regex closeBBCodeRegex = new Regex(@"^『/(b|i|u|color)』");
        #endregion

        #region Cleanup functions
        /// <summary>
        /// Make sure all BBCode tags have appropriate matching start/end tags.
        /// Any tags that don't have a proper match are removed.  This includes having
        /// a close tag before any opening tag (even if there's another open tag later on),
        /// or an open tag that's not followed by an end tag (even if there was an end tag
        /// earlier on).
        /// </summary>
        /// <param name="line">The vote line to modify.</param>
        /// <returns>Returns a normalized version of the vote line, with proper matching BBCode tags.</returns>
        public static string NormalizeContentBBCode(string line)
        {
            if (string.IsNullOrEmpty(line))
                return "";

            var lineSplit = allBBCodeRegex.Split(line);

            // If there were no BBCode tags, just return the original line.
            if (lineSplit.Length == 1)
                return line;

            // Run matches for opens and closes on all line splits, so we don't have to do it again later.
            Match[] openMatches = new Match[lineSplit.Length];
            Match[] closeMatches = new Match[lineSplit.Length];

            for (int i = 0; i < lineSplit.Length; i++)
            {
                openMatches[i] = openBBCodeRegex.Match(lineSplit[i]);
                closeMatches[i] = closeBBCodeRegex.Match(lineSplit[i]);
            }

            // Rebuild the result
            StringBuilder sb = new StringBuilder(line.Length);
            string tag;

            // A lookup table for how many times each tag is found.
            Dictionary<string, int> countTags = new Dictionary<string, int> { ["b"] = 0, ["i"] = 0, ["u"] = 0, ["color"] = 0 };

            for (int i = 0; i < lineSplit.Length; i++)
            {
                // Skip blank entries from the split
                if (string.IsNullOrEmpty(lineSplit[i]))
                    continue;

                if (openMatches[i].Success)
                {
                    tag = openMatches[i].Groups[1].Value;

                    for (int j = i + 1; j < lineSplit.Length; j++)
                    {
                        if (string.IsNullOrEmpty(lineSplit[j]))
                            continue;

                        if (closeMatches[j].Success && closeMatches[j].Groups[1].Value == tag)
                        {
                            if (countTags[tag] > 0)
                            {
                                countTags[tag]--;
                            }
                            else
                            {
                                // We've found a matching open tag.  Add this close tag and end the loop.
                                sb.Append(lineSplit[i]);
                                break;
                            }
                        }
                        else if (openMatches[j].Success && openMatches[j].Groups[1].Value == tag)
                        {
                            countTags[tag]++;
                        }
                    }

                    countTags[tag] = 0;
                }
                else if (closeMatches[i].Success)
                {
                    tag = closeMatches[i].Groups[1].Value;

                    for (int j = i - 1; j >= 0; j--)
                    {
                        if (string.IsNullOrEmpty(lineSplit[j]))
                            continue;

                        if (openMatches[j].Success && openMatches[j].Groups[1].Value == tag)
                        {
                            if (countTags[tag] > 0)
                            {
                                countTags[tag]--;
                            }
                            else
                            {
                                // We've found a matching open tag.  Add this close tag and end the loop.
                                sb.Append(lineSplit[i]);
                                break;
                            }
                        }
                        else if (closeMatches[j].Success && closeMatches[j].Groups[1].Value == tag)
                        {
                            countTags[tag]++;
                        }
                    }

                    countTags[tag] = 0;
                }
                else
                {
                    // This isn't a BBCode tag, so just add it to the pile.
                    sb.Append(lineSplit[i]);
                }
            }

            return sb.ToString();
        }

        #endregion
    }
}
