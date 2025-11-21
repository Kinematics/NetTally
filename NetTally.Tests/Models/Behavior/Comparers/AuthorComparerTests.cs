using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NetTally.Models;

namespace NetTally.Tests.Models.Behavior.Comparers;

[TestClass]
public class AuthorComparerTests
{
    [ClassInitialize]
    public static void ClassInit(TestContext _)
    {
        TestStartup.ConfigureServices();
    }

    // Local test-only subtype to exercise the NotImplementedException branch.
    private record CustomAuthor() : Author;

    [TestMethod]
    public void Instance_IsNotNull_Singleton()
    {
        Assert.IsNotNull(AuthorComparer.Instance);
    }

    [TestMethod]
    public void Compare_Null_Null_ReturnsZero()
    {
        Author? a = null;
        Author? b = null;

        Assert.AreEqual(0, AuthorComparer.Instance.Compare(a, b));
    }

    [TestMethod]
    public void Compare_Null_Named_ReturnsNegative()
    {
        Author? a = null;
        var b = Author.Create("Kinematics");
        Assert.IsNotNull(b);

        Assert.AreEqual(-1, AuthorComparer.Instance.Compare(a, b));
    }

    [TestMethod]
    public void Compare_Named_Null_ReturnsPositive()
    {
        var a = Author.Create("Kinematics");
        Assert.IsNotNull(a);
        Author? b = null;

        Assert.AreEqual(1, AuthorComparer.Instance.Compare(a, b));
    }

    [TestMethod]
    public void Compare_NoAuthor_NoAuthor_ReturnsZero()
    {
        var a = Author.None;
        var b = Author.None;

        Assert.AreEqual(0, AuthorComparer.Instance.Compare(a, b));
    }

    [TestMethod]
    public void Compare_Unknown_Unknown_ReturnsZero()
    {
        var a = Author.Unknown;
        var b = Author.Unknown;

        Assert.AreEqual(0, AuthorComparer.Instance.Compare(a, b));
    }

    [TestMethod]
    public void Compare_NoAuthor_Vs_Unknown_Behaviour()
    {
        // According to switch logic NoAuthor is ordered before UnknownAuthor
        var no = Author.None;
        var unk = Author.Unknown;

        Assert.AreEqual(-1, AuthorComparer.Instance.Compare(no, unk));
        Assert.AreEqual(1, AuthorComparer.Instance.Compare(unk, no));
    }

    [TestMethod]
    public void Compare_Named_CaseInsensitive_Equal()
    {
        var a = Author.Create("Kinematics");
        var b = Author.Create("kinematics");
        Assert.IsNotNull(a);
        Assert.IsNotNull(b);

        Assert.AreEqual(0, AuthorComparer.Instance.Compare(a, b));
        Assert.IsTrue(AuthorComparer.Instance.Equals(a, b));
    }

    [TestMethod]
    public void Compare_Named_Orderings()
    {
        var a = Author.Create("Aardvark");
        var b = Author.Create("Zebra");
        Assert.IsNotNull(a);
        Assert.IsNotNull(b);

        Assert.AreEqual(-1, AuthorComparer.Instance.Compare(a, b));
        Assert.AreEqual(1, AuthorComparer.Instance.Compare(b, a));
    }

    [TestMethod]
    public void Equals_ReferenceEquals_True()
    {
        var a = Author.Create("Kinematics");
        Assert.IsNotNull(a);

        // same reference
        Assert.IsTrue(AuthorComparer.Instance.Equals(a, a));
    }

    [TestMethod]
    public void Equals_OneNull_False()
    {
        var a = Author.Create("Kinematics");
        Assert.IsNotNull(a);
        Author? b = null;

        Assert.IsFalse(AuthorComparer.Instance.Equals(a, b));
        Assert.IsFalse(AuthorComparer.Instance.Equals(b, a));
    }

    [TestMethod]
    public void GetHashCode_SameForDifferentCase_NamedAuthors()
    {
        var a = Author.Create("Kinematics");
        var b = Author.Create("kinematics");
        Assert.IsNotNull(a);
        Assert.IsNotNull(b);

        int ha = AuthorComparer.Instance.GetHashCode(a);
        int hb = AuthorComparer.Instance.GetHashCode(b);

        Assert.AreEqual(ha, hb);
    }

    [TestMethod]
    public void GetHashCode_DifferentFor_None_And_Unknown()
    {
        var none = Author.None;
        var unknown = Author.Unknown;

        int hn = AuthorComparer.Instance.GetHashCode(none);
        int hu = AuthorComparer.Instance.GetHashCode(unknown);

        Assert.AreNotEqual(hn, hu);
    }

    [TestMethod]
    public void Compare_UnknownAuthorTypes_ThrowsNotImplementedException()
    {
        var custom = new CustomAuthor();
        var named = Author.Create("Kinematics");
        Assert.IsNotNull(named);

        // When comparing a type not handled by the switch pattern, a NotImplementedException is expected.
        Assert.ThrowsExactly<NotImplementedException>(() => AuthorComparer.Instance.Compare(custom, named));
        Assert.ThrowsExactly<NotImplementedException>(() => AuthorComparer.Instance.Compare(named, custom));
        Assert.ThrowsExactly<NotImplementedException>(() => AuthorComparer.Instance.Compare(custom, custom));
    }
}