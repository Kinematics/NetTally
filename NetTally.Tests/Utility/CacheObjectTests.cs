using System;
using System.Collections.Generic;
using Microsoft.Extensions.Time.Testing;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NetTally.Cache;

namespace NetTally.Tests.Utility
{
    [TestClass]
    public class CacheObjectTests
    {
        static FakeTimeProvider fake = null!;

        [ClassInitialize]
        public static void ClassInitialize(TestContext context)
        {
            fake = new FakeTimeProvider();
            CacheInfo.TimeProvider = fake;
        }


        [TestMethod]
        public void Create_Default()
        {
            string str = "hello";
            CacheObject<string> cacheObject = new(str);
            Assert.AreEqual(str, cacheObject.Store);
            Assert.AreEqual(CacheInfo.DefaultExpiration, cacheObject.Expires);
        }

        [TestMethod]
        public void Create_ExpiresAt()
        {
            fake.SetUtcNow(new DateTimeOffset(2024, 4, 1, 12, 0, 0, TimeSpan.Zero));

            string str = "hello";
            DateTimeOffset expireTime = fake.GetUtcNow();
            CacheObject<string> cacheObject = new(str, expireTime);
            Assert.AreEqual(str, cacheObject.Store);
            Assert.AreEqual(expireTime, cacheObject.Expires);
        }

        [TestMethod]
        public void Create_ExpiresIn()
        {
            fake.SetUtcNow(new DateTimeOffset(2024, 4, 1, 12, 0, 0, TimeSpan.Zero));

            string str = "hello";
            TimeSpan expireIn = TimeSpan.FromMinutes(1);
            DateTimeOffset expireTime = fake.GetUtcNow() + expireIn;

            CacheObject<string> cacheObject = new(str, expireIn);

            Assert.AreEqual(str, cacheObject.Store);
            Assert.AreEqual(expireTime, cacheObject.Expires);
        }

        [TestMethod]
        public void Create_Timestamp()
        {
            fake.SetUtcNow(new DateTimeOffset(2024, 4, 1, 12, 0, 0, TimeSpan.Zero));

            string str = "hello";
            DateTimeOffset timestamp = fake.GetUtcNow();
            DateTimeOffset expireTime = timestamp.AddMinutes(1);
            
            CacheObject<string> cacheObject = new(str, expireTime, timestamp);

            Assert.AreEqual(str, cacheObject.Store);
            Assert.AreEqual(timestamp, cacheObject.Timestamp);
            Assert.AreEqual(expireTime, cacheObject.Expires);
        }

        [TestMethod]
        public void GetHashcode()
        {
            string str = "hello";
            CacheObject<string> cacheObject = new(str);
            Assert.AreEqual(str.GetHashCode(), cacheObject.GetHashCode());
        }

        [TestMethod]
        public void Equals_Copy()
        {
            string str = "hello";
            CacheObject<string> cacheObject1 = new(str);
            CacheObject<string> cacheObject2 = cacheObject1;
            Assert.IsTrue(cacheObject1.Equals(cacheObject2));
        }

        [TestMethod]
        public void Equals_NotEqual()
        {
            CacheObject<string> cacheObject1 = new("hello");
            CacheObject<string> cacheObject2 = new("world");
            Assert.IsFalse(cacheObject1.Equals(cacheObject2));
        }

        [TestMethod]
        public void Equals_NotEqual_NonType()
        {
            CacheObject<string> cacheObject1 = new("hello");
            List<string> list = ["hello"];
            Assert.IsFalse(cacheObject1.Equals(list));
        }

        [TestMethod]
        public void Equals_Content()
        {
            string str = "hello";
            CacheObject<string> cacheObject1 = new(str);
            CacheObject<string> cacheObject2 = new(str);
            Assert.IsTrue(cacheObject1.Equals(cacheObject2));
        }

        [TestMethod]
        public void Equals_Store()
        {
            string str = "hello";
            CacheObject<string> cacheObject1 = new(str);
            Assert.IsTrue(cacheObject1.Equals(str));
        }
    }
}
