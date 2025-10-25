using Microsoft.VisualStudio.TestTools.UnitTesting;
using NetTally.Configure;
using NetTally.Models;

namespace NetTally.Tests.Models.Posts;

[TestClass]
public class AuthorTests
{
    [ClassInitialize]
    public static void ClassInit(TestContext _)
    {
        TestStartup.ConfigureServices();
    }

    [TestMethod]
    [TestCategory("Creation")]
    public void Construct_Null_Null()
    {
        var author = Author.Create(null);
        Assert.IsNull(author);
    }

    [TestMethod]
    [TestCategory("Creation")]
    public void Construct_Empty_Null()
    {
        var author = Author.Create("");
        Assert.IsNull(author);
    }

    [TestMethod]
    [TestCategory("Creation")]
    public void Construct_Space_Null()
    {
        var author = Author.Create("     ");
        Assert.IsNull(author);
    }

    [TestMethod]
    [TestCategory("Creation")]
    public void Construct_Invalid_Null()
    {
        var author = Author.Create("​\u200b");
        Assert.IsNull(author);
    }

    [TestMethod]
    [TestCategory("Creation")]
    public void Construct_InvalidSpace_Null()
    {
        var author = Author.Create("  \u200b ");
        Assert.IsNull(author);
    }

    [TestMethod]
    [TestCategory("Creation")]
    public void Construct_Basic_Normal()
    {
        var author = Author.Create("Kinematics");
        Assert.IsNotNull(author);
        Assert.IsTrue(author is NamedAuthor);
        if (author is  NamedAuthor na)
        {
            Assert.AreEqual("Kinematics", na.Name);
        }
    }

    [TestMethod]
    [TestCategory("Creation")]
    public void Construct_SpaceEnd_Trimmed()
    {
        var author = Author.Create("Kinematics     ");
        Assert.IsNotNull(author);
        Assert.AreEqual("Kinematics", author.DisplayName);
    }

    [TestMethod]
    [TestCategory("Creation")]
    public void Construct_SpaceAround_Trimmed()
    {
        var author = Author.Create("  Kinematics     ");
        Assert.IsNotNull(author);
        Assert.AreEqual("Kinematics", author.DisplayName);
    }

    [TestMethod]
    [TestCategory("Creation")]
    public void Construct_Unsafe_Cleaned()
    {
        var author = Author.Create("Kinema\u200btics");
        Assert.IsNotNull(author);
        Assert.AreEqual("Kinematics", author.DisplayName);
    }

    [TestMethod]
    [TestCategory("Creation")]
    public void Construct_UTF_Normal()
    {
        var author = Author.Create("KinematicsΩ°");
        Assert.IsNotNull(author);
        Assert.AreEqual("KinematicsΩ°", author.DisplayName);
    }


    [TestMethod]
    [TestCategory("Comparison")]
    public void IsEqual_Normal_Same()
    {
        var author1 = Author.Create("Kinematics");
        var author2 = Author.Create("Kinematics");

        Assert.AreEqual(author1, author2, AuthorComparer.Instance);
    }

    [TestMethod]
    [TestCategory("Comparison")]
    public void IsEqual_Lowercase_Same()
    {
        var author1 = Author.Create("Kinematics");
        var author2 = Author.Create("kinematics");

        Assert.AreEqual(author1, author2, AuthorComparer.Instance);
    }

    [TestMethod]
    [TestCategory("Comparison")]
    public void IsEqual_Uppercase_Same()
    {
        var author1 = Author.Create("Kinematics");
        var author2 = Author.Create("KINEMATICS");

        Assert.AreEqual(author1, author2, AuthorComparer.Instance);
    }

    [TestMethod]
    [TestCategory("Comparison")]
    public void IsEqual_UppercaseUTF1_Same()
    {
        var author1 = Author.Create("KinematicsΩ");
        var author2 = Author.Create("KINEMATICSΩ");

        Assert.AreEqual(author1, author2, AuthorComparer.Instance);
    }

    [TestMethod]
    [TestCategory("Comparison")]
    public void IsEqual_UppercaseUTF2_Same()
    {
        var author1 = Author.Create("Kinematicsω");
        var author2 = Author.Create("KINEMATICSΩ");

        Assert.AreEqual(author1, author2, AuthorComparer.Instance);
    }

    [TestMethod]
    [TestCategory("Comparison")]
    public void IsEqual_Similar_NotEqual()
    {
        var author1 = Author.Create("Kinematicsω");
        var author2 = Author.Create("KINEMATICS");

        Assert.AreNotEqual(author1, author2, AuthorComparer.Instance);
    }

    [TestMethod]
    [TestCategory("Comparison")]
    public void IsEqual_None_NotEqual()
    {
        var author1 = Author.Create("Kinematicsω");
        var author2 = Author.None;

        Assert.AreNotEqual(author1, author2, AuthorComparer.Instance);
    }

