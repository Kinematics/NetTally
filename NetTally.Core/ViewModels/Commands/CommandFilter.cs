using System.Collections.Generic;

namespace NetTally.ViewModels.Commands
{
    /// <summary>
    /// The options for how to use the property filter list in ICommandFilter
    /// when determining whether CanExecuteChanged should be called.
    /// </summary>
    public enum PropertyFilterListOption
    {
        /// <summary>
        /// Ignore the property filter list.
        /// </summary>
        Ignore,
        /// <summary>
        /// Exclude any properties found in the property filter list.
        /// </summary>
        Exclude,
        /// <summary>
        /// Ignore anything that isn't in the property filter list.
        /// </summary>
        IncludeOnly,
    }

    /// <summary>
    /// Interface for implementing a filter on property changes reviewed for CanExecute checks.
    /// </summary>
    public interface ICommandFilter
    {
        /// <summary>
        /// What mode the command filter should operate in.
        /// </summary>
        public PropertyFilterListOption PropertyFilterListMode { get; }
        /// <summary>
        /// The list of properties that determine what causes CanExecuteChanged events.
        /// </summary>
        public HashSet<string> PropertyFilterList { get; }
    }

    /// <summary>
    /// Default implementation of an ICommandFilter.
    /// </summary>
    public class CommandFilter : ICommandFilter
    {
        public static readonly CommandFilter Default = new CommandFilter(PropertyFilterListOption.Ignore, new HashSet<string>());

        public PropertyFilterListOption PropertyFilterListMode { get; }

        public HashSet<string> PropertyFilterList { get; }

        public CommandFilter(PropertyFilterListOption mode, HashSet<string> list)
        {
            PropertyFilterListMode = mode;
            PropertyFilterList = list;
        }
    }
}
