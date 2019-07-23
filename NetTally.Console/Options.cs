﻿using NetTally.Output;
using NetTally.Votes;
using System.Collections.Generic;
using CommandLine;
using CommandLine.Text;

namespace NetTally.CLI
{
    /// <summary>
    /// Class defining the accepted commandline parameters.
    /// </summary>
    public class Options
    {
        [Value(0, HelpText = "Thread to tally.", Required = true)]
        public string Thread { get; set; } = "";

        [Option('v', "verbose", Default = false, HelpText = "Print page load details during execution.")]
        public bool Verbose { get; set; }

        [Option('k', "threadmark", Default = false, SetName = "ByThreadmark", HelpText = "Try to find the starting post via threadmarks.")]
        public bool Threadmark { get; set; }

        [Option('s', "start", SetName = "ByPost", HelpText = "The post to start tallying at.")]
        public int? StartPost { get; set; }

        [Option('e', "end", SetName = "ByPost", HelpText = "The post to end tallying at. 0 reads to end of thread.")]
        public int? EndPost { get; set; }

        [Option("partition", Default = PartitionMode.None, HelpText = "The vote partitioning method to use. Case sensitive. Values: None (default), ByLine, ByBlock.")]
        public PartitionMode PartitionMode { get; set; }

        [Option("display", Default = DisplayMode.SpoilerVoters, HelpText = "The display mode to use for output. Case sensitive.  Values: SpoilerAll (default), Normal, Compact, NormalNoVoters, CompactNoVoters")]
        public DisplayMode DisplayMode { get; set; }

        [Option("spoilerall", Default = false, HelpText = "Wrap spoilers around the entire displayed output.")]
        public bool SpoilerAll { get; set; }

        [Option("threadmarkfilters", HelpText = "Filters used on threadmark titles.")]
        public string ThreadmarkFilters { get; set; } = "";

        [Option("usernamefilters", HelpText = "Filters used on usernames.")]
        public string UsernameFilters { get; set; } = "";

        [Option("postfilters", HelpText = "Filters used on post numbers or IDs.")]
        public string PostFilters { get; set; } = "";

        [Option("taskfilters", HelpText = "Filters used on tasks.")]
        public string TaskFilters { get; set; } = "";

        [Option("postsperpage", Default = 0, HelpText = "The number of posts per page for the forum.  Default of 0 will try to auto-detect.")]
        public int PostsPerPage { get; set; }

        [Option("whitespace", Default = false, HelpText = "Treat whitespace and punctuation as significant.")]
        public bool Whitespace { get; set; }

        [Option("case", Default = false, HelpText = "Treat case as significant.")]
        public bool Case { get; set; }

        [Option("nolabelplans", Default = false, HelpText = "Forbid vote label plan names.")]
        public bool ForbidPlanLabels { get; set; }

        [Option("canupdateplans", Default = false, HelpText = "Allow users to update plans they wrote in later posts.")]
        public bool AllowUsersToUpdatePlans { get; set; }

        [Option("mustlabelplans", Default = false, HelpText = "Plan references must be labeled as such.")]
        public bool MustLabelPlanReferences { get; set; }

        [Option("nouserproxy", Default = false, HelpText = "Disable user proxy votes.")]
        public bool NoUserProxy { get; set; }

        [Option("forcepinproxy", Default = false, HelpText = "Treat all user proxy votes as pinned.")]
        public bool ForcePinProxy { get; set; }

        [Option("ignorespoilers", Default = false, HelpText = "Ignore spoilers when reading posts.")]
        public bool IgnoreSpoilers { get; set; }

        [Option("trim", Default = false, HelpText = "Trim extended vote lines.")]
        public bool Trim { get; set; }

        [Option("display0votes", Default = false, HelpText = "Display plans that do not have any user votes.")]
        public bool Display0Votes { get; set; }

        [Option("disablewebproxy", Default = false, HelpText = "Disable use of internal web proxy when loading web pages.")]
        public bool DisableWebProxy { get; set; }

        [Option("debug", Default = false, Hidden = true, HelpText = "Enable debug mode.")]
        public bool Debug { get; set; }

        [Usage(ApplicationAlias = "dotnet nettally.dll")]
        public static IEnumerable<Example> Examples
        {
            get {
                yield return new Example("Basic use", UnParserSettings.WithGroupSwitchesOnly(), new Options
                {
                    Thread = "https://forums.sufficientvelocity.com/threads/threadname.13528/"
                });
                yield return new Example("Set partitioning", UnParserSettings.WithGroupSwitchesOnly(), new Options
                {
                    Thread = "https://forums.sufficientvelocity.com/threads/threadname.13528/",
                    PartitionMode = PartitionMode.ByBlock
                });
                yield return new Example("Specify a post range", UnParserSettings.WithGroupSwitchesOnly(), new Options
                {
                    Thread = "https://forums.sufficientvelocity.com/threads/threadname.13528/",
                    StartPost = 300,
                    EndPost = 400
                });
            }
        }
    }
}