    [TestMethod]
    [TestCategory("Comparison")]
    public void Compare_Ascending()
    {
        var author1 = Author.Create("Kinematics");
        var author2 = Author.Create("Ubrey");

        Assert.AreEqual(-1, AuthorComparer.Instance.Compare(author1, author2));
    }

    [TestMethod]
    [TestCategory("Comparison")]
    public void Compare_Descending()
    {
        var author1 = Author.Create("Kinematics");
        var author2 = Author.Create("Aubrey");

        Assert.AreEqual(1, AuthorComparer.Instance.Compare(author1, author2));
    }

    [TestMethod]
    [TestCategory("Comparison")]
    public void Compare_Same_Equal()
    {
        var author1 = Author.Create("Kinematics");
        var author2 = Author.Create("kinematics");

        Assert.AreEqual(0, AuthorComparer.Instance.Compare(author1, author2));
    }

    [TestMethod]
    [TestCategory("Comparison")]
    public void CompareNulls_Equal()
    {
        Author? author1 = null;
        Author? author2 = null;

        Assert.AreEqual(author1, author2, AuthorComparer.Instance);
        Assert.AreEqual(0, AuthorComparer.Instance.Compare(author1, author2));
    }

    [TestMethod]
    [TestCategory("Comparison")]
    public void CompareNullToNone_NotEqual()
    {
        Author? author1 = null;
        Author? author2 = Author.None;

        Assert.AreNotEqual(author1, author2, AuthorComparer.Instance);
        Assert.AreEqual(-1, AuthorComparer.Instance.Compare(author1, author2));
    }

    [TestMethod]
    [TestCategory("Comparison")]
    public void CompareNoneToNull_NotEqual()
    {
        Author? author1 = Author.None;
        Author? author2 = null;

        Assert.AreNotEqual(author1, author2, AuthorComparer.Instance);
        Assert.AreEqual(1, AuthorComparer.Instance.Compare(author1, author2));
    }

    [TestMethod]
    [TestCategory("Comparison")]
    public void CompareNullToUnknown_NotEqual()
    {
        Author? author1 = null;
        Author? author2 = Author.Unknown;

        Assert.AreNotEqual(author1, author2, AuthorComparer.Instance);
        Assert.AreEqual(-1, AuthorComparer.Instance.Compare(author1, author2));
    }

    [TestMethod]
    [TestCategory("Comparison")]
    public void CompareUnknownToNull_NotEqual()
    {
        Author? author1 = Author.Unknown;
        Author? author2 = null;

        Assert.AreNotEqual(author1, author2, AuthorComparer.Instance);
        Assert.AreEqual(1, AuthorComparer.Instance.Compare(author1, author2));
    }

    [TestMethod]
    [TestCategory("Comparison")]
    public void CompareNullToNamed_NotEqual()
    {
        Author? author1 = null;
        Author? author2 = Author.Create("Kinematics");

        Assert.AreNotEqual(author1, author2, AuthorComparer.Instance);
        Assert.AreEqual(-1, AuthorComparer.Instance.Compare(author1, author2));
    }

    [TestMethod]
    [TestCategory("Comparison")]
    public void CompareNamedToNull_NotEqual()
    {
        Author? author1 = Author.Create("Kinematics");
        Author? author2 = null;

        Assert.AreNotEqual(author1, author2, AuthorComparer.Instance);
        Assert.AreEqual(1, AuthorComparer.Instance.Compare(author1, author2));
    }

    [TestMethod]
    [TestCategory("Comparison")]
    public void CompareNoneToNone_Equal()
    {
        Author author1 = Author.None;
        Author author2 = Author.None;

        Assert.AreEqual(author1, author2, AuthorComparer.Instance);
        Assert.AreEqual(0, AuthorComparer.Instance.Compare(author1, author2));
    }

    [TestMethod]
    [TestCategory("Comparison")]
    public void CompareNoneToUnknown_LessThan()
    {
        Author author1 = Author.None;
        Author author2 = Author.Unknown;

        Assert.AreNotEqual(author1, author2, AuthorComparer.Instance);
        Assert.AreEqual(-1, AuthorComparer.Instance.Compare(author1, author2));
    }

    [TestMethod]
    [TestCategory("Comparison")]
    public void CompareUnknownToNone_GreaterThan()
    {
        Author author1 = Author.Unknown;
        Author author2 = Author.None;

        Assert.AreNotEqual(author1, author2, AuthorComparer.Instance);
        Assert.AreEqual(1, AuthorComparer.Instance.Compare(author1, author2));
    }

    [TestMethod]
    [TestCategory("Comparison")]
    public void CompareNoneToNamed_LessThan()
    {
        Author author1 = Author.None;
        Author? author2 = Author.Create("Kinematics");
        Assert.IsNotNull(author2);

        Assert.AreNotEqual(author1, author2, AuthorComparer.Instance);
        Assert.AreEqual(-1, AuthorComparer.Instance.Compare(author1, author2));
    }

