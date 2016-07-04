using System;
using System.Text.RegularExpressions;
using NetTally.Filters;

namespace NetTally.Adapters
{
    public class ThreadmarkFilter : BaseFilter
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ThreadmarkFilter"/> class.
        /// </summary>
        /// <param name="quest">The quest.</param>
        public ThreadmarkFilter(IQuest quest)
            : base(defaultThreadmarkRegex, ThreadmarkRegex(quest))
        {
        }

        /// <summary>
        /// The default threadmark regex.
        /// </summary>
        static Regex defaultThreadmarkRegex = new Regex(@"\bomake\b", RegexOptions.IgnoreCase);

        /// <summary>
        /// Gets the custom threadmark regex for the quest, if any.
        /// </summary>
        /// <param name="quest">The quest.</param>
        /// <returns>Returns a custom regex, or null.</returns>
        static Regex ThreadmarkRegex(IQuest quest)
        {
            if (quest != null && quest.UseCustomThreadmarkFilters)
            {
                return CreateCustomRegex(quest.CustomThreadmarkFilters);
            }

            return null;
        }
    }
}
