using System;
using System.Collections.Generic;
using System.Threading;
using HintServiceMeow.Core.Utilities;
using HintServiceMeow.Tests.Helpers;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace HintServiceMeow.Tests.Core.Utilities
{
    [TestClass]
    public class UpdateAnalyzerTests
    {
        [TestMethod]
        public void EstimateNextUpdate_WhenNoUpdates_ReturnsMaxValue()
        {
            // Arrange
            UpdateAnalyzer analyzer = new();

            // Act
            DateTime result = analyzer.EstimateNextUpdate();

            // Assert
            Assert.AreEqual(DateTime.MaxValue, result, "Should be DateTime.MaxValue under initial condition");
        }

        [TestMethod]
        public void EstimateNextUpdate_WhenOnlyOneUpdate_ReturnsMaxValue()
        {
            // Arrange
            UpdateAnalyzer analyzer = new();
            analyzer.OnUpdate();

            // Act
            DateTime result = analyzer.EstimateNextUpdate();

            // Assert
            Assert.AreEqual(DateTime.MaxValue, result, "Should be DateTime.MaxValue when having only 1 data");
        }

        [TestMethod]
        public void EstimateNextUpdate_WhenTwoUpdates_ReturnsEstimatedTime()
        {
            // Arrange
            UpdateAnalyzer analyzer = new();
            analyzer.OnUpdate();
            Thread.Sleep(60);
            analyzer.OnUpdate();

            // Act
            DateTime next = analyzer.EstimateNextUpdate();

            // Assert
            Assert.AreNotEqual(DateTime.MaxValue, next, "Should return estimated time when having more than 1 data");
            Assert.IsTrue(next > DateTime.Now, "Estimated time should be later than current time.");
        }

        [TestMethod]
        public void OnUpdate_WhenCalledTooFrequently_IgnoresCall()
        {
            // Arrange
            UpdateAnalyzer analyzer = new();
            analyzer.OnUpdate();
            Thread.Sleep(60);
            analyzer.OnUpdate(); // Valid call

            // Act
            DateTime before = analyzer.EstimateNextUpdate();
            analyzer.OnUpdate(); // Invalid call due to short interval
            DateTime after = analyzer.EstimateNextUpdate();

            // Assert
            Assert.AreEqual(before, after, "Analyzer should ignore the second call since the time elapsed between two action is too short");
        }

        [TestMethod]
        public void OnUpdate_WhenTimestampsOld_RemovesOldEntries()
        {
            // Arrange
            UpdateAnalyzer analyzer = new();
            Queue<DateTime> queue = ReflectionHelper.GetFieldValue<Queue<DateTime>>(analyzer, "updateTimestamps");

            DateTime old = DateTime.Now - TimeSpan.FromSeconds(31); // Old timestamp, should be removed
            queue.Enqueue(old);
            queue.Enqueue(DateTime.Now);
            Thread.Sleep(60);

            // Act
            analyzer.OnUpdate(); // Should remove old timestamps here

            // Assert
            Assert.IsLessThanOrEqualTo(2, queue.Count, "Queue should remove timestamp that is older than 30 seconds");
        }

        [TestMethod]
        public void EstimateNextUpdate_WhenCalledMultipleTimes_ReturnsCachedValue()
        {
            // Arrange
            UpdateAnalyzer analyzer = new();
            analyzer.OnUpdate();
            Thread.Sleep(60);
            analyzer.OnUpdate();

            // Act
            DateTime t1 = analyzer.EstimateNextUpdate();
            DateTime t2 = analyzer.EstimateNextUpdate();

            // Assert
            Assert.AreEqual(t1, t2, "Two value should be identical due to cache");
        }

        [TestMethod]
        public void EstimateNextUpdate_WhenExceptionOccurs_ReturnsMaxValue()
        {
            // Arrange
            UpdateAnalyzer analyzer = new();
            ReflectionHelper.SetFieldValue(analyzer, "updateTimestamps", null); // Trigger NullReferenceException

            // Act
            DateTime result = analyzer.EstimateNextUpdate();

            // Assert
            Assert.AreEqual(DateTime.MaxValue, result, "Should return MaxValue when there's exception");
        }
    }
}
