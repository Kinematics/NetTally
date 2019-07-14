using System;
using System.Text.RegularExpressions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NetTally.Utility;
using NetTally.Utility.Comparers;
using NetTally.Votes;

namespace NetTally.Tests.Benchmark
{
    [TestClass]
    [Ignore]
    public class Benchmarks
    {
        static IServiceProvider serviceProvider;
        readonly static Regex voteLineRegex = new Regex(@"^[-\s]*\[\s*[xX✓✔1-9]\s*\]");

        [ClassInitialize]
        public static void ClassInit(TestContext context)
        {
            serviceProvider = TestStartup.ConfigureServices();
            var hash = serviceProvider.GetRequiredService<IHash>();
            Agnostic.HashStringsUsing(hash.HashFunction);
        }

        [TestMethod]
        public void Time_VoteLine_Parsing_and_Construction()
        {
            using (var rp = new RegionProfiler("Warm up"))
            {

            }

            string input1, input2, input3, input4;
            string? line1, line2, line3, line4;
            string? prefix, marker, task, content;
            VoteLine? vote1, vote2, vote3, vote4;
            object o1, o2, o3, o4;

            input1 = "It depends, but the general trend is that it's easier once you've done it once. So, one day you're at 5 San and you killed a Witch by binding it's existence to a giant, abstract clock made of your own crystallized blood, dooming it to die when it strikes twelve.";
            input2 = "[X][Other] Ask about the time you \"met\" before. Make sure she remembers it.";
            input3 = "[X] Ask about the time you \"met\" before. Make sure she remembers it.";
            input4 = "[x][To Do?] 『b』A 『url='http://example.com/image.jpg'』normal『/url』 day at the beach.『/b』";

            using (var rp = new RegionProfiler("Baseline with empty vote lines"))
            {
                for (int i = 0; i < 2500; i++)
                {
                    o1 = VoteLine.Empty;
                    o2 = VoteLine.Empty;
                    o3 = VoteLine.Empty;
                    o4 = VoteLine.Empty;
                }
            }

            using (var rp = new RegionProfiler("Time constructing vote lines"))
            {
                for (int i = 0; i < 2500; i++)
                {
                    vote1 = new VoteLine("", "X", "", "Ask about the time you \"met\" before. Make sure she remembers it.", MarkerType.Vote, 100);
                    vote2 = new VoteLine("", "X", "", "Ask about the time you \"met\" before. Make sure she remembers it.", MarkerType.Vote, 100);
                    vote3 = new VoteLine("", "X", "", "Ask about the time you \"met\" before. Make sure she remembers it.", MarkerType.Vote, 100);
                    vote4 = new VoteLine("", "X", "", "Ask about the time you \"met\" before. Make sure she remembers it.", MarkerType.Vote, 100);
                }
            }

            using (var rp = new RegionProfiler("Time parsing lines"))
            {
                for (int i = 0; i < 2500; i++)
                {
                    vote1 = VoteLineParser.ParseLine(input1);
                    vote2 = VoteLineParser.ParseLine(input2);
                    vote3 = VoteLineParser.ParseLine(input3);
                    vote4 = VoteLineParser.ParseLine(input4);
                }
            }

            using (var rp = new RegionProfiler("Time parsing strings"))
            {
                for (int i = 0; i < 2500; i++)
                {
                    if (voteLineRegex.Match(VoteString.RemoveBBCode(input1)).Success)
                    {
                        line1 = VoteString.CleanVoteLineBBCode(input1);
                        VoteString.GetVoteComponents(line1, out prefix, out marker, out task, out content);
                    }
                    if (voteLineRegex.Match(VoteString.RemoveBBCode(input2)).Success)
                    {
                        line2 = VoteString.CleanVoteLineBBCode(input2);
                        VoteString.GetVoteComponents(line2, out prefix, out marker, out task, out content);
                    }
                    if (voteLineRegex.Match(VoteString.RemoveBBCode(input3)).Success)
                    {
                        line3 = VoteString.CleanVoteLineBBCode(input3);
                        VoteString.GetVoteComponents(line3, out prefix, out marker, out task, out content);
                    }
                    if (voteLineRegex.Match(VoteString.RemoveBBCode(input4)).Success)
                    {
                        line4 = VoteString.CleanVoteLineBBCode(input4);
                        VoteString.GetVoteComponents(line4, out prefix, out marker, out task, out content);
                    }
                }
            }

            using (var rp = new RegionProfiler("Comparing parsed lines"))
            {
                vote1 = VoteLineParser.ParseLine(input1);
                vote2 = VoteLineParser.ParseLine(input2);
                vote3 = VoteLineParser.ParseLine(input3);
                vote4 = VoteLineParser.ParseLine(input4);

                for (int i = 0; i < 2500; i++)
                {
                    if (vote1 == vote4 || vote2 == vote3)
                    {
                        o1 = new object();
                    }
                }
            }

            using (var rp = new RegionProfiler("Comparing parsed lines"))
            {
                if (voteLineRegex.Match(VoteString.RemoveBBCode(input1)).Success)
                    line1 = VoteString.CleanVoteLineBBCode(input1);
                else
                    line1 = null;

                if (voteLineRegex.Match(VoteString.RemoveBBCode(input2)).Success)
                    line2 = VoteString.CleanVoteLineBBCode(input2);
                else
                    line2 = null;

                if (voteLineRegex.Match(VoteString.RemoveBBCode(input3)).Success)
                    line3 = VoteString.CleanVoteLineBBCode(input3);
                else
                    line3 = null;

                if (voteLineRegex.Match(VoteString.RemoveBBCode(input4)).Success)
                    line4 = VoteString.CleanVoteLineBBCode(input4);
                else
                    line4 = null;

                for (int i = 0; i < 2500; i++)
                {
                    if (
                        //Agnostic.StringComparer.Equals(VoteString.GetVoteContent(line1), VoteString.GetVoteContent(line2)) ||
                        Agnostic.StringComparer.Equals(VoteString.GetVoteContent(line3!), VoteString.GetVoteContent(line4!))
                        )
                    {
                        o1 = new object();
                    }
                }
            }


            VoteLine vote = new VoteLine("", "", "", "", MarkerType.None, 0);

            Assert.AreEqual(VoteLine.Empty, vote);
        }
    }
}
