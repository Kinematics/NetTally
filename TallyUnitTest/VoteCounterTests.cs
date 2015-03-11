using Microsoft.VisualStudio.TestTools.UnitTesting;
using NetTally;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Runtime.CompilerServices;

namespace NetTally.Tests
{
    [TestClass()]
    public class VoteCounterTests
    {
        static VoteCounter voteCounter;
        static PrivateObject privateVote;
        static IForumData forumData;

        [ClassInitialize()]
        public static void ClassInit(TestContext context)
        {
            forumData = new SVForumData();
            voteCounter = new VoteCounter(forumData);
            privateVote = new PrivateObject(voteCounter);
        }

        private void SetPartitionByLine()
        {
            voteCounter.UseVotePartitions = true;
            voteCounter.PartitionByLine = true;
        }

        private void SetPartitionByBlock()
        {
            voteCounter.UseVotePartitions = true;
            voteCounter.PartitionByLine = false;
        }

        private void SetPartitionByVote()
        {
            voteCounter.UseVotePartitions = false;
            voteCounter.PartitionByLine = true;
        }

        [TestInitialize()]
        public void Initialize()
        {
            privateVote.Invoke("Reset");

            voteCounter.UseVotePartitions = false;
            voteCounter.PartitionByLine = true;
        }

        [TestMethod()]
        public void TallyVotesTest()
        {
            var a = new VoteCounter(forumData);
            Assert.AreEqual(0, a.VoterMessageId.Count);
            Assert.AreEqual(0, a.VotesWithSupporters.Count);
            Assert.AreEqual(false, a.UseVotePartitions);
            Assert.AreEqual(true, a.PartitionByLine);

            privateVote.Invoke("Reset");
        }

        [TestMethod()]
        public void UsePartitionsTest()
        {
            voteCounter.UseVotePartitions = true;
            Assert.AreEqual(true, voteCounter.UseVotePartitions);
            voteCounter.UseVotePartitions = false;
            Assert.AreEqual(false, voteCounter.UseVotePartitions);
        }

        [TestMethod()]
        public void PartitionByLineTest()
        {
            voteCounter.PartitionByLine = true;
            Assert.AreEqual(true, voteCounter.PartitionByLine);
            voteCounter.PartitionByLine = false;
            Assert.AreEqual(false, voteCounter.PartitionByLine);
        }

        [TestMethod()]
        public void CleanVoteWholeTest()
        {
            string input = "[X] We [i]did[/i] agree to non-lethal. My most [color=blue]powerful[/color] stuff either knocks people out or kills them without having to fight at all. Everything else I've learned to do so far feels like a witch barrier, and I try not to use that since it freaks everyone out.";
            string expected = "[x]wedidagreetonon-lethalmymostpowerfulstuffeitherknockspeopleoutorkillsthemwithouthavingtofightatalleverythingelsei'velearnedtodosofarfeelslikeawitchbarrier,anditrynottousethatsinceitfreakseveryoneout";

            SetPartitionByVote();
            var results = privateVote.Invoke("CleanVote", input);
            Assert.AreEqual(expected, results);
        }

        [TestMethod()]
        public void CleanVoteBlockTest()
        {
            string input = "-[X] We [i]did[/i] agree to non-lethal. My most [color=blue]powerful[/color] stuff either knocks people out or kills them without having to fight at all. Everything else I've learned to do so far feels like a witch barrier, and I try not to use that since it freaks everyone out.";
            string expected = "-[x]wedidagreetonon-lethalmymostpowerfulstuffeitherknockspeopleoutorkillsthemwithouthavingtofightatalleverythingelsei'velearnedtodosofarfeelslikeawitchbarrier,anditrynottousethatsinceitfreakseveryoneout";

            SetPartitionByBlock();
            var results = privateVote.Invoke("CleanVote", input);
            Assert.AreEqual(expected, results);
        }

        [TestMethod()]
        public void CleanVoteLineTest()
        {
            string input = "-[X] We [i]did[/i] agree to non-lethal. My most [color=blue]powerful[/color] stuff either knocks people out or kills them without having to fight at all. Everything else I've learned to do so far feels like a witch barrier, and I try not to use that since it freaks everyone out.";
            string expected = "[x]wedidagreetonon-lethalmymostpowerfulstuffeitherknockspeopleoutorkillsthemwithouthavingtofightatalleverythingelsei'velearnedtodosofarfeelslikeawitchbarrier,anditrynottousethatsinceitfreakseveryoneout";

            SetPartitionByLine();
            var results = privateVote.Invoke("CleanVote", input);
            Assert.AreEqual(expected, results);
        }

