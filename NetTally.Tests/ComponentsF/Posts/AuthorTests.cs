using Microsoft.VisualStudio.TestTools.UnitTesting;
using NetTally.Tally.ComponentsF.Posts;

namespace NetTally.Tests.ComponentsF.Posts;
[TestClass]
public class AuthorTests
{
    [ClassInitialize]
    public static void ClassInit(TestContext _)
    {
        TestStartup.ConfigureServices();
    }

    [TestMethod]
    public void Construct_Null_Error()
    {
        var author = Author.Create(null!);
        Assert.AreEqual(Author.None, author);
    }

    [TestMethod]
    public void Construct_Empty_Error()
    {
        var author = Author.Create("");
        Assert.AreEqual(Author.None, author);
    }

    [TestMethod]
    public void Construct_Space_Error()
    {
        var author = Author.Create("     ");
        Assert.AreEqual(Author.None, author);
    }

    [TestMethod]
    public void Construct_SpaceEnd_Trimmed()
    {
        var author = Author.Create("Kinematics     ");
        Assert.AreEqual("Kinematics", author.Name);
    }

    [TestMethod]
    public void Construct_SpaceAround_Trimmed()
    {
        var author = Author.Create("  Kinematics     ");
        Assert.AreEqual("Kinematics", author.Name);
    }

    [TestMethod]
    public void Construct_Unsafe_Cleaned()
    {
        var author = Author.Create("Kinema\u200btics");
        Assert.AreEqual("Kinematics", author.Name);
    }

    [TestMethod]
    public void Construct_UTF_Normal()
    {
        var author = Author.Create("KinematicsΩ°");
        Assert.AreEqual("KinematicsΩ°", author.Name);
    }

    [TestMethod]
    public void AreEqual_Normal()
    {
        var author1 = Author.Create("Kinematics");
        var author2 = Author.Create("Kinematics");

        Assert.IsTrue(AuthorComparer.Instance.Equals(author1, author2));
    }

    [TestMethod]
    public void AreEqual_CaseDiff1()
    {
        var author1 = Author.Create("Kinematics");
        var author2 = Author.Create("kinematics");

        Assert.IsTrue(AuthorComparer.Instance.Equals(author1, author2));
    }

    [TestMethod]
    public void AreEqual_CaseDiff2()
    {
        var author1 = Author.Create("Kinematics");
        var author2 = Author.Create("KINEMATICS");

        Assert.IsTrue(AuthorComparer.Instance.Equals(author1, author2));
    }

    [TestMethod]
    public void AreEqual_CaseDiff3_UTF()
    {
        var author1 = Author.Create("KinematicsΩ");
        var author2 = Author.Create("KINEMATICSΩ");

        Assert.IsTrue(AuthorComparer.Instance.Equals(author1, author2));
    }

    [TestMethod]
    public void AreEqual_CaseDiff4_UTF()
    {
        var author1 = Author.Create("Kinematicsω");
        var author2 = Author.Create("KINEMATICSΩ");

        Assert.IsTrue(AuthorComparer.Instance.Equals(author1, author2));
    }

    [TestMethod]
    public void CompareWith_Ascend()
    {
        var author1 = Author.Create("Kinematics");
        var author2 = Author.Create("Ubrey");

        Assert.AreEqual(-1, AuthorComparer.Instance.Compare(author1, author2));
    }

    [TestMethod]
    public void CompareWith_Descend()
    {
        var author1 = Author.Create("Kinematics");
        var author2 = Author.Create("Aubrey");

        Assert.AreEqual(1, AuthorComparer.Instance.Compare(author1, author2));
    }

    [TestMethod]
    public void CompareWith_Equal()
    {
        var author1 = Author.Create("Kinematics");
        var author2 = Author.Create("kinematics");

        Assert.AreEqual(0, AuthorComparer.Instance.Compare(author1, author2));
    }
}
