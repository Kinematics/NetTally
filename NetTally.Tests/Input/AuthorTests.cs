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
    }

    [TestMethod]
    [ExpectedException(typeof(ArgumentException))]
    public void Construct_Blank_Error()
    {
        string name = "";

        Author author = new(name);
    }

    [TestMethod]
    [ExpectedException(typeof(ArgumentException))]
    public void Construct_Whitespace_Error()
    {
        string name = "   ";

        Author author = new(name);
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

        Author author = new(name);
        Assert.AreEqual(name.Trim(), author.Name);
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
}
