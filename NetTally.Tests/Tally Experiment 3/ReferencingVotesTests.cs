using System;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NetTally.Experiment3;
using NetTally.Forums;
using NetTally.VoteCounting;

namespace NetTally.Tests.Experiment3
{
    [TestClass]
    public class ReferencingVotesTests
    {
        #region Setup
        static IServiceProvider serviceProvider;
        static IVoteCounter voteCounter;
        static VoteConstructor voteConstructor;
        static Tally tally;
        static IQuest quest;

        [ClassInitialize]
        public static void ClassInit(TestContext context)
        {
            serviceProvider = TestStartup.ConfigureServices();

            voteCounter = serviceProvider.GetRequiredService<IVoteCounter>();
            tally = serviceProvider.GetRequiredService<Tally>();
            voteConstructor = serviceProvider.GetRequiredService<VoteConstructor>();
        }

        [TestInitialize]
        public void Initialize()
        {
            quest = new Quest();

            voteCounter.Reset();
            voteCounter.ClearPosts();
        }
        #endregion

        #region Define post text
        /*
        readonly static string oneLine = @"[X] Run Lola Run!";
        readonly static string oneLineTask = @"[X][Movie] Run Lola Run!";
        readonly static string twoLine = @"[X] Run Lola Run!
[X] National Geographic";
        readonly static string twoLineTask = @"[X][Movie] Run Lola Run!
[X] National Geographic";
        readonly static string childLine = @"[X][Movie] Run Lola Run!
-[X] National Geographic";
        readonly static string twoChunk = @"[X][Movie] Run Lola Run!
-[X] National Geographic
[X] Gunbuster";

        readonly static string refKinematics = @"[X] Kinematics";
        readonly static string refAtreya = @"[X] Atreya";
        readonly static string refKimberly = @"[X] Kimberly";
        readonly static string refKinematicsPercent = @"[88%] Kinematics";
        readonly static string refAtreyaPercent = @"[77%] Atreya";
        readonly static string refKimberlyPercent = @"[66%] Kimberly";
        readonly static string refKinematicsApprove = @"[+] Kinematics";
        readonly static string refAtreyaApprove = @"[+] Atreya";
        readonly static string refKimberlyApprove = @"[-] Kimberly";
        */
        #endregion

        #region Generate user posts
        Post GetPostFromUser1(string postText)
        {
            Origin origin = new Origin("Kinematics", "123456", 100, new Uri("http://www.example.com/"), "http://www.example.com");

            return new Post(origin, postText);
        }

        Post GetPostFromUser2(string postText)
        {
            Origin origin = new Origin("Atreya", "123457", 101, new Uri("http://www.example.com/"), "http://www.example.com");

            return new Post(origin, postText);
        }

        Post GetPostFromUser3(string postText)
        {
            Origin origin = new Origin("Kimberly", "123458", 102, new Uri("http://www.example.com/"), "http://www.example.com");

            return new Post(origin, postText);
        }

        Post GetPostFromUser4(string postText)
        {
            Origin origin = new Origin("Kinematics", "123459", 103, new Uri("http://www.example.com/"), "http://www.example.com");

            return new Post(origin, postText);
        }
        #endregion


    }
}
