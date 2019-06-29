using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NetTally.Votes.Experiment2;

namespace NTTests.Experiment_2
{
    [TestClass]
    public class Identities
    {
        [TestMethod]
        public void NoDirectConstruction()
        {
            // Cannot construct a new Identity directly, because constructor is protected.
            //var ident = new Identity("", false, 0);
        }

#nullable disable
        [TestMethod]
        [ExpectedException(typeof(ArgumentNullException))]
        public void Create_user_null()
        {
            UserIdent user = new UserIdent(null);
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentNullException))]
        public void Create_plan_null()
        {
            PlanIdent plan = new PlanIdent(null);
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentNullException))]
        public void Create_user_empty()
        {
            UserIdent user = new UserIdent("");
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentNullException))]
        public void Create_plan_empty()
        {
            PlanIdent plan = new PlanIdent("");
        }
#nullable enable

        [TestMethod]
        public void Create_user()
        {
            string username = "Kinematics";
            UserIdent user = new UserIdent(username);

            Assert.IsNotNull(user);
            Assert.IsTrue(user is Identity ident);
            Assert.AreEqual(username, user.Name);
            Assert.IsFalse(user.IsPlan);
            Assert.AreEqual(0, user.Number);
            Assert.AreEqual(user.Name, user.BasicName);
            Assert.IsTrue(user.Matches(username));
            Assert.IsTrue(user.Matches("KINEMATICS"));
            Assert.IsTrue(user.Matches("kinematics"));

            Assert.AreEqual(username, user.ToString());
        }

        [TestMethod]
        public void Create_plan()
        {
            string planname = "Go big!";
            PlanIdent plan = new PlanIdent(planname);

            Assert.IsNotNull(plan);
            Assert.IsTrue(plan is Identity ident);
            Assert.AreEqual(planname, plan.BasicName);
            Assert.IsTrue(plan.IsPlan);
            Assert.AreEqual(0, plan.Number);
            Assert.AreNotEqual(plan.Name, plan.BasicName);
            Assert.IsTrue(plan.Matches(planname));
            Assert.IsTrue(plan.Matches("Go big"));
            Assert.IsTrue(plan.Matches("GO BIG!"));
            Assert.IsTrue(plan.Matches("gobig"));

            Assert.AreEqual(plan.Name, plan.ToString());
        }
    }
}
