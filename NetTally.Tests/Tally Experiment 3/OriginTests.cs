using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NetTally.Forums;

namespace NetTally.Tests.Experiment3
{
    [TestClass]
    public class OriginTests
    {
        #region Setup
        static IServiceProvider serviceProvider;

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
        public void Construct_Fail_BadID()
        {
            Origin origin = new Origin("Kinematics", "-101", 10, new Uri("http://www.example.com/"), "http://www.example.com/");
        }

        [TestMethod]
        public void Construct_BadID_Unknown()
        {
            Origin origin = new Origin("Kinematics", "101xq", 10, new Uri("http://www.example.com/"), "http://www.example.com/");
        }

        [TestMethod]
        public void Construct_BadID_Overflow()
        {
            Origin origin = new Origin("Kinematics", "4294967296", 10, new Uri("http://www.example.com/"), "http://www.example.com/");
        }
    }
}
