using Microsoft.VisualStudio.TestTools.UnitTesting;
using NetTally.Models;

namespace NetTally.Tests.Models.Votes;

[TestClass]
public class TaskTests
{
    [ClassInitialize]
    public static void ClassInit(TestContext _)
    {
        TestStartup.ConfigureServices();
    }

    [TestMethod]
    public void Construct_Null_Error()
    {
        var task = VoteTask.Create(null!);
        Assert.AreEqual(VoteTask.None, task);
    }

    [TestMethod]
    public void Construct_Empty_Error()
    {
        var task = VoteTask.Create("");
        Assert.AreEqual(VoteTask.None, task);
    }

    [TestMethod]
    public void Construct_Space_Error()
    {
        var task = VoteTask.Create("     ");
        Assert.AreEqual(VoteTask.None, task);
    }

    [TestMethod]
    public void Construct_SpaceEnd_Trimmed()
    {
        var task = VoteTask.Create("Kinematics     ");
        Assert.AreEqual("Kinematics", task.Name);
    }

    [TestMethod]
    public void Construct_SpaceAround_Trimmed()
    {
        var task = VoteTask.Create("  Kinematics     ");
        Assert.AreEqual("Kinematics", task.Name);
    }

    [TestMethod]
    public void Construct_Unsafe_Cleaned()
    {
        var task = VoteTask.Create("Kinema\u200btics");
        Assert.AreEqual("Kinematics", task.Name);
    }

    [TestMethod]
    public void Construct_UTF_Normal()
    {
        var task = VoteTask.Create("KinematicsΩ°");
        Assert.AreEqual("KinematicsΩ°", task.Name);
    }

    [TestMethod]
    public void AreEqual_Normal()
    {
        var task1 = VoteTask.Create("Kinematics");
        var task2 = VoteTask.Create("Kinematics");

        Assert.IsTrue(VoteTaskComparer.Instance.Equals(task1, task2));
    }

    [TestMethod]
    public void AreEqual_CaseDiff1()
    {
        var task1 = VoteTask.Create("Kinematics");
        var task2 = VoteTask.Create("kinematics");

        Assert.IsTrue(VoteTaskComparer.Instance.Equals(task1, task2));
    }

    [TestMethod]
    public void AreEqual_CaseDiff2()
    {
        var task1 = VoteTask.Create("Kinematics");
        var task2 = VoteTask.Create("KINEMATICS");

        Assert.IsTrue(VoteTaskComparer.Instance.Equals(task1, task2));
    }

    [TestMethod]
    public void AreEqual_CaseDiff3_UTF()
    {
        var task1 = VoteTask.Create("KinematicsΩ");
        var task2 = VoteTask.Create("KINEMATICSΩ");

        Assert.IsTrue(VoteTaskComparer.Instance.Equals(task1, task2));
    }

    [TestMethod]
    public void AreEqual_CaseDiff4_UTF()
    {
        var task1 = VoteTask.Create("Kinematicsω");
        var task2 = VoteTask.Create("KINEMATICSΩ");

        Assert.IsTrue(VoteTaskComparer.Instance.Equals(task1, task2));
    }

    [TestMethod]
    public void CompareWith_Ascend()
    {
        var task1 = VoteTask.Create("Kinematics");
        var task2 = VoteTask.Create("Ubrey");

        Assert.AreEqual(-1, VoteTaskComparer.Instance.Compare(task1, task2));
    }

    [TestMethod]
    public void CompareWith_Descend()
    {
        var task1 = VoteTask.Create("Kinematics");
        var task2 = VoteTask.Create("Aubrey");

        Assert.AreEqual(1, VoteTaskComparer.Instance.Compare(task1, task2));
    }

    [TestMethod]
    public void CompareWith_Equal()
    {
        var task1 = VoteTask.Create("Kinematics");
        var task2 = VoteTask.Create("kinematics");

        Assert.AreEqual(0, VoteTaskComparer.Instance.Compare(task1, task2));
    }
}
