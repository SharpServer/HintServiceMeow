using System;
using System.Threading;
using System.Threading.Tasks;
using HintServiceMeow.Core.Utilities;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace HintServiceMeow.Tests
{
    [TestClass]
    public class PeriodicRunnerTests
    {
        private static readonly TimeSpan ShortInterval = TimeSpan.FromMilliseconds(30);

        private static TimeSpan GetLength(TimeSpan interval, int times)
        {
            return TimeSpan.FromTicks(interval.Ticks * times);
        }

        [TestMethod]
        public async Task Start_RunsPeriodically()
        {
            int count = 0;

            using PeriodicRunner runner = PeriodicRunner.Start(
                () =>
                {
                    Interlocked.Increment(ref count);
                    return Task.CompletedTask;
                },
                ShortInterval,
                runImmediately: false);
            await Task.Delay(GetLength(ShortInterval, 5));
            Assert.IsGreaterThanOrEqualTo(4, count);
        }

        [TestMethod]
        public async Task Start_WithRunImmediately_InvokesAtOnce()
        {
            int count = 0;

            using PeriodicRunner runner = PeriodicRunner.Start(
                () =>
                {
                    Interlocked.Increment(ref count);
                    return Task.CompletedTask;
                },
                ShortInterval,
                runImmediately: true);
            await Task.Delay(TimeSpan.FromMilliseconds(10));
            Assert.AreEqual(1, count);
        }

        [TestMethod]
        public async Task PauseAndResume_Works()
        {
            int count = 0;

            using PeriodicRunner runner = PeriodicRunner.Start(
                () =>
                {
                    Interlocked.Increment(ref count);
                    return Task.CompletedTask;
                },
                ShortInterval);
            await Task.Delay(GetLength(ShortInterval, 3));

            runner.Pause();
            int before = count;
            await Task.Delay(GetLength(ShortInterval, 4));
            Assert.AreEqual(before, count);

            runner.Resume();
            await Task.Delay(GetLength(ShortInterval, 3));
            Assert.IsGreaterThan(before, count);
        }

        [TestMethod]
        public async Task Dispose_StopsFurtherInvocations()
        {
            int count = 0;
            PeriodicRunner runner = PeriodicRunner.Start(
                () =>
                {
                    Interlocked.Increment(ref count);
                    return Task.CompletedTask;
                },
                ShortInterval);

            await Task.Delay(GetLength(ShortInterval, 3));
            int beforeDispose = count;

            runner.Dispose();
            await Task.Delay(GetLength(ShortInterval, 4));
            Assert.AreEqual(beforeDispose, count);

            await runner.CurrentTask;
        }

        [TestMethod]
        public async Task Callback_Exception_IsSwallowedAndContinues()
        {
            int count = 0;

            using PeriodicRunner runner = PeriodicRunner.Start(
                () =>
                {
                    int cur = Interlocked.Increment(ref count);
                    if (cur == 1)
                        throw new InvalidOperationException("Test");
                    return Task.CompletedTask;
                },
                ShortInterval);
            await Task.Delay(GetLength(ShortInterval, 4));
            Assert.IsGreaterThanOrEqualTo(3, count);
        }

        [TestMethod]
        public void NegativeInterval_Throws()
        {
            Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => PeriodicRunner.Start(() => Task.CompletedTask,
                                 TimeSpan.FromMilliseconds(-1)));
        }
    }
}
