using Microsoft.VisualStudio.TestTools.UnitTesting;
using NetTally.Utility.Comparers;

namespace NetTally.Tests.Utility;

[TestClass]
public class StringComparerTests
{
    [ClassInitialize]
    public static void Initialize(TestContext _)
    {
        TestStartup.ConfigureServices();
    }

    #region Hash function hash comparisons
    [TestMethod]
    public void Insensitive_01_space()
    {
        var comparer = Agnostic.InsensitiveComparer;
        string str1 = "Kinematics";
        string str2 = "Kinematics ";

        int result1 = comparer.GetHashCode(str1);
        int result2 = comparer.GetHashCode(str2);

        Assert.AreEqual(result1, result2);

        Assert.IsTrue(comparer.Equals(str1, str2));
    }

    [TestMethod]
    public void CaseInsensitive_01_space()
    {
        var comparer = Agnostic.CaseInsensitiveComparer;
        string str1 = "Kinematics";
        string str2 = "Kinematics ";

        int result1 = comparer.GetHashCode(str1);
        int result2 = comparer.GetHashCode(str2);

        Assert.AreNotEqual(result1, result2);

        Assert.IsFalse(comparer.Equals(str1, str2));
    }

    [TestMethod]
    public void Insensitive_02_extra_chars()
    {
        var comparer = Agnostic.InsensitiveComparer;
        string str1 = "Kinematics";
        string str2 = "Kinematicss";

        int result1 = comparer.GetHashCode(str1);
        int result2 = comparer.GetHashCode(str2);

        Assert.AreNotEqual(result1, result2);

        Assert.IsFalse(comparer.Equals(str1, str2));
    }

    [TestMethod]
    public void CaseInsensitive_02_extra_chars()
    {
        var comparer = Agnostic.CaseInsensitiveComparer;
        string str1 = "Kinematics";
        string str2 = "Kinematicss";

        int result1 = comparer.GetHashCode(str1);
        int result2 = comparer.GetHashCode(str2);

        Assert.AreNotEqual(result1, result2);

        Assert.IsFalse(comparer.Equals(str1, str2));
    }

    [TestMethod]
    public void Insensitive_03_diacriticals()
    {
        var comparer = Agnostic.InsensitiveComparer;
        string str1 = "resume";
        string str2 = "resumé";

        int result1 = comparer.GetHashCode(str1);
        int result2 = comparer.GetHashCode(str2);

        Assert.AreEqual(result1, result2);

        Assert.IsTrue(comparer.Equals(str1, str2));
    }

    [TestMethod]
    public void CaseInsensitive_03_diacriticals()
    {
        var comparer = Agnostic.CaseInsensitiveComparer;
        string str1 = "resume";
        string str2 = "resumé";

        int result1 = comparer.GetHashCode(str1);
        int result2 = comparer.GetHashCode(str2);

        Assert.AreEqual(result1, result2);

        Assert.IsTrue(comparer.Equals(str1, str2));
    }

    [TestMethod]
    public void Insensitive_04_combDiacriticals()
    {
        var comparer = Agnostic.InsensitiveComparer;
        string str1 = "resume";
        string str2 = "resumé";

        int result1 = comparer.GetHashCode(str1);
        int result2 = comparer.GetHashCode(str2);

        Assert.AreEqual(result1, result2);

        Assert.IsTrue(comparer.Equals(str1, str2));
    }

    [TestMethod]
    public void CaseInsensitive_04_combDiacriticals()
    {
        var comparer = Agnostic.CaseInsensitiveComparer;
        string str1 = "resume";
        string str2 = "resumé";

        int result1 = comparer.GetHashCode(str1);
        int result2 = comparer.GetHashCode(str2);

        Assert.AreEqual(result1, result2);

        Assert.IsTrue(comparer.Equals(str1, str2));
    }

    [TestMethod]
    public void Insensitive_05_fraction_form()
    {
        var comparer = Agnostic.InsensitiveComparer;
        string str1 = "Ranma ½";
        string str2 = "Ranma 1/2";

        int result1 = comparer.GetHashCode(str1);
        int result2 = comparer.GetHashCode(str2);

        Assert.AreEqual(result1, result2);

        Assert.IsTrue(comparer.Equals(str1, str2));
    }

    [TestMethod]
    public void CaseInsensitive_05_fraction_form()
    {
        var comparer = Agnostic.CaseInsensitiveComparer;
        string str1 = "Ranma ½";
        string str2 = "Ranma 1/2";

        int result1 = comparer.GetHashCode(str1);
        int result2 = comparer.GetHashCode(str2);

        Assert.AreNotEqual(result1, result2);

        Assert.IsFalse(comparer.Equals(str1, str2));
    }