        [TestMethod()]
        public void GetVoteKeyTest1()
        {
            string myVote = "[x] Vote for stuff";

            string key = (string)privateVote.Invoke("GetVoteKey", myVote);
            Assert.AreEqual(myVote, key);
        }

        [TestMethod()]
        public void GetVoteKeyTest2()
        {
            string myVote = "[x] Vote for stuff";
            voteCounter.VotesWithSupporters[myVote] = new HashSet<string>() { "me" };

            string key = (string)privateVote.Invoke("GetVoteKey", myVote);
            Assert.AreEqual(myVote, key);
        }

        [TestMethod()]
        public void GetVoteKeyTest3()
        {
            string myVote = "[x] Vote for stuff";

            string key = (string)privateVote.Invoke("GetVoteKey", myVote);
            Assert.AreEqual(myVote, key);
            string key2 = (string)privateVote.Invoke("GetVoteKey", myVote);
            Assert.AreEqual(myVote, key2);

            string myBoldVote = "[x] Vote for [b]stuff[/b] ";
            string key3 = (string)privateVote.Invoke("GetVoteKey", myBoldVote);
            Assert.AreEqual(myVote, key3);
        }

        [TestMethod()]
        public void StripFormattingTest()
        {
            string line1 = "[x] Vote for stuff";
            string line2 = "-[x] Vote for stuff";

            string line3 = "[b][x] Vote for stuff[/b]";
            string line4 = "[color=blue][x] Vote for stuff[/color]";
            string line5 = "[b][x] Vote for stuff";
            string line6 = "[color=blue][b][x] Vote for stuff[/b]";
            string line7 = "[b]-[x] Vote for stuff";
            string line8 = "[color=blue]-[x] Vote for stuff[/color]";

            string e = "";

            Assert.AreEqual(line1, (string)privateVote.Invoke("StripFormatting", line1));
            Assert.AreEqual(line1, (string)privateVote.Invoke("StripFormatting", line3));
            Assert.AreEqual(line1, (string)privateVote.Invoke("StripFormatting", line4));
            Assert.AreEqual(line1, (string)privateVote.Invoke("StripFormatting", line5));
            Assert.AreEqual(line1, (string)privateVote.Invoke("StripFormatting", line6));

            Assert.AreEqual(line2, (string)privateVote.Invoke("StripFormatting", line2));
            Assert.AreEqual(line2, (string)privateVote.Invoke("StripFormatting", line7));
            Assert.AreEqual(line2, (string)privateVote.Invoke("StripFormatting", line8));

            Assert.AreEqual(e, (string)privateVote.Invoke("StripFormatting", e));
        }

