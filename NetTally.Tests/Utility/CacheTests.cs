using System;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Time.Testing;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NetTally.Cache;

namespace NetTally.Tests.Utility
{
    [TestClass]
    public class CacheTests
    {
        static PageCache cache = null!;
        static string resourceContent = string.Empty;
        static readonly FakeTimeProvider fakeTimeProvider = new();

        [ClassInitialize]
        public static async Task Initialize(TestContext _)
        {
            resourceContent = await LoadResource.Read("Resources/RenascenceSV.html") ?? string.Empty;

            var serviceProvider = TestStartup.ConfigureServices(fakeTimeProvider);

            cache = serviceProvider.GetRequiredService<PageCache>();
        }

        [TestInitialize]
        public void Prepare()
        {
            cache.Clear();
        }

        [TestMethod]
        public void ContentLoaded()
        {
            Assert.IsFalse(string.IsNullOrEmpty(resourceContent));
            Assert.IsTrue(resourceContent.Length > 200000);
            Assert.IsTrue(resourceContent.Length < 250000);
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentNullException))]
        public void Cache_null_key()
        {
            cache.Add(null!, null!, CacheInfo.DefaultExpiration);
        }

        [TestMethod]
        public void Cache_null_data()
        {
            string data = null!;

            cache.Add("null data", data, CacheInfo.DefaultExpiration);
            var (found, content) = cache.Get("null data");

            Assert.IsTrue(found);
            Assert.AreEqual("", content);
        }

        [TestMethod]
        public void Cache_page_data()
        {
            cache.Add("page data", resourceContent, CacheInfo.DefaultExpiration);
            var (found, content) = cache.Get("page data");

            Assert.IsTrue(found);
            Assert.AreEqual(resourceContent, content);
        }

        [TestMethod]
        public void Cache_expired_data()
        {
            DateTimeOffset clockTime = new(2017, 7, 1, 12, 0, 0, TimeSpan.Zero);
            DateTimeOffset expireTime = new(2017, 7, 1, 11, 59, 0, TimeSpan.Zero);

            fakeTimeProvider.SetUtcNow(clockTime);

            cache.Add("page data", resourceContent, expireTime);
            var (found, _) = cache.Get("page data");

            Assert.IsFalse(found);
        }

        [TestMethod]
        public void Cache_unexpired_data()
        {
            DateTimeOffset clockTime = new(2017, 7, 1, 12, 0, 0, TimeSpan.Zero);
            DateTimeOffset expireTime = new(2017, 7, 1, 12, 1, 0, TimeSpan.Zero);

            fakeTimeProvider.SetUtcNow(clockTime);

            cache.Add("page data", resourceContent, expireTime);
            var (found, content) = cache.Get("page data");

            Assert.IsTrue(found);
            Assert.AreEqual(resourceContent, content);
        }

        [TestMethod]
        public void Cache_expired_data_invalidate()
        {
            DateTimeOffset clockTime = new(2017, 7, 1, 12, 0, 0, TimeSpan.Zero);
            DateTimeOffset expireTime = new(2017, 7, 1, 11, 59, 0, TimeSpan.Zero);

            fakeTimeProvider.SetUtcNow(clockTime);

            int storeCount = cache.MaxCacheEntries + 2;
            for (int i = 0; i < storeCount; i++)
            {
                cache.Add($"page data {i}", "A page of data", expireTime);
            }

            Assert.AreEqual(storeCount, cache.Count);
            cache.InvalidateCache();
            Assert.AreEqual(0, cache.Count);
        }
    }
}
