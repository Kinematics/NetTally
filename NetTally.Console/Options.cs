using System.Text;
using CommandLine;
using NetTally.Output;
using NetTally.Votes;

namespace NetTally.CLI
{
    public class Options
    {
        [Option('t', "thread",  HelpText = "Input thread to tally.")]
        public string Thread { get; set; }

        [Option('k', "threadmark", Default = false, HelpText = "Try to find the starting post via threadmarks.")]
        public bool Threadmark { get; set; }

        [Option('v', "verbose", Default = false, HelpText = "Print details during execution.")]
        public bool Verbose { get; set; }

        [Option("start", Default = 1, HelpText = "The post to start tallying at.")]
        public int StartPost { get; set; }

        [Option("end", Default = 0, HelpText = "The post to end tallying at. 0 reads to end of thread.")]
        public int EndPost { get; set; }

        [Option("partition", Default = PartitionMode.None, HelpText = "The vote partitioning method to use. Case sensitive. Values: None (default), ByLine, ByBlock.")]
        public PartitionMode PartitionMode { get; set; }

        [Option("display", Default = DisplayMode.SpoilerVoters, HelpText = "The display mode to use for output. Case sensitive.  Values: SpoilerAll (default), Normal, Compact, NormalNoVoters, CompactNoVoters")]
        public DisplayMode DisplayMode { get; set; }

        [Option("threadmarkfilters", HelpText = "Filters used on threadmark titles.")]
        public string ThreadmarkFilters { get; set; }

        [Option("taskfilters", HelpText = "Filters used on tasks.")]
        public string TaskFilters { get; set; }

        [Option("posts", Default = 0, HelpText = "The number of posts per page for the forum.  Default of 0 will try to auto-detect.")]
        public int PostsPerPage { get; set; }

        [Option("whitespace", Default = false, HelpText = "Treat whitespace and punctuation as significant.")]
        public bool Whitespace { get; set; }

        [Option("noplanlabels", Default = false, HelpText = "Forbid vote label plan names.")]
        public bool ForbidPlanLabels { get; set; }

        [Option("noproxy", Default = false, HelpText = "Disable user proxy votes.")]
        public bool NoProxy { get; set; }

        [Option("pinproxy", Default = false, HelpText = "Treat all user proxy votes as pinned.")]
        public bool PinProxy { get; set; }

        [Option("nospoilers", Default = false, HelpText = "Ignore spoilers when reading posts.")]
        public bool NoSpoilers { get; set; }

        [Option("trim", Default = false, HelpText = "Trim extended vote lines.")]
        public bool Trim { get; set; }

        [Option("noranks", Default = false, HelpText = "Do not allow ranked voting.")]
        public bool NoRanks { get; set; }


        public static string GetUsage()
        {
            var usage = new StringBuilder();
            usage.AppendLine("NetTally Commandline 1.7.7");
            usage.AppendLine("-t, --thread: Required.  Specify the thread to tally.");
            usage.AppendLine("-k, --threadmark: Indicate that you want to try to find the starting post via threadmark.");
            usage.AppendLine("--start: Specify the starting tally post.  Default is 1.");
            usage.AppendLine("--end: Specify the ending tally post.  Default is 0 (read to end of thread).");
            usage.AppendLine("--posts: Specify the number of posts per thread page.  Default is auto-detect.");
            usage.AppendLine("-v, --verbose: Verbose mode.");
            return usage.ToString();
        }
    }
}
