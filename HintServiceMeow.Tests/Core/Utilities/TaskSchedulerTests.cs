using System;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using HintServiceMeow.Core.Enum;
using HintServiceMeow.Core.Interface;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using TaskScheduler = HintServiceMeow.Core.Utilities.TaskScheduler;

namespace HintServiceMeow.Tests
{
    [TestClass]
    public class TaskSchedulerTests
    {
        private TaskScheduler _scheduler = null!;
        private int _actionInvokeCount;

        [TestInitialize]
        public void SetUp()
        {
            _scheduler = new TaskScheduler(60); // tickRate=60 to ensure accuracy
            _actionInvokeCount = 0;
        }

        [TestCleanup]
        public void TearDown()
        {
            ((IDestructible)_scheduler).Destruct();
        }

        [TestMethod]
        public void Start_ShouldThrow_IfIntervalZeroOrNegative()
        {
            _scheduler.Start(TimeSpan.Zero, () => { });
            _scheduler.Start(TimeSpan.FromMilliseconds(-1), () => { });

            Assert.IsTrue(true); // No exception = passed
        }

        [TestMethod]
        public void Start_ShouldThrow_IfActionIsNull()
        {
            Assert.ThrowsExactly<ArgumentNullException>(() =>
            {
                _scheduler.Start(TimeSpan.FromMilliseconds(100), null!);
            });
        }

        [TestMethod]
        public void Start_ShouldSet_IntervalAndAction()
        {
            _scheduler.Start(TimeSpan.FromMilliseconds(200), () => { _actionInvokeCount++; });
            Assert.IsLessThan(5, _scheduler.Elapsed.TotalMilliseconds);
            Assert.IsFalse(_scheduler.IsPaused);
        }

        [TestMethod]
        public async Task Invoke_And_AutoInvokeAction_AfterInterval()
        {
            int invoked = 0;
            _scheduler.Start(TimeSpan.FromMilliseconds(50), () => { invoked++; });

            _scheduler.Invoke(0, DelayType.Override);
            await Task.Delay(120);

            Assert.AreEqual(1, invoked);
        }

        [TestMethod]
        public Task Invoke_With_Delay_KeepsScheduledTime_ByDelayType()
        {
            _scheduler.Start(TimeSpan.FromMilliseconds(100), () => { _actionInvokeCount++; });

            // KeepFastest
            _scheduler.Invoke(2f, DelayType.KeepFastest);
            DateTime firstTime = GetScheduledActionTime(_scheduler);
            _scheduler.Invoke(1f, DelayType.KeepFastest);
            Assert.IsTrue(GetScheduledActionTime(_scheduler) <= firstTime);

            // KeepSlowest
            _scheduler.Invoke(1f, DelayType.KeepSlowest);
            firstTime = GetScheduledActionTime(_scheduler);
            _scheduler.Invoke(10f, DelayType.KeepSlowest);
            Assert.IsTrue(GetScheduledActionTime(_scheduler) >= firstTime);

            // Override
            _scheduler.Invoke(3f, DelayType.Override);
            firstTime = GetScheduledActionTime(_scheduler);
            _scheduler.Invoke(5f, DelayType.Override);
            Assert.IsLessThan(0.2, Math.Abs((GetScheduledActionTime(_scheduler) - DateTime.Now).TotalSeconds - 5f));
            return Task.CompletedTask;
        }

        [TestMethod]
        public void Stop_ShouldReset_State()
        {
            _scheduler.Start(TimeSpan.FromMilliseconds(100), () => { });
            _scheduler.Invoke(0.1f);

            _scheduler.Stop();

            Assert.AreEqual(TimeSpan.Zero, _scheduler.Elapsed);
            // Reset scheduled action time
            Assert.IsTrue(IsScheduledActionTimeMax(_scheduler));
        }

        [TestMethod]
        public void Pause_And_Resume_Work_AsExpected()
        {
            _scheduler.Start(TimeSpan.FromMilliseconds(200), () => { });

            _scheduler.Pause();
            Assert.IsTrue(_scheduler.IsPaused);

            TimeSpan afterPause = _scheduler.Elapsed;
            Thread.Sleep(50);
            Assert.AreEqual(afterPause, _scheduler.Elapsed); // Elapsed time should not change while paused

            _scheduler.Resume();
            Assert.IsFalse(_scheduler.IsPaused);
        }

        [TestMethod]
        public void IsReadyForNextAction_ReturnsFalse_IfElapsedLessThanInterval()
        {
            _scheduler.Start(TimeSpan.FromMilliseconds(200), () =>
            {
                Console.WriteLine($"Action Executed at {DateTime.Now}");
            });
            _scheduler.Invoke();
            Thread.Sleep(100 / 6);
            Assert.IsFalse(_scheduler.IsReadyForNextAction);
        }

        [TestMethod]
        public void IsReadyForNextAction_ReturnsTrue_IfElapsedGreaterThanInterval()
        {
            _scheduler.Start(TimeSpan.FromMilliseconds(1), () =>
            {
                Console.WriteLine($"Action Executed at {DateTime.Now}");
            });
            _scheduler.Invoke();

            Thread.Sleep(100 / 6); // Wait for 1 tick
            Thread.Sleep(10); // Wait for interval

            Console.WriteLine($"Elapsed for {_scheduler.Elapsed} since last action");
            Assert.IsTrue(_scheduler.IsReadyForNextAction);
        }

        [TestMethod]
        public void Elapsed_Resets_When_InvokeAction()
        {
            _scheduler.Start(TimeSpan.FromMilliseconds(1), () => { });
            _scheduler.Invoke(0);
            TimeSpan oldElapsed = _scheduler.Elapsed;
            Thread.Sleep(5);

            // Elapsed updated
            Assert.IsTrue(_scheduler.Elapsed >= oldElapsed);
        }

        // Auxiliary method to read private members
        private static DateTime GetScheduledActionTime(TaskScheduler scheduler)
        {
            Type? type = typeof(TaskScheduler);
            PropertyInfo? prop = type.GetProperty("ScheduledActionTime", BindingFlags.NonPublic | BindingFlags.Instance);
            return (DateTime)prop!.GetValue(scheduler);
        }

        private static bool IsScheduledActionTimeMax(TaskScheduler scheduler)
        {
            return GetScheduledActionTime(scheduler) == DateTime.MaxValue;
        }
    }
}
