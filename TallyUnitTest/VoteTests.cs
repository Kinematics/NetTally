using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using NetTally.Tests.Platform;
using NetTally.Utility;
using NetTally.ViewModels;
using NetTally.VoteCounting;
using NetTally.Votes;

namespace NetTally.Tests
{
    [TestClass]
    public class VoteTests
    {
        #region Setup
        static IQuest sampleQuest;

        [ClassInitialize]
        public static void ClassInit(TestContext context)
        {
            Agnostic.HashStringsUsing(UnicodeHashFunction.HashFunction);

            sampleQuest = new Quest();

            ViewModelService.Instance.Build();
        }

        [TestInitialize]
        public void Initialize()
        {
            VoteCounter.Instance.Reset();
            VoteCounter.Instance.PostsList.Clear();
            sampleQuest = new Quest();
        }
        #endregion

        #region Test Harness
        public async Task TestSinglePostParsing(string vote, List<string> results)
        {
            string author = "User1";
            string postId = "123456";
            int postNumber = 100;

            PostComponents post = new PostComponents(author, postId, vote, postNumber);
            List<PostComponents> posts = new List<PostComponents>();
            posts.Add(post);

            await VoteCounter.Instance.TallyPosts(posts, sampleQuest, CancellationToken.None);

            var votes = GetVotesBy(author, VoteType.Vote);

            CollectionAssert.AreEqual(results, votes);
        }

        public static List<string> GetVotesBy(string author, VoteType voteType)
        {
            var votes = VoteCounter.Instance.GetVotesCollection(voteType);
            return votes.Where(v => v.Value.Contains(author)).Select(v => v.Key).ToList();
        }

        public async Task TestReferencePostParsing(List<string> votes, List<string> authors, List<List<string>> results)
        {
            List<PostComponents> posts = new List<PostComponents>();

            for (int i = 0; i < votes.Count; i++)
            {
                string postID = $"{12345 + i}";
                int postNumber = 100 + i;

                PostComponents post = new PostComponents(authors[i], postID, votes[i], postNumber);
                posts.Add(post);
            }

            await VoteCounter.Instance.TallyPosts(posts, sampleQuest, CancellationToken.None);

            for (int i = 0; i < results.Count; i++)
            {
                var post = VoteCounter.Instance.PostsList[i];
                var userVotes = GetVotesBy(post.Author, VoteType.Vote);
                CollectionAssert.AreEqual(results[i], userVotes);
            }
        }

        public static void TestReferencePostParsing(List<string> votes, List<List<string>> results)
        {
            if (votes == null)
                throw new ArgumentNullException(nameof(votes));

            List<string> authors = new List<string>();
            for (int i = 1; i <= votes.Count; i++)
            {
                authors.Add($"User{i}");
            }
        }
        #endregion

        #region Single Vote Tests
        [TestMethod]
        public async Task Vote_Parition_None_1()
        {
            string testVote =
@"[X]Run";

            List<string> expected = new List<string>
            {
@"[X]Run
"
        };

            await TestSinglePostParsing(testVote, expected);
        }

        [TestMethod]
        public async Task Vote_Parition_None_2()
        {

            string testVote =
@"[X] Run
[x] Kill";

            List<string> expected = new List<string>
            {
@"[X] Run
[x] Kill
"
        };

            await TestSinglePostParsing(testVote, expected);
        }

        [TestMethod]
        public async Task Vote_Parition_None_3()
        {
            string testVote =
@"[X] Ambush
-[X] Kill";

            List<string> expected = new List<string>
            {
@"[X] Ambush
-[X] Kill
"
        };

            await TestSinglePostParsing(testVote, expected);
        }

        [TestMethod]
        public async Task Vote_Parition_None_WithTask_1()
        {
            string testVote =
@"[X][Plan]Run";

            List<string> expected = new List<string>
            {
@"[X][Plan]Run
"
        };

            await TestSinglePostParsing(testVote, expected);
        }

        [TestMethod]
        public async Task Vote_Parition_None_WithTask_2()
        {
            string testVote =
@"[X][Plan] Run
[x][Plan] Kill";

            List<string> expected = new List<string>
            {
@"[X][Plan] Run
[x][Plan] Kill
"
        };

            await TestSinglePostParsing(testVote, expected);
        }

        [TestMethod]
        public async Task Vote_Parition_None_WithTask_3()
        {
            string testVote =
@"[X][Plan] Ambush
-[X] Kill";

            List<string> expected = new List<string>
            {
@"[X][Plan] Ambush
-[X] Kill
"
        };

            await TestSinglePostParsing(testVote, expected);
        }


