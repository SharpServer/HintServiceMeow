using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using HintServiceMeow.Core.Models.Arguments;
using HintServiceMeow.Core.Models.HintContent;
using HintServiceMeow.Core.Utilities.Tools;
using HintServiceMeow.Tests.Helpers;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace HintServiceMeow.Tests.HintContent
{
    [TestClass]
    public class AutoContentTests
    {
        private MockLogger _mockLogger;

        [TestInitialize]
        public void Setup()
        {
            _mockLogger = new MockLogger();
            Logger.Instance = _mockLogger;
        }

        private static ContentUpdateArg CreateArg()
        {
            return new ContentUpdateArg(null, null);
        }

        private static void SetNextUpdateTime(AutoContent content, DateTime time)
        {
            ReflectionHelper.SetFieldValue(content, "nextUpdateTime", time);
        }

        private static DateTime GetNextUpdateTime(AutoContent content)
        {
            return ReflectionHelper.GetFieldValue<DateTime>(content, "nextUpdateTime");
        }

        private static TimeSpan GetDefaultUpdateInterval(AutoContent content)
        {
            return ReflectionHelper.GetFieldValue<TimeSpan>(content, "defaultUpdateInterval");
        }

        #region Constructor

        [TestMethod]
        public void Constructor_WithHandler_AutoTextIsSet()
        {
            // Arrange
            AutoContent.TextUpdateHandler handler = ev => "result";

            // Act
            var content = new AutoContent(handler);

            // Assert
            Assert.IsNotNull(content.AutoText);
        }

        [TestMethod]
        public void Constructor_WithNull_AutoTextIsNull()
        {
            // Act
            var content = new AutoContent(null);

            // Assert
            Assert.IsNull(content.AutoText);
        }

        [TestMethod]
        public void Constructor_GetText_ReturnsNull_BeforeTryUpdate()
        {
            // Arrange
            var content = new AutoContent(ev => "text");

            // Act
            string result = content.GetText();

            // Assert
            Assert.IsNull(result);
        }

        #endregion

        #region TryUpdate Normal Flow

        [TestMethod]
        public void TryUpdate_FirstCall_InvokesDelegate()
        {
            // Arrange
            int callCount = 0;
            var content = new AutoContent(ev =>
            {
                Interlocked.Increment(ref callCount);
                return "result";
            });

            // Act
            content.TryUpdate(CreateArg());

            // Assert
            Assert.AreEqual(1, callCount);
            Assert.AreEqual("result", content.GetText());
        }

        [TestMethod]
        public void TryUpdate_DelegateReturnsDifferentValue_TriggersContentUpdated()
        {
            // Arrange
            var content = new AutoContent(ev => "new value");
            int eventCount = 0;
            content.ContentUpdated += () => eventCount++;

            // Act
            content.TryUpdate(CreateArg());

            // Assert - text changes from null to "new value"
            Assert.AreEqual(1, eventCount);
        }

        [TestMethod]
        public void TryUpdate_DelegateReturnsSameValue_DoesNotTriggerContentUpdated()
        {
            // Arrange
            var content = new AutoContent(ev => "same");
            content.TryUpdate(CreateArg()); // Set text to "same"

            // Reset throttle to allow second call
            SetNextUpdateTime(content, DateTime.MinValue);

            int eventCount = 0;
            content.ContentUpdated += () => eventCount++;

            // Act
            content.TryUpdate(CreateArg());

            // Assert
            Assert.AreEqual(0, eventCount, "Same value should not trigger event");
        }

        [TestMethod]
        public void TryUpdate_DelegateReturnsNull_TextBecomesNull()
        {
            // Arrange
            var content = new AutoContent(ev => null);

            // Act
            content.TryUpdate(CreateArg());

            // Assert - text starts as null, delegate returns null, no change
            Assert.IsNull(content.GetText());
        }

        [TestMethod]
        public void TryUpdate_DelegateReturnsEmpty_TextBecomesEmpty()
        {
            // Arrange
            var content = new AutoContent(ev => string.Empty);

            // Act
            content.TryUpdate(CreateArg());

            // Assert
            Assert.AreEqual(string.Empty, content.GetText());
        }

        #endregion

        #region Throttling

        [TestMethod]
        public void TryUpdate_BeforeNextUpdateTime_DoesNotInvokeDelegate()
        {
            // Arrange
            int callCount = 0;
            var content = new AutoContent(ev =>
            {
                Interlocked.Increment(ref callCount);
                return "text";
            });

            // First call should invoke
            content.TryUpdate(CreateArg());
            Assert.AreEqual(1, callCount);

            // Act - second call immediately should be throttled
            content.TryUpdate(CreateArg());

            // Assert
            Assert.AreEqual(1, callCount, "Delegate should not be invoked again within throttle period");
        }

        [TestMethod]
        public void TryUpdate_AfterNextUpdateTime_InvokesDelegateAgain()
        {
            // Arrange
            int callCount = 0;
            var content = new AutoContent(ev =>
            {
                Interlocked.Increment(ref callCount);
                return "text";
            });

            content.TryUpdate(CreateArg());
            Assert.AreEqual(1, callCount);

            // Set nextUpdateTime to the past to bypass throttle
            SetNextUpdateTime(content, DateTime.MinValue);

            // Act
            content.TryUpdate(CreateArg());

            // Assert
            Assert.AreEqual(2, callCount);
        }

        [TestMethod]
        public void TryUpdate_DelegateModifiesNextUpdateDelay_Respected()
        {
            // Arrange
            var content = new AutoContent(ev =>
            {
                ev.NextUpdateDelay = TimeSpan.FromSeconds(10);
                return "text";
            });

            DateTime beforeUpdate = DateTime.Now;

            // Act
            content.TryUpdate(CreateArg());

            // Assert - nextUpdateTime should be approximately now + 10 seconds
            DateTime nextUpdateTime = GetNextUpdateTime(content);
            TimeSpan diff = nextUpdateTime - beforeUpdate;
            Assert.IsTrue(diff.TotalSeconds >= 9.5 && diff.TotalSeconds <= 11,
                $"NextUpdateTime should be ~10s from now, but was {diff.TotalSeconds}s");
        }

        [TestMethod]
        public void TryUpdate_DelegateModifiesDefaultUpdateDelay_PersistsToSubsequentCalls()
        {
            // Arrange
            TimeSpan? receivedDelay = null;
            var content = new AutoContent(ev =>
            {
                ev.DefaultUpdateDelay = TimeSpan.FromSeconds(5);
                return "text1";
            });

            // First call - sets DefaultUpdateDelay to 5s
            content.TryUpdate(CreateArg());

            // Change handler to capture the DefaultUpdateDelay it receives
            content.AutoText = ev =>
            {
                receivedDelay = ev.DefaultUpdateDelay;
                return "text2";
            };

            // Act - AutoText setter resets nextUpdateTime, so second call can proceed
            content.TryUpdate(CreateArg());

            // Assert
            Assert.AreEqual(TimeSpan.FromSeconds(5), receivedDelay,
                "DefaultUpdateDelay should persist from previous call");
        }

        [TestMethod]
        public void TryUpdate_DefaultUpdateInterval_Is100ms()
        {
            // Arrange
            var content = new AutoContent(ev => "text");

            // Act
            TimeSpan defaultTime = GetDefaultUpdateInterval(content);

            // Assert
            Assert.AreEqual(TimeSpan.FromSeconds(0.1), defaultTime);
        }

        #endregion

        #region AutoText Setter

        [TestMethod]
        public void SetAutoText_ResetsNextUpdateTime()
        {
            // Arrange
            int callCount = 0;
            var content = new AutoContent(ev =>
            {
                Interlocked.Increment(ref callCount);
                return "text";
            });

            // First call
            content.TryUpdate(CreateArg());
            Assert.AreEqual(1, callCount);

            // Immediate second call should be throttled
            content.TryUpdate(CreateArg());
            Assert.AreEqual(1, callCount);

            // Act - set AutoText resets throttle
            content.AutoText = ev =>
            {
                Interlocked.Increment(ref callCount);
                return "new text";
            };
            content.TryUpdate(CreateArg());

            // Assert
            Assert.AreEqual(2, callCount, "Delegate should be invoked after AutoText setter resets throttle");
        }

        [TestMethod]
        public void SetAutoText_Null_TryUpdate_TextBecomesNull()
        {
            // Arrange
            var content = new AutoContent(ev => "initial");
            content.TryUpdate(CreateArg());
            Assert.AreEqual("initial", content.GetText());

            // Act
            content.AutoText = null;
            content.TryUpdate(CreateArg());

            // Assert - null delegate returns null, text changes from "initial" to null
            Assert.IsNull(content.GetText());
        }

        [TestMethod]
        public void SetAutoText_MultipleTimesRapidly_EachResetsThrottle()
        {
            // Arrange
            int callCount = 0;
            var content = new AutoContent(ev =>
            {
                Interlocked.Increment(ref callCount);
                return "text";
            });

            // Act - set AutoText multiple times, each should allow TryUpdate
            for (int i = 0; i < 5; i++)
            {
                content.AutoText = ev =>
                {
                    Interlocked.Increment(ref callCount);
                    return $"text{i}";
                };
                content.TryUpdate(CreateArg());
            }

            // Assert - each setter reset should allow a TryUpdate invocation
            Assert.AreEqual(5, callCount);
        }

        #endregion

        #region Exception Handling

        [TestMethod]
        public void TryUpdate_DelegateThrows_TextBecomesEmptyString()
        {
            // Arrange
            var content = new AutoContent(ev => throw new InvalidOperationException("test error"));

            // Act
            content.TryUpdate(CreateArg());

            // Assert
            Assert.AreEqual(string.Empty, content.GetText());
        }

        [TestMethod]
        public void TryUpdate_DelegateThrows_ExceptionLoggedToLogger()
        {
            // Arrange
            var content = new AutoContent(ev => throw new InvalidOperationException("test error"));

            // Act
            content.TryUpdate(CreateArg());

            // Assert
            Assert.AreEqual(1, _mockLogger.ErrorMessages.Count);
            Assert.IsInstanceOfType(_mockLogger.ErrorMessages[0], typeof(InvalidOperationException));
        }

        [TestMethod]
        public void TryUpdate_DelegateThrows_SubsequentTryUpdateStillWorks()
        {
            // Arrange - start with throwing handler
            var content = new AutoContent(ev => throw new InvalidOperationException("error"));
            content.TryUpdate(CreateArg());
            Assert.AreEqual(string.Empty, content.GetText());

            // Act - switch to a working handler (setter resets throttle)
            content.AutoText = ev => "recovered";
            content.TryUpdate(CreateArg());

            // Assert
            Assert.AreEqual("recovered", content.GetText());
        }

        /// <summary>
        /// [BUG DETECTION] When the delegate throws, the catch block sets text = "" but does NOT
        /// update nextUpdateTime. This means subsequent TryUpdate calls will immediately retry
        /// the failing delegate without any throttle protection.
        /// Expected: After exception, nextUpdateTime should be updated to provide throttle.
        /// Actual: nextUpdateTime is not updated, causing immediate retries.
        /// </summary>
        [TestMethod]
        public void TryUpdate_DelegateThrows_NextUpdateTimeNotUpdated_BugDetection()
        {
            // Arrange
            int callCount = 0;
            var content = new AutoContent(ev =>
            {
                Interlocked.Increment(ref callCount);
                throw new InvalidOperationException("repeated error");
            });

            // Act - call TryUpdate multiple times rapidly
            for (int i = 0; i < 5; i++)
            {
                content.TryUpdate(CreateArg());
            }

            // Assert
            // BUG: delegate is called all 5 times because nextUpdateTime is never updated
            // after an exception. Expected behavior: only 1 call (throttled after first).
            // If this test PASSES (callCount == 1), the bug has been fixed.
            Assert.AreEqual(1, callCount,
                "[BUG] Delegate called multiple times without throttle after exception. " +
                "nextUpdateTime is not updated in catch block, causing unthrottled retries.");
        }

        /// <summary>
        /// [BUG DETECTION] When the delegate throws, text changes from its current value to "".
        /// However, OnUpdated() is NOT called in the catch block, so ContentUpdated event
        /// is never fired despite the text value changing.
        /// Expected: ContentUpdated should fire when text changes due to exception.
        /// Actual: ContentUpdated is not fired.
        /// </summary>
        [TestMethod]
        public void TryUpdate_DelegateThrows_ContentUpdatedNotFired_BugDetection()
        {
            // Arrange - first set text to a non-empty value
            var content = new AutoContent(ev => "something");
            content.TryUpdate(CreateArg());
            Assert.AreEqual("something", content.GetText());

            // Now switch to a throwing handler
            content.AutoText = ev => throw new InvalidOperationException("error");

            int eventCount = 0;
            content.ContentUpdated += () => eventCount++;

            // Act
            content.TryUpdate(CreateArg());

            // Assert
            // Text changed from "something" to "" but OnUpdated was not called
            Assert.AreEqual(string.Empty, content.GetText());
            // BUG: eventCount is 0 because catch block doesn't call OnUpdated()
            // If this test PASSES (eventCount == 1), the bug has been fixed.
            Assert.AreEqual(1, eventCount,
                "[BUG] ContentUpdated not fired when text changes due to exception. " +
                "The catch block sets text = \"\" but does not call OnUpdated().");
        }

        #endregion

        #region ContentUpdated Event

        [TestMethod]
        public void ContentUpdated_Subscribed_FiresOnTextChange()
        {
            // Arrange
            var content = new AutoContent(ev => "text");
            int eventCount = 0;
            content.ContentUpdated += () => eventCount++;

            // Act
            content.TryUpdate(CreateArg());

            // Assert
            Assert.AreEqual(1, eventCount);
        }

        [TestMethod]
        public void ContentUpdated_HandlerThrows_AutoContentContinuesWorking()
        {
            // Arrange
            var content = new AutoContent(ev => "text1");
            content.ContentUpdated += () => throw new InvalidOperationException("handler error");

            // Act - first call, handler throws but is caught by OnUpdated
            content.TryUpdate(CreateArg());
            Assert.AreEqual("text1", content.GetText());

            // Switch handler and bypass throttle
            content.AutoText = ev => "text2";
            content.TryUpdate(CreateArg());

            // Assert - AutoContent continues to work
            Assert.AreEqual("text2", content.GetText());
        }

        [TestMethod]
        public void ContentUpdated_MultipleSubscribers_AllNotified()
        {
            // Arrange
            var content = new AutoContent(ev => "text");
            int count1 = 0, count2 = 0, count3 = 0;
            content.ContentUpdated += () => count1++;
            content.ContentUpdated += () => count2++;
            content.ContentUpdated += () => count3++;

            // Act
            content.TryUpdate(CreateArg());

            // Assert
            Assert.AreEqual(1, count1);
            Assert.AreEqual(1, count2);
            Assert.AreEqual(1, count3);
        }

        #endregion

        #region Concurrency

        [TestMethod]
        [Timeout(10000)]
        public async Task ConcurrentTryUpdate_DoesNotThrow()
        {
            // Arrange
            int callCount = 0;
            var content = new AutoContent(ev =>
            {
                Interlocked.Increment(ref callCount);
                return "text";
            });

            // Act - multiple threads call TryUpdate concurrently
            var tasks = Enumerable.Range(0, 50).Select(_ => Task.Run(() =>
            {
                SetNextUpdateTime(content, DateTime.MinValue);
                content.TryUpdate(CreateArg());
            }));

            // Assert - should not throw
            await Task.WhenAll(tasks);
            Assert.IsTrue(callCount > 0);
        }

        [TestMethod]
        [Timeout(10000)]
        public async Task ConcurrentGetText_WhileTryUpdate_DoesNotThrow()
        {
            // Arrange
            var content = new AutoContent(ev => "text" + DateTime.Now.Ticks);
            var results = new ConcurrentBag<string>();
            var cts = new CancellationTokenSource(TimeSpan.FromSeconds(2));

            // Act - one thread does TryUpdate, others read GetText
            var updateTask = Task.Run(() =>
            {
                while (!cts.Token.IsCancellationRequested)
                {
                    SetNextUpdateTime(content, DateTime.MinValue);
                    content.TryUpdate(CreateArg());
                }
            });

            var readTasks = Enumerable.Range(0, 10).Select(_ => Task.Run(() =>
            {
                while (!cts.Token.IsCancellationRequested)
                {
                    results.Add(content.GetText());
                }
            }));

            // Assert - should not throw
            await Task.WhenAll(readTasks.Append(updateTask));
        }

        #endregion
    }
}
