using System;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Time.Testing;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NetTally.Utility.Cache;

namespace NetTally.Tests.Utility;

[TestClass]
public class CacheServiceTests
{
    static CacheService cacheService = null!;
    static readonly FakeTimeProvider fakeTimeProvider = new();

    [ClassInitialize]
    public static void ClassInit(TestContext _)
    {
        var serviceProvider = TestStartup.ConfigureServices();

        cacheService = serviceProvider.GetRequiredService<CacheService>();
    }

    [TestInitialize]
    public void TestInit()
    {
        cacheService.Clear();
    }

    [TestMethod]
    public void AddNull_Value_Fail()
    {
        Assert.ThrowsExactly<ArgumentNullException>(() => cacheService.Add("Key", null!));
    }

    [TestMethod]
    public void AddNull_Key_Fail()
    {
        Assert.ThrowsExactly<ArgumentNullException>(() => cacheService.Add(null!, "stuff"));
    }

    [TestMethod]
    public void AddEmpty_Value_Fail()
    {
        Assert.ThrowsExactly<ArgumentException>(() => cacheService.Add("Key", " "));
    }

    [TestMethod]
    public void AddEmpty_Key_Fail()
    {
        Assert.ThrowsExactly<ArgumentException>(() => cacheService.Add(" ", "stuff"));
    }

    [TestMethod]
    public void AddValue_Check()
    {
        string key = "key";
        string val = "value";
        cacheService.Add(key, val);

        bool found = cacheService.TryGet(key, out var value);
        Assert.IsTrue(found);
        Assert.AreEqual(val, value);
    }

    [TestMethod]
    public void AddValue_Check_UTF()
    {
        string key = "key2";
        string val = "value of Amélie";
        cacheService.Add(key, val);

        bool found = cacheService.TryGet(key, out var value);
        Assert.IsTrue(found);
        Assert.AreEqual(val, value);
    }

    [TestMethod]
    public async Task AddValue_FullPage()
    {
        string filename = "Resources/RenascenceSV.html";
        var resourceContent = await LoadResource.Read(filename);

        Assert.IsNotNull(resourceContent);

        cacheService.Add(filename, resourceContent);

        bool found = cacheService.TryGet(filename, out var value);
        Assert.IsTrue(found);
        Assert.AreEqual(resourceContent, value);
    }

    [TestMethod]
    public void AddValue_Count_1()
    {
        string key = "key";
        string val = "value";
        cacheService.Add(key, val);

        Assert.AreEqual(1, cacheService.Count);
    }

    [TestMethod]
    public void AddValue_Count_2()
    {
        string key1 = "key";
        string val1 = "value";
        cacheService.Add(key1, val1);
        string key2 = "keys";
        string val2 = "values";
        cacheService.Add(key2, val2);

        Assert.AreEqual(2, cacheService.Count);
    }

    [TestMethod]
    public void AddValue_Clear()
    {
        string key1 = "key";
        string val1 = "value";
        cacheService.Add(key1, val1);
        string key2 = "keys";
        string val2 = "values";
        cacheService.Add(key2, val2);

        Assert.AreEqual(2, cacheService.Count);

        cacheService.Clear();

        Assert.AreEqual(0, cacheService.Count);
    }

    [TestMethod]
    public void AddValue_Duplicate()
    {
        string key1 = "key";
        string val1 = "value";
        cacheService.Add(key1, val1);
        string key2 = "key";
        string val2 = "value";
        cacheService.Add(key2, val2);

        Assert.AreEqual(1, cacheService.Count);

        bool found = cacheService.TryGet(key1, out var value);
        Assert.IsTrue(found);
        Assert.AreEqual(val2, value);
    }

    [TestMethod]
    public void AddValue_Overwrite()
    {
        string key1 = "key";
        string val1 = "value";
        cacheService.Add(key1, val1);
        string key2 = "key";
        string val2 = "value2";
        cacheService.Add(key2, val2);

        Assert.AreEqual(1, cacheService.Count);

        bool found = cacheService.TryGet(key1, out var value);
        Assert.IsTrue(found);
        Assert.AreEqual(val2, value);
    }
}