    [TestMethod]
    public void Insensitive_05a_fraction_form_ignore()
    {
        var comparer = Agnostic.InsensitiveComparer;
        string str1 = "Ranma ½";
        string str2 = "Ranma";

        int result1 = comparer.GetHashCode(str1);
        int result2 = comparer.GetHashCode(str2);

        Assert.AreNotEqual(result1, result2);

        Assert.IsFalse(comparer.Equals(str1, str2));
    }

    [TestMethod]
    public void CaseInsensitive_05a_fraction_form_ignore()
    {
        var comparer = Agnostic.CaseInsensitiveComparer;
        string str1 = "Ranma ½";
        string str2 = "Ranma";

        int result1 = comparer.GetHashCode(str1);
        int result2 = comparer.GetHashCode(str2);

        Assert.AreNotEqual(result1, result2);

        Assert.IsFalse(comparer.Equals(str1, str2));
    }

    [TestMethod]
    public void Insensitive_06_punctuation()
    {
        var comparer = Agnostic.InsensitiveComparer;
        string str1 = "[bank]";
        string str2 = "bank";

        int result1 = comparer.GetHashCode(str1);
        int result2 = comparer.GetHashCode(str2);

        Assert.AreEqual(result1, result2);

        Assert.IsTrue(comparer.Equals(str1, str2));
    }

    [TestMethod]
    public void CaseInsensitive_06_punctuation()
    {
        var comparer = Agnostic.CaseInsensitiveComparer;
        string str1 = "[bank]";
        string str2 = "bank";

        int result1 = comparer.GetHashCode(str1);
        int result2 = comparer.GetHashCode(str2);

        Assert.AreNotEqual(result1, result2);

        Assert.IsFalse(comparer.Equals(str1, str2));
    }

    [TestMethod]
    public void Insensitive_07_capitals()
    {
        var comparer = Agnostic.InsensitiveComparer;
        string str1 = "BANK";
        string str2 = "bank";

        int result1 = comparer.GetHashCode(str1);
        int result2 = comparer.GetHashCode(str2);

        Assert.AreEqual(result1, result2);

        Assert.IsTrue(comparer.Equals(str1, str2));
    }

    [TestMethod]
    public void CaseInsensitive_07_capitals()
    {
        var comparer = Agnostic.CaseInsensitiveComparer;
        string str1 = "BANK";
        string str2 = "bank";

        int result1 = comparer.GetHashCode(str1);
        int result2 = comparer.GetHashCode(str2);

        Assert.AreEqual(result1, result2);

        Assert.IsTrue(comparer.Equals(str1, str2));
    }

    [TestMethod]
    public void Insensitive_08_spacing()
    {
        var comparer = Agnostic.InsensitiveComparer;
        string str1 = "ban k";
        string str2 = "bank ";

        int result1 = comparer.GetHashCode(str1);
        int result2 = comparer.GetHashCode(str2);

        Assert.AreEqual(result1, result2);

        Assert.IsTrue(comparer.Equals(str1, str2));
    }

    [TestMethod]
    public void CaseInsensitive_08_spacing()
    {
        var comparer = Agnostic.CaseInsensitiveComparer;
        string str1 = "ban k";
        string str2 = "bank ";

        int result1 = comparer.GetHashCode(str1);
        int result2 = comparer.GetHashCode(str2);

        Assert.AreNotEqual(result1, result2);

        Assert.IsFalse(comparer.Equals(str1, str2));
    }

    [TestMethod]
    public void Insensitive_09_numbers()
    {
        var comparer = Agnostic.InsensitiveComparer;
        string str1 = "runover1";
        string str2 = "runover2";

        int result1 = comparer.GetHashCode(str1);
        int result2 = comparer.GetHashCode(str2);

        Assert.AreNotEqual(result1, result2);

        Assert.IsFalse(comparer.Equals(str1, str2));
    }

    [TestMethod]
    public void CaseInsensitive_09_numbers()
    {
        var comparer = Agnostic.CaseInsensitiveComparer;
        string str1 = "runover1";
        string str2 = "runover2";

        int result1 = comparer.GetHashCode(str1);
        int result2 = comparer.GetHashCode(str2);

        Assert.AreNotEqual(result1, result2);

        Assert.IsFalse(comparer.Equals(str1, str2));
    }
    #endregion
}
