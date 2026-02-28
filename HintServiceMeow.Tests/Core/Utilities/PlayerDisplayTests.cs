using System;
using System.Linq;
using System.Reflection;
using HintServiceMeow.Core.Enum;
using HintServiceMeow.Core.Interface;
using HintServiceMeow.Core.Models.Hints;
using HintServiceMeow.Core.Utilities;
using HintServiceMeow.Tests.Core.Utilities.TestDoubles;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace HintServiceMeow.Tests.Core.Utilities
{
    /// <summary>
    /// Tests for <see cref="PlayerDisplay"/> that focus on public behavior
    /// while controlling runtime dependencies through test doubles.
    /// </summary>
    [TestClass]
    public class PlayerDisplayTests
    {
        private TestTaskScheduler scheduler = null!;
        private TestCompatibilityAdaptor adaptor = null!;
        private TestHintParser parser = null!;
        private TestPlayerContext context = null!;
        private TestCoroutineRunner coroutineRunner = null!;
        private PlayerDisplay display = null!;

        [TestInitialize]
        public void SetUp()
        {
            // Build a fully controlled test environment so each test is deterministic.
            scheduler = new TestTaskScheduler();
            adaptor = new TestCompatibilityAdaptor();
            parser = new TestHintParser();
            context = new TestPlayerContext { IsStillValid = false };
            coroutineRunner = new TestCoroutineRunner();

            display = new PlayerDisplay(context, updateScheduler: scheduler, adaptor: adaptor, hintParser: parser, coroutineRunner: coroutineRunner);
        }

        [TestCleanup]
        public void TearDown()
        {
            // Ensure internal resources are released between tests.
            ((IDestructible)display).Destruct();
        }

        [TestMethod]
        // Verify constructor rejects null player context input.
        public void Constructor_ShouldThrow_WhenPlayerContextNull()
        {
            Assert.ThrowsExactly<ArgumentNullException>(() =>
            {
                _ = new PlayerDisplay(null!);
            });
        }

        [TestMethod]
        // Verify constructor starts the internal update coroutine using injected runner.
        public void Constructor_ShouldStartCoroutine_WhenRunnerInjected()
        {
            Assert.AreEqual(1, coroutineRunner.StartedRoutines.Count);
            Assert.IsTrue(coroutineRunner.LastCoroutine.IsRunning);
            Assert.IsFalse(coroutineRunner.LastCoroutine.IsKilled);
        }

        [TestMethod]
        // Verify destruct stops the started coroutine.
        public void Destruct_ShouldKillStartedCoroutine()
        {
            ((IDestructible)display).Destruct();

            Assert.IsTrue(coroutineRunner.LastCoroutine.IsKilled);
            Assert.IsFalse(coroutineRunner.LastCoroutine.IsRunning);
        }

        [TestMethod]
        // Verify ForceUpdate schedules normal and immediate delays correctly.
        public void ForceUpdate_ShouldScheduleExpectedDelay()
        {
            // Default force update should use normal fast path delay.
            display.ForceUpdate();

            // Fast update should request immediate invocation.
            display.ForceUpdate(useFastUpdate: true);

            Console.WriteLine($"Scheduled invokes: {scheduler.Invokes.Count}");
            foreach (var invoke in scheduler.Invokes)
            {
                Console.WriteLine($"Delay: {invoke.Delay}, Strategy: {invoke.DelayType}");
            }

            Assert.AreEqual(2, scheduler.Invokes.Count);
            Assert.IsTrue(scheduler.Invokes[0].Delay < 0.3f);
            Assert.IsTrue(scheduler.Invokes[1].Delay <= 0);
        }

        [TestMethod]
        // Verify adding/removing hints by Guid and Id updates collection state as expected.
        public void AddAndRemoveHint_ShouldManageHintCollectionByGuidAndId()
        {
            Hint first = new() { Id = "hp" };
            Hint second = new() { Id = "hp" };

            display.AddHint(first, second);

            Assert.IsTrue(display.HasHint("hp"));
            Assert.IsTrue(display.HasHint(first.Guid));
            Assert.AreEqual(2, display.GetHints("hp").Count());

            // Remove by Guid should only remove the exact target hint.
            display.RemoveHint(first.Guid);
            Assert.IsFalse(display.HasHint(first.Guid));
            Assert.IsTrue(display.HasHint(second.Guid));

            // Remove by Id should remove all hints with that id in the same caller group.
            display.RemoveHint("hp");
            Assert.IsFalse(display.HasHint(second.Guid));
            Assert.AreEqual(0, display.GetHints().Count());
        }

        [TestMethod]
        // Verify TryGetHint APIs are consistent before and after ClearHint.
        public void TryGetHintAndClearHint_ShouldReturnConsistentResult()
        {
            Hint hint = new() { Id = "quest" };
            display.AddHint(hint);

            Assert.IsTrue(display.TryGetHint("quest", out AbstractHint stringHint));
            Assert.AreSame(hint, stringHint);

            Assert.IsTrue(display.TryGetHint(hint.Guid, out AbstractHint guidHint));
            Assert.AreSame(hint, guidHint);

            // Clear all hints from caller group and verify both retrieval APIs reflect it.
            display.ClearHint();
            Assert.IsFalse(display.TryGetHint("quest", out _));
            Assert.IsFalse(display.TryGetHint(hint.Guid, out _));
        }

        [TestMethod]
        // Verify null/empty AddHint inputs are ignored without changing collection state.
        public void AddHint_NullInput_ShouldBeIgnored()
        {
            display.AddHint((AbstractHint?)null);
            display.AddHint((AbstractHint[]?)null);
            display.AddHint(Array.Empty<AbstractHint>());

            Assert.AreEqual(0, display.GetHints().Count());
        }

        [TestMethod]
        // Verify hint property changes schedule updates according to SyncSpeed and Hide rules.
        public void HintPropertyUpdate_ShouldRespectSyncSpeedAndHideRules()
        {
            // Unsynced hints should not trigger scheduling when updated.
            Hint unSyncHint = new() { Id = "unsync", SyncSpeed = HintSyncSpeed.UnSync };
            display.AddHint(unSyncHint);
            int beforeUnSyncUpdate = scheduler.Invokes.Count;

            unSyncHint.FontSize++;
            Assert.AreEqual(beforeUnSyncUpdate, scheduler.Invokes.Count);

            // Hidden hint updates (except Hide itself) should be ignored.
            Hint hiddenHint = new() { Id = "hidden", SyncSpeed = HintSyncSpeed.Fast, Hide = true };
            display.AddHint(hiddenHint);
            int beforeHiddenUpdate = scheduler.Invokes.Count;
            hiddenHint.FontSize++;
            Assert.AreEqual(beforeHiddenUpdate, scheduler.Invokes.Count);

            // Changing Hide itself should trigger a sync-based schedule.
            hiddenHint.Hide = false;
            Assert.IsTrue(scheduler.Invokes.Count > beforeHiddenUpdate);

            // Fast hint should schedule with a short delay and KeepFastest strategy.
            (float Delay, DelayType DelayType) lastInvoke = scheduler.Invokes[scheduler.Invokes.Count - 1];
            Console.WriteLine($"Scheduled delay: {lastInvoke.Delay}, strategy: {lastInvoke.DelayType}");
            Assert.IsTrue(lastInvoke.Delay <= 0.1f);
            Assert.AreEqual(DelayType.KeepFastest, lastInvoke.DelayType);
        }

        [TestMethod]
        // Verify SendHint fans out only to active outputs and tolerates output exceptions.
        public void AddRemoveDisplayOutput_AndSendHint_ShouldDeliverToActiveOutputsOnly()
        {
            TestDisplayOutput keepOutput = new();
            TestDisplayOutput removeOutput = new();
            TestDisplayOutput throwOutput = new() { ThrowOnShow = true };

            display.AddDisplayOutput(keepOutput);
            display.AddDisplayOutput(removeOutput);
            display.AddDisplayOutput(throwOutput);
            display.RemoveDisplayOutput(removeOutput);

            // Removed output must not receive messages; throwing output must not break others.
            InvokeSendHint(display, "hello");

            Assert.AreEqual(1, keepOutput.Calls.Count);
            Assert.AreEqual("hello", keepOutput.Calls[0].Content);
            Assert.AreEqual(0, removeOutput.Calls.Count);
        }

        [TestMethod]
        // Verify compatibility hint calls are forwarded to adaptor with correct payload.
        public void ShowCompatibilityHint_ShouldForwardToAdaptor()
        {
            display.ShowCompatibilityHint("test-asm", "compat-content", 2.5f);

            Assert.AreEqual(1, adaptor.Calls.Count);
            Assert.AreEqual("test-asm", adaptor.Calls[0].AssemblyName);
            Assert.AreEqual("compat-content", adaptor.Calls[0].Content);
            Assert.AreEqual(2.5f, adaptor.Calls[0].Duration, 0.001f);
        }

        [TestMethod]
        // Verify Destruct propagates cleanup to injected scheduler and adaptor dependencies.
        public void Destruct_ShouldDestructInjectedSchedulerAndAdaptor()
        {
            ((IDestructible)display).Destruct();

            Assert.IsTrue(scheduler.IsDestructed);
            Assert.IsTrue(adaptor.IsDestructed);
        }

        private static void InvokeSendHint(PlayerDisplay pd, string text)
        {
            // SendHint is private; invoke via reflection to test output fan-out behavior directly.
            MethodInfo method = typeof(PlayerDisplay).GetMethod("SendHint", BindingFlags.NonPublic | BindingFlags.Instance)!;
            method.Invoke(pd, [text]);
        }
    }
}
