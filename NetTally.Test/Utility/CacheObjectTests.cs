using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NetTally.Cache;
using NetTally.SystemInfo;

namespace TallyUnitTest.Utility
{
    [TestClass]
    public class CacheObjectTests
    {

        [TestMethod]
        public void Create_default()
        {
            string str = "hello";
            CacheObject<string> cacheObject = new CacheObject<string>(str);
            Assert.AreEqual(str, cacheObject.Store);
            Assert.AreEqual(CacheInfo.DefaultExpiration, cacheObject.Expires);
        }

        [TestMethod]
        public void Create_expires()
        {
            string str = "hello";
            DateTime expireTime = DateTime.Now;
            CacheObject<string> cacheObject = new CacheObject<string>(str, expireTime);
            Assert.AreEqual(str, cacheObject.Store);
            Assert.AreEqual(expireTime, cacheObject.Expires);
        }

        [TestMethod]
        public void Create_timestamp()
        {
            string str = "hello";
            DateTime timestamp = DateTime.Now;
            DateTime expireTime = timestamp.AddMinutes(1);
            CacheObject<string> cacheObject = new CacheObject<string>(str, expireTime, timestamp);
            Assert.AreEqual(str, cacheObject.Store);
            Assert.AreEqual(timestamp, cacheObject.Timestamp);
            Assert.AreEqual(expireTime, cacheObject.Expires);
        }

        [TestMethod]
        public void GetHashcode()
        {
            string str = "hello";
            CacheObject<string> cacheObject = new CacheObject<string>(str);
            Assert.AreEqual(str.GetHashCode(), cacheObject.GetHashCode());
        }

        [TestMethod]
        public void Equals_copy()
        {
            string str = "hello";
            CacheObject<string> cacheObject1 = new CacheObject<string>(str);
            CacheObject<string> cacheObject2 = cacheObject1;
            Assert.IsTrue(cacheObject1.Equals(cacheObject2));
        }

        [TestMethod]
        public void Equals_notequal()
        {
            CacheObject<string> cacheObject1 = new CacheObject<string>("hello");
            CacheObject<string> cacheObject2 = new CacheObject<string>("world");
            Assert.IsFalse(cacheObject1.Equals(cacheObject2));
        }

        [TestMethod]
        public void Equals_notequal_nontype()
        {
            CacheObject<string> cacheObject1 = new CacheObject<string>("hello");
            List<string> list = new List<string> { "hello" };
            Assert.IsFalse(cacheObject1.Equals(list));
        }

        [TestMethod]
        public void Equals_content()
        {
            string str = "hello";
            CacheObject<string> cacheObject1 = new CacheObject<string>(str);
            CacheObject<string> cacheObject2 = new CacheObject<string>(str);
            Assert.IsTrue(cacheObject1.Equals(cacheObject2));
        }

        [TestMethod]
        public void Equals_store()
        {
            string str = "hello";
            CacheObject<string> cacheObject1 = new CacheObject<string>(str);
            Assert.IsTrue(cacheObject1.Equals(str));
        }
    }
}
