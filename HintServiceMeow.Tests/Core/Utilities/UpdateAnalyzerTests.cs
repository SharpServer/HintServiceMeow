using HintServiceMeow.Core.Utilities;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Threading;

namespace HintServiceMeow.Tests
{
    [TestClass]
    public class UpdateAnalyzerTests
    {
        [TestMethod]
        public void EstimateNextUpdate_Initially_MaxValue()
        {
            UpdateAnalyzer analyzer = new();
            Assert.AreEqual(DateTime.MaxValue, analyzer.EstimateNextUpdate(), "Should be DateTime.MaxValue under initial condition");
        }

        [TestMethod]
        public void EstimateNextUpdate_AfterFirstUpdate_StillMaxValue()
        {
            UpdateAnalyzer analyzer = new();
            analyzer.OnUpdate();
            Assert.AreEqual(DateTime.MaxValue, analyzer.EstimateNextUpdate(), "Should be DateTime.MaxValue when having only 1 data");
        }

        [TestMethod]
        public void EstimateNextUpdate_AfterTwoUpdates_ShouldReturnTime()
        {
            UpdateAnalyzer analyzer = new();

            analyzer.OnUpdate();
            Thread.Sleep(60);
            analyzer.OnUpdate();

            DateTime next = analyzer.EstimateNextUpdate();
            Assert.AreNotEqual(DateTime.MaxValue, next, "Should return estimated time when having more than 1 data");
            Assert.IsTrue(next > DateTime.Now, "Estimated time should be later than current time.");
        }

        [TestMethod]
        public void OnUpdate_TooFrequent_ShouldIgnore()
        {
            UpdateAnalyzer analyzer = new();

            analyzer.OnUpdate();
            Thread.Sleep(60);
            analyzer.OnUpdate(); // Valid call
            DateTime before = analyzer.EstimateNextUpdate();
            analyzer.OnUpdate(); // Invalid call due to short interval
            DateTime after = analyzer.EstimateNextUpdate();

            Assert.AreEqual(before, after, "Analyzer should ignore the second call since the time elapsed between two action is too short");
        }

        [TestMethod]
        public void OnUpdate_ShouldRemoveOldTimestamps()
        {
            UpdateAnalyzer analyzer = new();

            // Inject data to simulate old timestamps
            FieldInfo? field = typeof(UpdateAnalyzer).GetField("updateTimestamps", BindingFlags.NonPublic | BindingFlags.Instance);
            Queue<DateTime>? queue = (Queue<DateTime>)field!.GetValue(analyzer);

            DateTime old = DateTime.Now - TimeSpan.FromSeconds(31); // Old timestamp, should be removed during next OnUpdate call
            queue.Enqueue(old);
            queue.Enqueue(DateTime.Now);

            Thread.Sleep(60);
            analyzer.OnUpdate();// Should remove old timestamps here

            Assert.IsLessThanOrEqualTo(2, queue.Count, "Queue should remove timestamp that is older than 30 seconds");
        }

        [TestMethod]
        public void EstimateNextUpdate_CachesResult()
        {
            UpdateAnalyzer analyzer = new();
            analyzer.OnUpdate();
            Thread.Sleep(60);
            analyzer.OnUpdate();

            DateTime t1 = analyzer.EstimateNextUpdate();
            DateTime t2 = analyzer.EstimateNextUpdate();
            Assert.AreEqual(t1, t2, "Two value should be identical due to cache");
        }

        [TestMethod]
        public void EstimateNextUpdate_Exception_ShouldReturnMaxValue()
        {
            UpdateAnalyzer analyzer = new();
            FieldInfo? field = typeof(UpdateAnalyzer).GetField("updateTimestamps", BindingFlags.NonPublic | BindingFlags.Instance);
            field!.SetValue(analyzer, null); // use this to trigger NullReferenceException

            Assert.AreEqual(DateTime.MaxValue, analyzer.EstimateNextUpdate(), "Should return MaxValue when there's exception");
        }
    }
}
