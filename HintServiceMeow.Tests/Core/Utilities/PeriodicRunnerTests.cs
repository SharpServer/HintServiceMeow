using System;
using System.Threading;
using System.Threading.Tasks;
using HintServiceMeow.Core.Utilities;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace HintServiceMeow.Tests.Core.Utilities
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
        public async Task Start_WhenCalled_RunsPeriodically()
        {
            // Arrange
            int count = 0;

            // Act
            using PeriodicRunner runner = PeriodicRunner.Start(
                () =>
                {
                    Interlocked.Increment(ref count);
                    return Task.CompletedTask;
                },
                ShortInterval,
                runImmediately: false);
            await Task.Delay(GetLength(ShortInterval, 5));

            // Assert
            Assert.IsGreaterThanOrEqualTo(4, count);
        }

        [TestMethod]
        public async Task Start_WithRunImmediately_InvokesAtOnce()
        {
            // Arrange
            int count = 0;

            // Act
            using PeriodicRunner runner = PeriodicRunner.Start(
                () =>
                {
                    Interlocked.Increment(ref count);
                    return Task.CompletedTask;
                },
                ShortInterval,
                runImmediately: true);
            await Task.Delay(TimeSpan.FromMilliseconds(10));

            // Assert
            Assert.AreEqual(1, count);
        }

        [TestMethod]
        public async Task PauseAndResume_WhenRunning_StopsAndResumesInvocations()
        {
            // Arrange
            int count = 0;
            using PeriodicRunner runner = PeriodicRunner.Start(
                () =>
                {
                    Interlocked.Increment(ref count);
                    return Task.CompletedTask;
                },
                ShortInterval);
            await Task.Delay(GetLength(ShortInterval, 3));

            // Act - pause and verify no further increments
            runner.Pause();
            int before = count;
            await Task.Delay(GetLength(ShortInterval, 4));

            // Assert - paused: count unchanged
            Assert.AreEqual(before, count);

            // Act - resume and verify increments continue
            runner.Resume();
            await Task.Delay(GetLength(ShortInterval, 3));

            // Assert - resumed: count increased
            Assert.IsGreaterThan(before, count);
        }

        [TestMethod]
        public async Task Dispose_WhenCalled_StopsFurtherInvocations()
        {
            // Arrange
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

            // Act
            runner.Dispose();
            await Task.Delay(GetLength(ShortInterval, 4));

            // Assert
            Assert.AreEqual(beforeDispose, count);
            await runner.CurrentTask;
        }

        [TestMethod]
        public async Task Callback_WhenThrows_IsSwallowedAndContinues()
        {
            // Arrange
            int count = 0;

            // Act
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

            // Assert
            Assert.IsGreaterThanOrEqualTo(3, count);
        }

        [TestMethod]
        public void Start_WhenIntervalIsNegative_ThrowsArgumentOutOfRangeException()
        {
            // Arrange & Act & Assert
            Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => PeriodicRunner.Start(() => Task.CompletedTask,
                             TimeSpan.FromMilliseconds(-1)));
        }
    }
}