        [TestMethod()]
        public void CloseFormattingTagsTest()
        {
            List<string> partitions = new List<string>();

            string line1 = "[x] Vote for stuff 1\r\n";
            string line2 = "[x] Vote for stuff 2\r\n";
            string line2a = "[x] Vote for [b]stuff[/b] 2\r\n";
            string line3 = "[x] Vote for stuff 3\r\n";
            string line3a = "[color=blue][x] Vote for stuff 3[/color]\r\n";
            string line4 = "[x] Vote for stuff 4\r\n";
            string line4a = "[color=blue][x] Vote for stuff 4\r\n";
            string line4b = "[color=blue][x] Vote for stuff 4[/color]\r\n";
            string line5 = "[x] Vote for stuff 5\r\n";
            string line5a = "[color=blue][b][x] Vote for stuff 5[/color]\r\n";
            string line5b = "[color=blue][b][x] Vote for stuff 5[/color][/b]\r\n";

            string line6 = "[x] Vote for stuff 6\r\n";
            string line6a = "[x] Vote for stuff 6[/b]\r\n";
            string line7 = "[x] [b]Vote[/b] for stuff 7\r\n";
            string line7a = "[x] [b]Vote[/b] for stuff 7[/b]\r\n";
            string line7b = "[b][x] [b]Vote[/b] for stuff 7[/b]\r\n";
            string line7c = "[x] [b]Vote for stuff 7[/b]\r\n";
            string line8 = "[x] [i]Vote[/i] for stuff 8\r\n";
            string line8a = "[x] [i]Vote[/i] for stuff 8[/color]\r\n";


            partitions.Add(line1);
            partitions.Add(line2a);
            partitions.Add(line3a);
            partitions.Add(line4a);
            partitions.Add(line5a);
            partitions.Add(line6a);
            partitions.Add(line7a);
            partitions.Add(line8a);

            privateVote.Invoke("CloseFormattingTags", partitions);

            Assert.AreEqual(8, partitions.Count);
            Assert.IsTrue(partitions.Contains(line1));
            Assert.IsTrue(partitions.Contains(line2a));
            Assert.IsTrue(partitions.Contains(line3a));
            Assert.IsTrue(partitions.Contains(line4b));
            Assert.IsTrue(partitions.Contains(line5b));
            Assert.IsTrue(partitions.Contains(line6));
            Assert.IsTrue(partitions.Contains(line7c));
            Assert.IsTrue(partitions.Contains(line8));

            Assert.IsFalse(partitions.Contains(line2));
            Assert.IsFalse(partitions.Contains(line3));
            Assert.IsFalse(partitions.Contains(line4));
            Assert.IsFalse(partitions.Contains(line4a));
            Assert.IsFalse(partitions.Contains(line5));
            Assert.IsFalse(partitions.Contains(line5a));
            Assert.IsFalse(partitions.Contains(line6a));
            Assert.IsFalse(partitions.Contains(line7));
            Assert.IsFalse(partitions.Contains(line7a));
            Assert.IsFalse(partitions.Contains(line7b));
            Assert.IsFalse(partitions.Contains(line8a));
        }


        [TestMethod()]
        public void FindVotesForVoterTest1()
        {
            string vote1 = "[x] Vote for stuff 1";
            string vote2 = "[x] Vote for stuff 2";
            voteCounter.VotesWithSupporters[vote1] = new HashSet<string>() { "me" };
            voteCounter.VotesWithSupporters[vote2] = new HashSet<string>() { "you" };

            List<string> votes = (List<string>)privateVote.Invoke("FindVotesForVoter", "me");
            Assert.AreEqual(1, votes.Count);
            Assert.IsTrue(votes.Contains(vote1));
        }

        [TestMethod()]
        public void FindVotesForVoterTest2()
        {
            string vote1 = "[x] Vote for stuff 1";
            string vote2 = "[x] Vote for stuff 2";
            voteCounter.VotesWithSupporters[vote1] = new HashSet<string>() { "me" };
            voteCounter.VotesWithSupporters[vote2] = new HashSet<string>() { "me", "you" };

            List<string> votes = (List<string>)privateVote.Invoke("FindVotesForVoter", "me");
            Assert.AreEqual(2, votes.Count);
            Assert.IsTrue(votes.Contains(vote1));
            Assert.IsTrue(votes.Contains(vote2));
        }

        [TestMethod()]
        public void RemoveSupportTest1()
        {
            string vote1 = "[x] Vote for stuff 1";
            string vote2 = "[x] Vote for stuff 2";
            voteCounter.VotesWithSupporters[vote1] = new HashSet<string>() { "me", "you" };
            voteCounter.VotesWithSupporters[vote2] = new HashSet<string>() { "him" };

            List<string> votes = (List<string>)privateVote.Invoke("RemoveSupport", "me");
            Assert.AreEqual(2, voteCounter.VotesWithSupporters.Count);
            Assert.IsTrue(voteCounter.VotesWithSupporters.Keys.Contains(vote1));
            Assert.IsTrue(voteCounter.VotesWithSupporters.Keys.Contains(vote2));
            Assert.IsTrue(voteCounter.VotesWithSupporters[vote1].Count == 1);
            Assert.IsTrue(voteCounter.VotesWithSupporters[vote2].Count == 1);
            Assert.IsTrue(voteCounter.VotesWithSupporters[vote1].Contains("you"));
            Assert.IsTrue(voteCounter.VotesWithSupporters[vote2].Contains("him"));
        }

        [TestMethod()]
        public void RemoveSupportTest2()
        {
            string vote1 = "[x] Vote for stuff 1";
            string vote2 = "[x] Vote for stuff 2";
            voteCounter.VotesWithSupporters[vote1] = new HashSet<string>() { "me" };
            voteCounter.VotesWithSupporters[vote2] = new HashSet<string>() { "you" };

            List<string> votes = (List<string>)privateVote.Invoke("RemoveSupport", "me");
            Assert.AreEqual(1, voteCounter.VotesWithSupporters.Count);
            Assert.IsTrue(voteCounter.VotesWithSupporters.Keys.Contains(vote2));
        }

