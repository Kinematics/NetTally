using System;
using System.Text.RegularExpressions;

namespace NetTally.Filters
{
    public class TaskFilter : BaseFilter
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="TaskFilter"/> class.
        /// </summary>
        /// <param name="quest">The quest.</param>
        public TaskFilter(IQuest quest)
            : base(defaultTaskFilter, TaskRegex(quest))
        {
        }

        /// <summary>
        /// The default threadmark regex.
        /// </summary>
        static Regex defaultTaskFilter = null;

        /// <summary>
        /// Gets the custom threadmark regex for the quest, if any.
        /// </summary>
        /// <param name="quest">The quest.</param>
        /// <returns>Returns a custom regex, or null.</returns>
        static Regex TaskRegex(IQuest quest)
        {
            if (quest != null && quest.UseCustomTaskFilters)
            {
                return CreateCustomRegex(quest.CustomTaskFilters, true);
            }

            return null;
        }
    }
}
