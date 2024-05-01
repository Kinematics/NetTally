using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NetTally.Tally.Components;

namespace NetTally.Tests.Forums;
[TestClass]
public class AuthorTests
{
    [TestMethod]
    [ExpectedException(typeof(ArgumentNullException))]
    public void Construct_Null_Error()
    {
        string? name = null;

        Author author = new(name!);
        Assert.AreEqual(name, author.Name);
    }

    [TestMethod]
    [ExpectedException(typeof(ArgumentException))]
    public void Construct_Blank_Error()
    {
        string name = "";

        Author author = new(name);
        Assert.AreEqual(name, author.Name);
    }

    [TestMethod]
    [ExpectedException(typeof(ArgumentException))]
    public void Construct_Whitespace_Error()
    {
        string name = "   ";

        Author author = new(name);
        Assert.AreEqual(name, author.Name);
    }

    [TestMethod]
    public void Construct_Name_Result()
    {
        string name = "Kinematics";

        Author author = new(name);
        Assert.AreEqual(name, author.Name);
    }

    [TestMethod]
    public void Construct_Name_Trimmed()
    {
        string name = "Kinematics  ";
        string trimmedName = "Kinematics";

        Author author = new(name);
        Assert.AreEqual(trimmedName, author.Name);
    }

    [TestMethod]
    public void Construct_UTFName_Result()
    {
        string name = "KinematicsΩ";

        Author author = new(name);
        Assert.AreEqual(name, author.Name);
    }

    [TestMethod]
    public void Construct_Unsafe_Result()
    {
        string name = "Kinema\u200bticsΩ";

        Author author = new(name);
        Assert.AreEqual("KinematicsΩ", author.Name);
    }

    [TestMethod]
    public void Compare_Name_Normal_Same()
    {
        string name1 = "Kinematics";
        Author author1 = new(name1);

        string name2 = "Kinematics";
        Author author2 = new(name2);

        Assert.AreEqual(author1, author2);
    }

    [TestMethod]
    public void Compare_Name_Case_Diff()
    {
        string name1 = "Kinematics";
        Author author1 = new(name1);

        string name2 = "kinematics";
        Author author2 = new(name2);

        Assert.AreNotEqual(author1, author2);
    }
}