    [TestMethod]
    [TestCategory("Comparison")]
    public void CompareNamedToNone_GreaterThan()
    {
        Author? author1 = Author.Create("Kinematics");
        Author author2 = Author.None;
        Assert.IsNotNull(author1);

        Assert.AreNotEqual(author1, author2, AuthorComparer.Instance);
        Assert.AreEqual(1, AuthorComparer.Instance.Compare(author1, author2));
    }

    [TestMethod]
    [TestCategory("Comparison")]
    public void CompareUnknownToUnknown_Equal()
    {
        Author author1 = Author.Unknown;
        Author author2 = Author.Unknown;

        Assert.AreEqual(author1, author2, AuthorComparer.Instance);
        Assert.AreEqual(0, AuthorComparer.Instance.Compare(author1, author2));
    }

    [TestMethod]
    [TestCategory("Comparison")]
    public void CompareUnknownToNamed_LessThan()
    {
        Author author1 = Author.Unknown;
        Author? author2 = Author.Create("Kinematics");
        Assert.IsNotNull(author2);

        Assert.AreNotEqual(author1, author2, AuthorComparer.Instance);
        Assert.AreEqual(-1, AuthorComparer.Instance.Compare(author1, author2));
    }

    [TestMethod]
    [TestCategory("Comparison")]
    public void CompareNamedToUnknown_GreaterThan()
    {
        Author? author1 = Author.Create("Kinematics");
        Author author2 = Author.Unknown;
        Assert.IsNotNull(author1);

        Assert.AreNotEqual(author1, author2, AuthorComparer.Instance);
        Assert.AreEqual(1, AuthorComparer.Instance.Compare(author1, author2));
    }

    [TestMethod]
    [TestCategory("Extensions")]
    public void Utility_DisplayName_Normal()
    {
        string name = "Kinematics";
        Author? author = Author.Create(name);
        Assert.IsNotNull(author);

        Assert.AreEqual(name, author.DisplayName);
    }

    [TestMethod]
    [TestCategory("Extensions")]
    public void Utility_DisplayName_Unknown()
    {
        Author author = Author.Unknown;
        Assert.IsNotNull(author);

        Assert.AreEqual(Strings.UnknownAuthor, author.DisplayName);
    }

    [TestMethod]
    [TestCategory("Extensions")]
    public void Utility_DisplayName_None()
    {
        Author author = Author.None;
        Assert.IsNotNull(author);

        Assert.AreEqual(Strings.NoAuthor, author.DisplayName);
    }

    [TestMethod]
    [TestCategory("Extensions")]
    public void Utility_Rename_Normal()
    {
        string name = "Kinematics";
        string to = "Aubrey";

        Author? author = Author.Create(name);
        Assert.IsNotNull(author);
        Assert.AreEqual(name, author.DisplayName);

        Author? author1 = author.Rename(to);
        Assert.IsNotNull(author1);
        Assert.AreEqual(to, author1.DisplayName);
    }

    [TestMethod]
    [TestCategory("Extensions")]
    public void Utility_Rename_Unknown()
    {
        string to = "Aubrey";

        Author? author = Author.Unknown;
        Assert.IsNotNull(author);
        Assert.AreEqual(Strings.UnknownAuthor, author.DisplayName);

        Author? author1 = author.Rename(to);
        Assert.IsNotNull(author1);
        Assert.AreEqual(to, author1.DisplayName);

        Assert.AreEqual(Strings.UnknownAuthor, Author.Unknown.DisplayName);
    }

    [TestMethod]
    [TestCategory("Extensions")]
    public void Utility_Rename_None()
    {
        string to = "Aubrey";

        Author? author = Author.None;
        Assert.IsNotNull(author);
        Assert.AreEqual(Strings.NoAuthor, author.DisplayName);

        Author? author1 = author.Rename(to);
        Assert.IsNotNull(author1);
        Assert.AreEqual(to, author1.DisplayName);

        Assert.AreEqual(Strings.NoAuthor, Author.None.DisplayName);
    }

    [TestMethod]
    [TestCategory("Extensions")]
    public void Utility_Rename_Normal_Fail()
    {
        string name = "Kinematics";
        string to = "";

        Author? author = Author.Create(name);
        Assert.IsNotNull(author);
        Assert.AreEqual(name, author.DisplayName);

        Author? author1 = author.Rename(to);
        Assert.IsNull(author1);
    }

    [TestMethod]
    [TestCategory("Extensions")]
    public void Utility_Rename_Unknown_Fail ()
    {
        string to = "";

        Author? author = Author.Unknown;
        Assert.IsNotNull(author);
        Assert.AreEqual(Strings.UnknownAuthor, author.DisplayName);

        Author? author1 = author.Rename(to);
        Assert.IsNull(author1);
    }

    [TestMethod]
    [TestCategory("Extensions")]
    public void Utility_Rename_None_Fail()
    {
        string to = "";

        Author? author = Author.None;
        Assert.IsNotNull(author);
        Assert.AreEqual(Strings.NoAuthor, author.DisplayName);

        Author? author1 = author.Rename(to);
        Assert.IsNull(author1);
    }
}