        [TestMethod()]
        public void RemoveSupportTest3()
        {
            string vote1 = "[x] Vote for stuff 1";
            string vote2 = "[x] Vote for stuff 2";
            voteCounter.VotesWithSupporters[vote1] = new HashSet<string>() { "me" };
            voteCounter.VotesWithSupporters[vote2] = new HashSet<string>() { "me", "you" };

            List<string> votes = (List<string>)privateVote.Invoke("RemoveSupport", "me");
            Assert.AreEqual(1, voteCounter.VotesWithSupporters.Count);
            Assert.IsTrue(voteCounter.VotesWithSupporters.Keys.Contains(vote2));
            Assert.IsTrue(voteCounter.VotesWithSupporters[vote2].Count == 1);
            Assert.IsTrue(voteCounter.VotesWithSupporters[vote2].Contains("you"));
        }


        [TestMethod()]
        public void ProcessPostContentsWholeTest()
        {
            string testVote = @"[x] Text Nagisa's uncle about her visiting today. Establish a specific time. (Keep in mind Sayaka's hospital visit.)
[x] Telepathy Oriko and Kirika. They probably need to pick up some groceries at this point. It should be fine if you go with them. And of course, you can cleanse their gems too.
[x] Head over to Oriko's.
-[x] 20 minutes roof hopping practice. Then fly the rest of the way.
-[x] Cleansing.
-[x] Take both of them food shopping (or whoever wants to go.)
-[x] Light conversation. No need for serious precog questions right now.";
            string author = "Muramasa";
            string postId = "123456";

            SetPartitionByVote();
            privateVote.Invoke("ProcessPostContents", testVote, author, postId);

            Assert.IsTrue(voteCounter.VotesWithSupporters.Count == 1);
            Assert.IsTrue(voteCounter.VoterMessageId.Count == 1);
        }

        [TestMethod()]
        public void ProcessPostContentsBlockTest()
        {
            string testVote = @"[x] Text Nagisa's uncle about her visiting today. Establish a specific time. (Keep in mind Sayaka's hospital visit.)
[x] Telepathy Oriko and Kirika. They probably need to pick up some groceries at this point. It should be fine if you go with them. And of course, you can cleanse their gems too.
[x] Head over to Oriko's.
-[x] 20 minutes roof hopping practice. Then fly the rest of the way.
-[x] Cleansing.
-[x] Take both of them food shopping (or whoever wants to go.)
-[x] Light conversation. No need for serious precog questions right now.";

            string author = "Muramasa";
            string postId = "123456";

            SetPartitionByBlock();
            privateVote.Invoke("ProcessPostContents", testVote, author, postId);

            Assert.IsTrue(voteCounter.VotesWithSupporters.Count == 3);
            Assert.IsTrue(voteCounter.VoterMessageId.Count == 1);
        }

        [TestMethod()]
        public void ProcessPostContentsLineTest()
        {
            string testVote = @"[x] Text Nagisa's uncle about her visiting today. Establish a specific time. (Keep in mind Sayaka's hospital visit.)
[x] Telepathy Oriko and Kirika. They probably need to pick up some groceries at this point. It should be fine if you go with them. And of course, you can cleanse their gems too.
[x] Head over to Oriko's.
-[x] 20 minutes roof hopping practice. Then fly the rest of the way.
-[x] Cleansing.
-[x] Take both of them food shopping (or whoever wants to go.)
-[x] Light conversation. No need for serious precog questions right now.";

            string author = "Muramasa";
            string postId = "123456";

            SetPartitionByLine();
            privateVote.Invoke("ProcessPostContents", testVote, author, postId);

            Assert.IsTrue(voteCounter.VotesWithSupporters.Count == 7);
            Assert.IsTrue(voteCounter.VoterMessageId.Count == 1);
        }

        [TestMethod()]
        public void ProcessPostContentsTallyTest()
        {
            string testVote = @"[b]Vote Tally[/b]
[color=transparent]##### NetTally 1.0[/color]
[x] Text Nagisa's uncle about her visiting today. Establish a specific time. (Keep in mind Sayaka's hospital visit.)
[x] Telepathy Oriko and Kirika. They probably need to pick up some groceries at this point. It should be fine if you go with them. And of course, you can cleanse their gems too.
[x] Head over to Oriko's.
-[x] 20 minutes roof hopping practice. Then fly the rest of the way.
-[x] Cleansing.
-[x] Take both of them food shopping (or whoever wants to go.)
-[x] Light conversation. No need for serious precog questions right now.";

