using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NetTally.Tests
{
    [TestClass]
    public class VoteTests
    {
        #region Setup
        static IVoteCounter voteCounter;
        static IQuest sampleQuest;

        [ClassInitialize]
        public static void ClassInit(TestContext context)
        {
            voteCounter = new VoteCounter();
            sampleQuest = new Quest();
        }

        [TestInitialize]
        public void Initialize()
        {
            voteCounter.Reset();
            voteCounter.PostsList.Clear();
            sampleQuest = new Quest();
        }
        #endregion

        #region Test Harness
        public void TestSinglePostParsing(string vote, List<string> results)
        {
            string author = "User1";
            string postId = "123456";
            int postNumber = 100;

            PostComponents post = new PostComponents(author, postId, vote, postNumber);
            voteCounter.PostsList.Add(post);

            voteCounter.TallyPosts(sampleQuest);

            var votes = GetVotesBy(author, VoteType.Vote);

            CollectionAssert.AreEqual(results, votes);
        }

        public List<string> GetVotesBy(string author, VoteType voteType)
        {
            var votes = voteCounter.GetVotesCollection(voteType);
            return votes.Where(v => v.Value.Contains(author)).Select(v => v.Key).ToList();
        }

        public void TestReferencePostParsing(List<string> votes, List<List<string>> results)
        {
            int count = 1;

            foreach (var vote in votes)
            {
                string author = $"User{count}";
                string postID = $"{12344 + count}";
                int postNumber = 99 + count;

                PostComponents post = new PostComponents(author, postID, vote, postNumber);
                voteCounter.PostsList.Add(post);

                count++;
            }

            voteCounter.TallyPosts(sampleQuest);

            count = 0;
            foreach (var post in voteCounter.PostsList)
            {
                var userVotes = GetVotesBy(post.Author, VoteType.Vote);
                CollectionAssert.AreEqual(results[count++], userVotes);
            }
        }

        #endregion

        #region Single Vote Tests
        [TestMethod]
        public void Vote_Parition_None_1()
        {
            string testVote =
@"[X]Run";

            List<string> expected = new List<string>()
            {
@"[X]Run
"
        };

            TestSinglePostParsing(testVote, expected);
        }

        [TestMethod]
        public void Vote_Parition_None_2()
        {

            string testVote =
@"[X] Run
[x] Kill";

            List<string> expected = new List<string>()
            {
@"[X] Run
[x] Kill
"
        };

            TestSinglePostParsing(testVote, expected);
        }

        [TestMethod]
        public void Vote_Parition_None_3()
        {
            string testVote =
@"[X] Ambush
-[X] Kill";

            List<string> expected = new List<string>()
            {
@"[X] Ambush
-[X] Kill
"
        };

            TestSinglePostParsing(testVote, expected);
        }

        [TestMethod]
        public void Vote_Parition_None_WithTask_1()
        {
            string testVote =
@"[X][Plan]Run";

            List<string> expected = new List<string>()
            {
@"[X][Plan]Run
"
        };

            TestSinglePostParsing(testVote, expected);
        }

        [TestMethod]
        public void Vote_Parition_None_WithTask_2()
        {
            string testVote =
@"[X][Plan] Run
[x][Plan] Kill";

            List<string> expected = new List<string>()
            {
@"[X][Plan] Run
[x][Plan] Kill
"
        };

            TestSinglePostParsing(testVote, expected);
        }

        [TestMethod]
        public void Vote_Parition_None_WithTask_3()
        {
            string testVote =
@"[X][Plan] Ambush
-[X] Kill";

            List<string> expected = new List<string>()
            {
@"[X][Plan] Ambush
-[X] Kill
"
        };

            TestSinglePostParsing(testVote, expected);
        }


        [TestMethod]
        public void Vote_Parition_Line_1()
        {
            sampleQuest.PartitionMode = PartitionMode.ByLine;

            string testVote =
@"[X]Run";

            List<string> expected = new List<string>()
            {
@"[X]Run
"
        };

            TestSinglePostParsing(testVote, expected);
        }

        [TestMethod]
        public void Vote_Parition_Line_2()
        {
            sampleQuest.PartitionMode = PartitionMode.ByLine;

            string testVote =
@"[X] Run
[x] Kill";

            List<string> expected = new List<string>()
            {
@"[X] Run
",
@"[x] Kill
"
        };

            TestSinglePostParsing(testVote, expected);
        }

        [TestMethod]
        public void Vote_Parition_Line_3()
        {
            sampleQuest.PartitionMode = PartitionMode.ByLine;

            string testVote =
@"[X] Ambush
-[X] Kill";

            List<string> expected = new List<string>()
            {
@"[X] Ambush
",
@"-[X] Kill
"
        };

            TestSinglePostParsing(testVote, expected);
        }

        [TestMethod]
        public void Vote_Parition_Line_WithTask_1()
        {
            sampleQuest.PartitionMode = PartitionMode.ByLine;

            string testVote =
@"[X][Plan]Run";

            List<string> expected = new List<string>()
            {
@"[X][Plan]Run
"
        };

            TestSinglePostParsing(testVote, expected);
        }

        [TestMethod]
        public void Vote_Parition_Line_WithTask_2()
        {
            sampleQuest.PartitionMode = PartitionMode.ByLine;

            string testVote =
@"[X][Plan] Run
[x][Plan] Kill";

            List<string> expected = new List<string>()
            {
@"[X][Plan] Run
",
@"[x][Plan] Kill
"
        };

            TestSinglePostParsing(testVote, expected);
        }

        [TestMethod]
        public void Vote_Parition_Line_WithTask_3()
        {
            sampleQuest.PartitionMode = PartitionMode.ByLine;

            string testVote =
@"[X][Plan] Ambush
-[X] Kill";

            List<string> expected = new List<string>()
            {
@"[X][Plan] Ambush
",
@"-[X] Kill
"
        };

            TestSinglePostParsing(testVote, expected);
        }


        [TestMethod]
        public void Vote_Parition_Line_WithTask_4()
        {
            sampleQuest.PartitionMode = PartitionMode.ByLine;

            string testVote =
@"[X][Action] Ambush
[X][Decision] Kill
[X] Run
[X] Report";

            List<string> expected = new List<string>()
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

            TestSinglePostParsing(testVote, expected);
        }


        [TestMethod]
        public void Vote_Parition_Block_1()
        {
            sampleQuest.PartitionMode = PartitionMode.ByBlock;

            string testVote =
@"[X] Ambush";

            List<string> expected = new List<string>()
            {
@"[X] Ambush
"
            };

            TestSinglePostParsing(testVote, expected);
        }

        [TestMethod]
        public void Vote_Parition_Block_2()
        {
            sampleQuest.PartitionMode = PartitionMode.ByBlock;

            string testVote =
@"[X] Ambush
[X] Run";

            List<string> expected = new List<string>()
            {
@"[X] Ambush
",
@"[X] Run
"
            };

            TestSinglePostParsing(testVote, expected);
        }


        [TestMethod]
        public void Vote_Parition_Block_3()
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

            TestSinglePostParsing(testVote, expected);
        }

        [TestMethod]
        public void Vote_Parition_Block_WithTask_1()
        {
            sampleQuest.PartitionMode = PartitionMode.ByBlock;

            string testVote =
@"[X][Action] Ambush";

            List<string> expected = new List<string>()
            {
@"[X][Action] Ambush
"
            };

            TestSinglePostParsing(testVote, expected);
        }

        [TestMethod]
        public void Vote_Parition_Block_WithTask_2()
        {
            sampleQuest.PartitionMode = PartitionMode.ByBlock;

            string testVote =
@"[X][Action] Ambush
[X] Run";

            List<string> expected = new List<string>()
            {
@"[X][Action] Ambush
",
@"[X] Run
"
            };

            TestSinglePostParsing(testVote, expected);
        }


        [TestMethod]
        public void Vote_Parition_Block_WithTask_3()
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

            TestSinglePostParsing(testVote, expected);
        }




        [TestMethod]
        public void Vote_Parition_BlockAll_WithTasks_1()
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

            TestSinglePostParsing(testVote, expected);
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

            List<string> expected = new List<string>()
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


            List<string> expected = new List<string>()
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
@"[X] Dodgy",
            };


            List<string> expected = new List<string>()
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
@"[X] User 1",
            };


            List<string> expected = new List<string>()
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
@"[X] User 1",
            };


            List<string> expected = new List<string>()
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
    }
}
