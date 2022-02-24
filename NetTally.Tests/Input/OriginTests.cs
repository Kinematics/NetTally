using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NetTally.Forums;
using NetTally.Votes;
using NetTally.Types.Enums;
using NetTally.Types.Components;

namespace NetTally.Tests.Forums
{
    [TestClass]
    public class OriginTests
    {
        #region Setup
        static IServiceProvider? serviceProvider;

        [ClassInitialize]
        public static void ClassInit(TestContext context)
        {
            serviceProvider = TestStartup.ConfigureServices();
        }

        [TestInitialize]
        public void Initialize()
        {
        }
        #endregion


        [TestMethod]
        public void Construct_BadID()
        {
            Origin origin = new("Kinematics", "-101", 10,
                new Uri("http://www.example.com/"), "http://www.example.com/");

            Assert.AreEqual("Kinematics", origin.Author.Name);
            Assert.AreEqual(IdentityType.User, origin.AuthorType);
            Assert.AreEqual(10, origin.ThreadPostNumber);
            Assert.AreEqual("http://www.example.com/", origin.Permalink);
            Assert.IsTrue(origin.ID == 0);
        }

        [TestMethod]
        public void Construct_BadID_Unknown()
        {
            Origin origin = new("Kinematics", "101xq", 10, 
                new Uri("http://www.example.com/"), "http://www.example.com/");

            Assert.AreEqual("Kinematics", origin.Author.Name);
            Assert.AreEqual(IdentityType.User, origin.AuthorType);
            Assert.AreEqual(10, origin.ThreadPostNumber);
            Assert.AreEqual("http://www.example.com/", origin.Permalink);
            Assert.IsTrue(origin.ID == 0);
        }

        [TestMethod]
        public void Construct_OverflowInt_ID()
        {
            Origin origin = new("Kinematics", "4294967296", 10, 
                new Uri("http://www.example.com/"), "http://www.example.com/");

            Assert.AreEqual("Kinematics", origin.Author.Name);
            Assert.AreEqual(IdentityType.User, origin.AuthorType);
            Assert.AreEqual(10, origin.ThreadPostNumber);
            Assert.AreEqual("http://www.example.com/", origin.Permalink);
            Assert.IsTrue(origin.ID == 4294967296);
        }

        [TestMethod]
        public void Construct_Short_Origin()
        {
            Origin origin = new("Kinematics", IdentityType.User);

            Assert.AreEqual("Kinematics", origin.Author.Name);
            Assert.AreEqual(IdentityType.User, origin.AuthorType);
            Assert.AreEqual(0, origin.ThreadPostNumber);
            Assert.AreEqual("", origin.Permalink);
            Assert.IsTrue(origin.ID == 0);
            Assert.IsTrue(origin.ID == PostId.Zero);
        }


        [TestMethod]
        public void Compare_Full_Origins_ExactEqual()
        {
            Origin origin1 = new("Kinematics", "5708138", 20, 
                new Uri("http://www.example.com/"), "http://www.example.com/");
            Origin origin2 = new("Kinematics", "5708138", 20, 
                new Uri("http://www.example.com/"), "http://www.example.com/");

            Assert.AreEqual(origin1, origin2);
            Assert.IsTrue(origin1 == origin2);
        }

        [TestMethod]
        public void Compare_Full_Origins_Equal()
        {
            Origin origin1 = new("Kinematics", "5708138", 20,
                new Uri("http://www.example.com/"), "http://www.example.com/");
            Origin origin2 = new("Kinematics", "5708139", 21,
                new Uri("http://www.example.com/"), "http://www.example.com/");

            Assert.AreEqual(origin1, origin2);
        }

        [TestMethod]
        public void Compare_Full_Origins_ExactEqual_Caps()
        {
            Origin origin1 = new("Kinematics", "5708138", 20, 
                new Uri("http://www.example.com/"), "http://www.example.com/");
            Origin origin2 = new("KINEMATICS", "5708138", 20, 
                new Uri("http://www.example.com/"), "http://www.example.com/");

            Assert.AreEqual(origin1, origin2);
            Assert.IsTrue(origin1 == origin2);
        }

        [TestMethod]
        public void Compare_Full_Origins_Equal_Caps()
        {
            Origin origin1 = new("Kinematics", "5708138", 20, 
                new Uri("http://www.example.com/"), "http://www.example.com/");
            Origin origin2 = new("KINEMATICS", "5708139", 21, 
                new Uri("http://www.example.com/"), "http://www.example.com/");

            Assert.AreEqual(origin1, origin2);
        }