        [TestMethod]
        public async Task Vote_Parition_Line_1()
        {
            sampleQuest.PartitionMode = PartitionMode.ByLine;

            string testVote =
@"[X]Run";

            List<string> expected = new List<string>
            {
@"[X]Run
"
        };

            await TestSinglePostParsing(testVote, expected);
        }

        [TestMethod]
        public async Task Vote_Parition_Line_2()
        {
            sampleQuest.PartitionMode = PartitionMode.ByLine;

            string testVote =
@"[X] Run
[x] Kill";

            List<string> expected = new List<string>
            {
@"[X] Run
",
@"[x] Kill
"
        };

            await TestSinglePostParsing(testVote, expected);
        }

        [TestMethod]
        public async Task Vote_Parition_Line_3()
        {
            sampleQuest.PartitionMode = PartitionMode.ByLine;

            string testVote =
@"[X] Ambush
-[X] Kill";

            List<string> expected = new List<string>
            {
@"[X] Ambush
",
@"-[X] Kill
"
        };

            await TestSinglePostParsing(testVote, expected);
        }

        [TestMethod]
        public async Task Vote_Parition_Line_WithTask_1()
        {
            sampleQuest.PartitionMode = PartitionMode.ByLine;

            string testVote =
@"[X][Plan]Run";

            List<string> expected = new List<string>
            {
@"[X][Plan]Run
"
        };

            await TestSinglePostParsing(testVote, expected);
        }

        [TestMethod]
        public async Task Vote_Parition_Line_WithTask_2()
        {
            sampleQuest.PartitionMode = PartitionMode.ByLine;

            string testVote =
@"[X][Plan] Run
[x][Plan] Kill";

            List<string> expected = new List<string>
            {
@"[X][Plan] Run
",
@"[x][Plan] Kill
"
        };

            await TestSinglePostParsing(testVote, expected);
        }

        [TestMethod]
        public async Task Vote_Parition_Line_WithTask_3()
        {
            sampleQuest.PartitionMode = PartitionMode.ByLine;

            string testVote =
@"[X][Plan] Ambush
-[X] Kill";

            List<string> expected = new List<string>
            {
@"[X][Plan] Ambush
",
@"-[X] Kill
"
        };

            await TestSinglePostParsing(testVote, expected);
        }


        [TestMethod]
        public async Task Vote_Parition_Line_WithTask_4()
        {
            sampleQuest.PartitionMode = PartitionMode.ByLine;

            string testVote =
@"[X][Action] Ambush
[X][Decision] Kill
[X] Run
[X] Report";

            List<string> expected = new List<string>
            {
@"[X][Action] Ambush
",
@"[X][Decision] Kill
",
@"[X] Run
",
@"[X] Report
"
            };

            await TestSinglePostParsing(testVote, expected);
        }


        [TestMethod]
        public async Task Vote_Parition_Block_1()
        {
            sampleQuest.PartitionMode = PartitionMode.ByBlock;

            string testVote =
@"[X] Ambush";

            List<string> expected = new List<string>
            {
@"[X] Ambush
"
            };

            await TestSinglePostParsing(testVote, expected);
        }

        [TestMethod]
        public async Task Vote_Parition_Block_2()
        {
            sampleQuest.PartitionMode = PartitionMode.ByBlock;

            string testVote =
@"[X] Ambush
[X] Run";

            List<string> expected = new List<string>
            {
@"[X] Ambush
",
@"[X] Run
"
            };

            await TestSinglePostParsing(testVote, expected);
        }


        [TestMethod]
        public async Task Vote_Parition_Block_3()
        {
            sampleQuest.PartitionMode = PartitionMode.ByBlock;

            string testVote =
@"[X] Ambush
-[X] Kill
[X] Run";

            List<string> expected = new List<string>(3)
            {
@"[X] Ambush
-[X] Kill
",
@"[X] Run
"
            };

            await TestSinglePostParsing(testVote, expected);
        }

        [TestMethod]
        public async Task Vote_Parition_Block_WithTask_1()
        {
            sampleQuest.PartitionMode = PartitionMode.ByBlock;

            string testVote =
@"[X][Action] Ambush";

            List<string> expected = new List<string>
            {
@"[X][Action] Ambush
"
            };

            await TestSinglePostParsing(testVote, expected);
        }

        [TestMethod]
        public async Task Vote_Parition_Block_WithTask_2()
        {
            sampleQuest.PartitionMode = PartitionMode.ByBlock;

            string testVote =
@"[X][Action] Ambush
[X] Run";

            List<string> expected = new List<string>
            {
@"[X][Action] Ambush
",
@"[X] Run
"
            };

            await TestSinglePostParsing(testVote, expected);
        }