            string author = "Muramasa";
            string postId = "123456";

            privateVote.Invoke("ProcessPostContents", testVote, author, postId);

            Assert.IsTrue(voteCounter.VotesWithSupporters.Count == 0);
            Assert.IsTrue(voteCounter.VoterMessageId.Count == 0);
        }


        [TestMethod()]
        public void ProcessPostContentsWholeWithReferralTest1()
        {
            SetPartitionByVote();

            string testVote = @"[x] Text Nagisa's uncle about her visiting today. Establish a specific time. (Keep in mind Sayaka's hospital visit.)
[x] Telepathy Oriko and Kirika. They probably need to pick up some groceries at this point. It should be fine if you go with them. And of course, you can cleanse their gems too.
[x] Head over to Oriko's.
-[x] 20 minutes roof hopping practice. Then fly the rest of the way.
-[x] Cleansing.
-[x] Take both of them food shopping (or whoever wants to go.)
-[x] Light conversation. No need for serious precog questions right now.";
            string author = "Muramasa";
            string postId = "123456";
            privateVote.Invoke("ProcessPostContents", testVote, author, postId);

            string referralVote = @"[x] Muramasa";
            string refAuthor = "Gerbil";
            string refID = "123457";
            privateVote.Invoke("ProcessPostContents", referralVote, refAuthor, refID);

            Assert.IsTrue(voteCounter.VotesWithSupporters.Count == 1);
            Assert.IsTrue(voteCounter.VotesWithSupporters.All(v => v.Value.Count == 2));
            Assert.IsTrue(voteCounter.VoterMessageId.Count == 2);
        }

        [TestMethod()]
        public void ProcessPostContentsBlockWithReferralTest1()
        {
            SetPartitionByBlock();

            string testVote = @"[x] Text Nagisa's uncle about her visiting today. Establish a specific time. (Keep in mind Sayaka's hospital visit.)
[x] Telepathy Oriko and Kirika. They probably need to pick up some groceries at this point. It should be fine if you go with them. And of course, you can cleanse their gems too.
[x] Head over to Oriko's.
-[x] 20 minutes roof hopping practice. Then fly the rest of the way.
-[x] Cleansing.
-[x] Take both of them food shopping (or whoever wants to go.)
-[x] Light conversation. No need for serious precog questions right now.";

            string author = "Muramasa";
            string postId = "123456";
            privateVote.Invoke("ProcessPostContents", testVote, author, postId);

            string referralVote = @"[x] Muramasa";
            string refAuthor = "Gerbil";
            string refID = "123457";
            privateVote.Invoke("ProcessPostContents", referralVote, refAuthor, refID);

            Assert.IsTrue(voteCounter.VotesWithSupporters.Count == 3);
            Assert.IsTrue(voteCounter.VotesWithSupporters.All(v => v.Value.Count == 2));
            Assert.IsTrue(voteCounter.VoterMessageId.Count == 2);
        }

        [TestMethod()]
        public void ProcessPostContentsLineWithReferralTest1()
        {
            SetPartitionByLine();

            string testVote = @"[x] Text Nagisa's uncle about her visiting today. Establish a specific time. (Keep in mind Sayaka's hospital visit.)
[x] Telepathy Oriko and Kirika. They probably need to pick up some groceries at this point. It should be fine if you go with them. And of course, you can cleanse their gems too.
[x] Head over to Oriko's.
-[x] 20 minutes roof hopping practice. Then fly the rest of the way.
-[x] Cleansing.
-[x] Take both of them food shopping (or whoever wants to go.)
-[x] Light conversation. No need for serious precog questions right now.";

            string author = "Muramasa";
            string postId = "123456";
            privateVote.Invoke("ProcessPostContents", testVote, author, postId);

            string referralVote = @"[x] Muramasa";
            string refAuthor = "Gerbil";
            string refID = "123457";
            privateVote.Invoke("ProcessPostContents", referralVote, refAuthor, refID);

            Assert.IsTrue(voteCounter.VotesWithSupporters.Count == 7);
            Assert.IsTrue(voteCounter.VotesWithSupporters.All(v => v.Value.Count == 2));
            Assert.IsTrue(voteCounter.VoterMessageId.Count == 2);
        }