        [TestMethod]
        public void Compare_Full_Origins_Diff()
        {
            Origin origin1 = new("Kinematics", "5708138", 20, 
                new Uri("http://www.example.com/"), "http://www.example.com/");
            Origin origin2 = new("Louie", "5708139", 21, 
                new Uri("http://www.example.com/"), "http://www.example.com/");

            Assert.AreNotEqual(origin1, origin2);
        }

        [TestMethod]
        public void Compare_User_with_Plan()
        {
            Origin origin1 = new("Kinematics", "5708138", 20, 
                new Uri("http://www.example.com/"), "http://www.example.com/");
            Origin origin2 = origin1.GetPlanOrigin("Nightlife");

            Assert.AreNotEqual(origin1, origin2);
        }

        [TestMethod]
        public void Compare_Equal_Plan()
        {
            Origin origin1 = new("Kinematics", "5708138", 20, 
                new Uri("http://www.example.com/"), "http://www.example.com/");
            Origin origin2 = origin1.GetPlanOrigin("Nightlife");
            Origin origin3 = origin1.GetPlanOrigin("Nightlife");

            Assert.AreEqual(origin2, origin3);
        }

        [TestMethod]
        public void Compare_Diff_Plan()
        {
            Origin origin1 = new("Kinematics", "5708138", 20, 
                new Uri("http://www.example.com/"), "http://www.example.com/");
            Origin origin2 = origin1.GetPlanOrigin("Nightlife");
            Origin origin3 = origin1.GetPlanOrigin("Beach Trip");

            Assert.AreNotEqual(origin2, origin3);
        }

        [TestMethod]
        public void Compare_Short_Origin_Same()
        {
            Origin origin1 = new("Kinematics", "5708138", 20,
                new Uri("http://www.example.com/"), "http://www.example.com/");
            Origin origin2 = new("Kinematics", IdentityType.User);

            Assert.AreEqual(origin1, origin2);
        }

        [TestMethod]
        public void Compare_Short_Origin_Same_Caps()
        {
            Origin origin1 = new("Kinematics", "5708138", 20, 
                new Uri("http://www.example.com/"), "http://www.example.com/");
            Origin origin2 = new("KINEMATICS", IdentityType.User);

            Assert.AreEqual(origin1, origin2);
        }

        [TestMethod]
        public void Compare_Short_Origin_Diff_Name()
        {
            Origin origin1 = new("Kinematics", "5708138", 20, 
                new Uri("http://www.example.com/"), "http://www.example.com/");
            Origin origin2 = new("KinematicsZ", IdentityType.User);

            Assert.AreNotEqual(origin1, origin2);
        }

        [TestMethod]
        public void Compare_Short_Origin_Diff_Type()
        {
            Origin origin1 = new("Kinematics", "5708138", 20, 
                new Uri("http://www.example.com/"), "http://www.example.com/");
            Origin origin2 = new("Kinematics", IdentityType.Plan);

            Assert.AreNotEqual(origin1, origin2);
        }

        [TestMethod]
        public void Compare_Aquired_Plan_Short_Plan()
        {
            Origin origin1 = new("Kinematics", "5708138", 20, 
                new Uri("http://www.example.com/"), "http://www.example.com/");
            Origin origin2 = origin1.GetPlanOrigin("Nightlife");
            Origin origin3 = new("Nightlife", IdentityType.Plan);

            Assert.AreEqual(origin2, origin3);
        }

        [TestMethod]
        public void Compare_Aquired_Plan_Punc()
        {
            Origin origin1 = new("Kinematics", "5708138", 20, 
                new Uri("http://www.example.com/"), "http://www.example.com/");
            Origin origin2 = origin1.GetPlanOrigin("Nightlife~!");
            Origin origin3 = new("Nightlife", IdentityType.Plan);

            Assert.AreEqual(origin2, origin3);
        }

        [TestMethod]
        public void Compare_Diff_Source()
        {
            Origin origin1 = new("Kinematics", "5708138", 20, 
                new Uri("http://www.example1.com/"), "http://www.example1.com/");
            Origin origin2 = new("Kinematics", "5708139", 20, 
                new Uri("http://www.example2.com/"), "http://www.example2.com/");

            Assert.AreNotEqual(origin1, origin2);
        }

        [TestMethod]
        public void Compare_Diff_Source_Short()
        {
            Origin origin1 = new("Kinematics", "5708138", 20, 
                new Uri("http://www.example1.com/"), "http://www.example1.com/");
            Origin origin2 = new("Kinematics", IdentityType.User);

            Assert.AreEqual(origin1, origin2);
        }

    }
}