        [TestMethod]
        public async Task Vote_Parition_Block_WithTask_3()
        {
            sampleQuest.PartitionMode = PartitionMode.ByBlock;

            string testVote =
@"[X][Action] Ambush
-[X] Kill
[X] Run";

            List<string> expected = new List<string>(3)
            {
@"[X][Action] Ambush
-[X] Kill
",
@"[X] Run
"
            };

            await TestSinglePostParsing(testVote, expected);
        }




        [TestMethod]
        public async Task Vote_Parition_BlockAll_WithTasks_1()
        {
            string testVote =
@"[X][Action] Plan One
-[X] Ambush
-[X][Decision] Kill
-[X] Run
[X] Plan Two
-[X] Report";

            List<string> expected = new List<string>(3)
            {
@"[X][Action] Ambush
",
@"[X][Decision] Kill
",
@"[X][Action] Run
",
@"[X] Report
"
            };

            sampleQuest.PartitionMode = PartitionMode.ByBlockAll;

            await TestSinglePostParsing(testVote, expected);
        }


        #endregion

        #region Multi Vote Tests
        #endregion

        #region Reference Vote Tests
        [TestMethod]
        public void Vote_Reference_Author_NoPartition_1()
        {
            List<string> votes = new List<string>
            {
@"[X]Run",
@"[X] User1"
            };

            List<string> expected = new List<string>
            {
@"[X]Run
"
            };

            List<List<string>> listOfExpected = new List<List<string>> { expected, expected };

            TestReferencePostParsing(votes, listOfExpected);
        }

        [TestMethod]
        public void Vote_Reference_Author_NoPartition_2()
        {
            List<string> votes = new List<string>
            {
@"[X] Stuff
[X] More Stuff
-[X] Yet more stuff",
@"[X] User1"
            };


            List<string> expected = new List<string>
            {
@"[X] Stuff
[X] More Stuff
-[X] Yet more stuff
"
            };

            List<List<string>> listOfExpected = new List<List<string>> { expected, expected };

            TestReferencePostParsing(votes, listOfExpected);
        }

        [TestMethod]
        public void Vote_Reference_Author_NoPartition_3()
        {
            List<string> votes = new List<string>
            {
@"[X] Plan Dodgy
-[X] More Stuff
-[X] Yet more stuff",
@"[X] User1",
@"[X] Dodgy"
            };


            List<string> expected = new List<string>
            {
@"[X] Plan Dodgy
-[X] More Stuff
-[X] Yet more stuff
"
            };

            List<List<string>> listOfExpected = new List<List<string>> { expected, expected, expected };

            TestReferencePostParsing(votes, listOfExpected);
        }

        [TestMethod]
        public void Vote_Reference_Author_NoPartition_4()
        {
            List<string> votes = new List<string>
            {
@"[X] Plan User1
-[X] More Stuff
-[X] Yet more stuff",
@"[X] Plan User1",
@"[X] User 1"
            };


            List<string> expected = new List<string>
            {
@"[X] Plan User1
-[X] More Stuff
-[X] Yet more stuff
"
            };

            List<List<string>> listOfExpected = new List<List<string>> { expected, expected, expected };

            TestReferencePostParsing(votes, listOfExpected);
        }

        [TestMethod]
        public void Vote_Reference_Author_NoPartition_5()
        {
            List<string> votes = new List<string>
            {
@"[X] Plan User1
-[X] More Stuff
-[X] Yet more stuff",
@"[X] Plan User1
-[X] More Stuff
-[X] Yet more stuff",
@"[X] User 1"
            };


            List<string> expected = new List<string>
            {
@"[X] Plan User1
-[X] More Stuff
-[X] Yet more stuff
"
            };

            List<List<string>> listOfExpected = new List<List<string>> { expected, expected, expected };

            TestReferencePostParsing(votes, listOfExpected);
        }
        #endregion


        [TestMethod]
        public async Task Vote_Reference_Author_SelfReference()
        {
            List<string> votes = new List<string>
            {
@"[x] plan actions
-[x] do stuff
[x] Name: Gonzo",
@"[x] plan Kinematic's
-[x] do stuff
[x] Name: Guido"
            };


            List<string> expected = new List<string>
            {
@"[x] plan Kinematic's
-[x] do stuff
[x] Name: Guido
"
            };

            List<List<string>> listOfExpected = new List<List<string>> { expected, expected };

            List<string> authors = new List<string> { "Kinematics", "Kinematics" };

            await TestReferencePostParsing(votes, authors, listOfExpected);
        }


    }
}