        [TestMethod()]
        public void ProcessPostContentsWholeWithReferralTest2()
        {
            SetPartitionByVote();

            string testVote = @"[x] Text Nagisa's uncle about her visiting today. Establish a specific time. (Keep in mind Sayaka's hospital visit.)
[x] Telepathy Oriko and Kirika. They probably need to pick up some groceries at this point. It should be fine if you go with them. And of course, you can cleanse their gems too.
[x] Head over to Oriko's.
-[x] 20 minutes roof hopping practice. Then fly the rest of the way.
-[x] Cleansing.
-[x] Take both of them food shopping (or whoever wants to go.)
-[x] Light conversation. No need for serious precog questions right now.";
            string author = "Muramasa";
            string postId = "123456";
            privateVote.Invoke("ProcessPostContents", testVote, author, postId);

            string referralVote = @"[x] Muramasa
[x] With Cake";
            string refAuthor = "Gerbil";
            string refID = "123457";
            privateVote.Invoke("ProcessPostContents", referralVote, refAuthor, refID);

            Assert.IsTrue(voteCounter.VotesWithSupporters.Count == 2);
            Assert.IsTrue(voteCounter.VotesWithSupporters.All(v => v.Value.Count == 1));
            Assert.IsTrue(voteCounter.VoterMessageId.Count == 2);
        }

        [TestMethod()]
        public void ProcessPostContentsBlockWithReferralTest2()
        {
            SetPartitionByBlock();

            string testVote = @"[x] Text Nagisa's uncle about her visiting today. Establish a specific time. (Keep in mind Sayaka's hospital visit.)
[x] Telepathy Oriko and Kirika. They probably need to pick up some groceries at this point. It should be fine if you go with them. And of course, you can cleanse their gems too.
[x] Head over to Oriko's.
-[x] 20 minutes roof hopping practice. Then fly the rest of the way.
-[x] Cleansing.
-[x] Take both of them food shopping (or whoever wants to go.)
-[x] Light conversation. No need for serious precog questions right now.";

            string author = "Muramasa";
            string postId = "123456";
            privateVote.Invoke("ProcessPostContents", testVote, author, postId);

            string referralVote = @"[x] Muramasa
[x] With Cake";
            string refAuthor = "Gerbil";
            string refID = "123457";
            privateVote.Invoke("ProcessPostContents", referralVote, refAuthor, refID);

            Assert.IsTrue(voteCounter.VotesWithSupporters.Count == 4);
            Assert.IsTrue(voteCounter.VotesWithSupporters.Count(v => v.Value.Count == 2) == 3);
            Assert.IsTrue(voteCounter.VotesWithSupporters.Count(v => v.Value.Count == 1) == 1);
            Assert.IsTrue(voteCounter.VoterMessageId.Count == 2);
        }

        [TestMethod()]
        public void ProcessPostContentsLineWithReferralTest2()
        {
            SetPartitionByLine();

            string testVote = @"[x] Text Nagisa's uncle about her visiting today. Establish a specific time. (Keep in mind Sayaka's hospital visit.)
[x] Telepathy Oriko and Kirika. They probably need to pick up some groceries at this point. It should be fine if you go with them. And of course, you can cleanse their gems too.
[x] Head over to Oriko's.
-[x] 20 minutes roof hopping practice. Then fly the rest of the way.
-[x] Cleansing.
-[x] Take both of them food shopping (or whoever wants to go.)
-[x] Light conversation. No need for serious precog questions right now.";

            string author = "Muramasa";
            string postId = "123456";
            privateVote.Invoke("ProcessPostContents", testVote, author, postId);

            string referralVote = @"[x] Muramasa
[x] With Cake";
            string refAuthor = "Gerbil";
            string refID = "123457";
            privateVote.Invoke("ProcessPostContents", referralVote, refAuthor, refID);

            Assert.IsTrue(voteCounter.VotesWithSupporters.Count == 8);
            Assert.IsTrue(voteCounter.VotesWithSupporters.Count(v => v.Value.Count == 2) == 7);
            Assert.IsTrue(voteCounter.VotesWithSupporters.Count(v => v.Value.Count == 1) == 1);
            Assert.IsTrue(voteCounter.VoterMessageId.Count == 2);
        }



    }
}