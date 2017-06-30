using System;
using System.Runtime.CompilerServices;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NetTally;
using NetTally.SystemInfo;

namespace TallyUnitTest.General
{
    [TestClass]
    public class LoggerTest
    {
        [TestMethod]
        public void LoggerExists()
        {
            Logger.LoggingLevel = LoggingLevel.Error;
            Assert.IsInstanceOfType(Logger.Clock, typeof(IClock));
            Assert.AreEqual(LoggingLevel.Error, Logger.LoggingLevel);
        }

        [TestMethod]
        public void LogError()
        {
            IClock clock = new StaticClock(DateTime.Now);

            Logger.LogUsing(new NullLogger());
            Logger.Clock = clock;
            Logger.LoggingLevel = LoggingLevel.Error;

            Assert.IsTrue(Logger.Error("Logger test"));
            Assert.IsFalse(Logger.Warning("Logger test"));
            Assert.IsFalse(Logger.Info("Logger test"));
        }

        [TestMethod]
        public void LogWarning()
        {
            IClock clock = new StaticClock(DateTime.Now);

            Logger.LogUsing(new NullLogger());
            Logger.Clock = clock;
            Logger.LoggingLevel = LoggingLevel.Warning;

            Assert.IsTrue(Logger.Error("Logger test"));
            Assert.IsTrue(Logger.Warning("Logger test"));
            Assert.IsFalse(Logger.Info("Logger test"));
        }

        [TestMethod]
        public void LogInfo()
        {
            IClock clock = new StaticClock(DateTime.Now);

            Logger.LogUsing(new NullLogger());
            Logger.Clock = clock;
            Logger.LoggingLevel = LoggingLevel.Info;

            Assert.IsTrue(Logger.Error("Logger test"));
            Assert.IsTrue(Logger.Warning("Logger test"));
            Assert.IsTrue(Logger.Info("Logger test"));
        }

        [TestMethod]
        public void LogNone()
        {
            IClock clock = new StaticClock(DateTime.Now);

            Logger.LogUsing(new NullLogger());
            Logger.Clock = clock;
            Logger.LoggingLevel = LoggingLevel.None;

            Assert.IsFalse(Logger.Error("Logger test"));
            Assert.IsFalse(Logger.Warning("Logger test"));
            Assert.IsFalse(Logger.Info("Logger test"));
        }

        [TestMethod]
        public void LastKnownLogLocation()
        {
            IClock clock = new StaticClock(DateTime.Now);
            Logger.Clock = clock;
            Logger.LoggingLevel = LoggingLevel.Error;

            Assert.IsTrue(Logger.Error("Logger test"));
            Assert.AreEqual("Nowhere", Logger.LastLogLocation);
        }

        [TestMethod]
        public void LocalILogger()
        {
            IClock clock = new StaticClock(DateTime.Now);
            Logger.Clock = clock;
            Logger.LoggingLevel = LoggingLevel.Error;
            var testLogger = new TestLogger();
            Logger.LogUsing(testLogger);

            string message = "Logger test";

            Assert.IsTrue(Logger.Error(message));
            Assert.AreEqual("Local", Logger.LastLogLocation);
            Assert.AreEqual(message, testLogger.LogMessage);
        }
    }

    internal class TestLogger : ILogger
    {
        public string LogMessage { get; set; }

        public bool Log(string message, Exception exception, IClock clock, [CallerMemberName] string callingMethod = null)
        {
            LogMessage = message;
            return true;
        }

        public string LastLogLocation => "Local";
    }

}
