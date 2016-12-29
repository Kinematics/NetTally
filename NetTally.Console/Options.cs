using System;
using System.Collections.Generic;
using System.Text;
using CommandLine;
using CommandLine.Text;

namespace NetTally.CLI
{
    public class Options
    {
        [Option('t', "thread",  HelpText = "Input thread to tally.")]
        public string Thread { get; set; }

        [Option('k', "threadmark", Default = false, HelpText = "Try to find the start by via threadmarks.")]
        public bool Threadmark { get; set; }

        [Option("start", Default = 1, HelpText = "The post to start tallying at.")]
        public int StartPost { get; set; }

        [Option("end", Default = 0, HelpText = "The post to end tallying at. 0 reads to end of thread.")]
        public int EndPost { get; set; }

        [Option("posts", Default = 0, HelpText = "The number of posts per page for the forum.")]
        public int PostsPerPage { get; set; }

        [Option('v', "verbose", Default = false, HelpText = "Print details during execution.")]
        public bool Verbose { get; set; }

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
