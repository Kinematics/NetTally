using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NetTally.Experiment3;

namespace NetTally.Tests.Experiment3
{
    [TestClass]
    public class PostIdTests
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


#nullable disable
        [TestMethod]
        [ExpectedException(typeof(ArgumentNullException))]
        public void Construct_Null()
        {
            _ = new PostId(null);
        }
#nullable enable

        [TestMethod]
        [ExpectedException(typeof(ArgumentOutOfRangeException))]
        public void Construct_Empty()
        {
            _ = new PostId("");
        }

        [TestMethod]
        public void Construct_One()
        {
            PostId id = new PostId("1");
            Assert.AreEqual(1, id.Value);
            Assert.IsTrue(id.Equals(1));
        }

        [TestMethod]
        public void Construct_Basic()
        {
            PostId id = new PostId("2701897");
            Assert.AreEqual(2701897, id.Value);
            Assert.IsTrue(id.Equals(2701897));
        }

        [TestMethod]
        public void Construct_Comma()
        {
            PostId id = new PostId("2,701,897");
            Assert.AreEqual(2701897, id.Value);
            Assert.IsTrue(id.Equals(2701897));
        }

        [TestMethod]
        public void Construct_Int_Overflow()
        {
            PostId id = new PostId("4,294,967,296");
            Assert.AreEqual(4294967296, id.Value);
            Assert.IsTrue(id.Equals(4294967296));
        }

        [TestMethod]
        public void Construct_Negative()
        {
            PostId id = new PostId("-2701897");
            Assert.AreEqual(0, id.Value);
            Assert.IsTrue(id.Equals(0));
        }

        [TestMethod]
        public void Construct_Hex()
        {
            PostId id = new PostId("270A8C7");
            Assert.AreEqual(0, id.Value);
            Assert.IsTrue(id.Equals(0));
        }


        [TestMethod]
        public void Compare_Basic_Equal()
        {
            PostId id1 = new PostId("2701897");
            PostId id2 = new PostId("2701897");
            Assert.AreEqual(id1, id2);
            Assert.IsTrue(id1.CompareTo(id2) == 0);
            Assert.IsTrue(id1.Equals(id2));
            Assert.IsTrue(id1 == id2);
        }

        [TestMethod]
        public void Compare_Basic_Diff()
        {
            PostId id1 = new PostId("2701897");
            PostId id2 = new PostId("2701911");
            Assert.AreNotEqual(id1, id2);
            Assert.IsTrue(id1.CompareTo(id2) == -1);
            Assert.IsFalse(id1.Equals(id2));
            Assert.IsTrue(id1 < id2);
            Assert.IsFalse(id1 == id2);
        }

        [TestMethod]
        public void Compare_Long_Diff()
        {
            PostId id1 = new PostId("2701897");
            PostId id2 = new PostId("4,294,967,296");
            Assert.AreNotEqual(id1, id2);
            Assert.IsTrue(id1.CompareTo(id2) == -1);
            Assert.IsFalse(id1.Equals(id2));
            Assert.IsTrue(id1 < id2);
            Assert.IsFalse(id1 == id2);
        }

        [TestMethod]
        public void Compare_String_Equal()
        {
            PostId id1 = new PostId("2701897");
            PostId id2 = new PostId("2701897");
            Assert.AreEqual(id1, id2);
            Assert.IsTrue(id1.CompareTo(id2.Text) == 0);
            Assert.IsTrue(id1.Equals("2701897"));
        }

        [TestMethod]
        public void Compare_String_Diff()
        {
            PostId id1 = new PostId("2701897");
            PostId id2 = new PostId("2701911");
            Assert.AreNotEqual(id1, id2);
            Assert.IsTrue(id1.CompareTo("2701911") == -1);
            Assert.IsFalse(id1.Equals("2701911"));
            Assert.IsTrue(id1 < "2701911");
            Assert.IsFalse(id1 == "2701911");
        }

    }
}
