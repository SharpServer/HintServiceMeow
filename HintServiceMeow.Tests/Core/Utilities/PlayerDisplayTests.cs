using HintServiceMeow.Core.Enum;
using HintServiceMeow.Core.Interface;
using HintServiceMeow.Core.Models;
using HintServiceMeow.Core.Models.Hints;
using HintServiceMeow.Core.Utilities;
using HintServiceMeow.Tests.Core.Utilities.TestDoubles;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Linq;
using System.Reflection;

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
        private PlayerDisplay display = null!;

        [TestInitialize]
        public void SetUp()
        {
            // Build a fully controlled test environment so each test is deterministic.
            scheduler = new TestTaskScheduler();
            adaptor = new TestCompatibilityAdaptor();
            parser = new TestHintParser();
            context = new TestPlayerContext { IsStillValid = false };

            display = new PlayerDisplay(context, updateScheduler: scheduler, adaptor: adaptor, hintParser: parser);
        }

        [TestCleanup]
        public void TearDown()
        {
            // Ensure internal resources are released between tests.
            ((IDestructible)display).Destruct();
        }

        [TestMethod]
        public void Constructor_ShouldThrow_WhenPlayerContextNull()
        {
            Assert.ThrowsExactly<ArgumentNullException>(() =>
            {
                _ = new PlayerDisplay(null!);
            });
        }

        [TestMethod]
        public void ForceUpdate_ShouldScheduleExpectedDelay()
        {
            // Default force update should use normal fast path delay.
            display.ForceUpdate();

            // Fast update should request immediate invocation.
            display.ForceUpdate(useFastUpdate: true);

            Assert.AreEqual(2, scheduler.Invokes.Count);
            Assert.AreEqual(0.3f, scheduler.Invokes[0].Delay, 0.0001f);
            Assert.AreEqual(0f, scheduler.Invokes[1].Delay, 0.0001f);
        }

        [TestMethod]
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
        public void AddHint_NullInput_ShouldBeIgnored()
        {
            display.AddHint((AbstractHint?)null);
            display.AddHint((AbstractHint[]?)null);
            display.AddHint(Array.Empty<AbstractHint>());

            Assert.AreEqual(0, display.GetHints().Count());
        }

        [TestMethod]
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
            (float Delay, DelayType DelayType) lastInvoke = scheduler.Invokes[^1];
            Assert.IsTrue(lastInvoke.Delay > 0f && lastInvoke.Delay <= 0.1f);
            Assert.AreEqual(DelayType.KeepFastest, lastInvoke.DelayType);
        }

        [TestMethod]
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
        public void ShowCompatibilityHint_ShouldForwardToAdaptor()
        {
            display.ShowCompatibilityHint("test-asm", "compat-content", 2.5f);

            Assert.AreEqual(1, adaptor.Calls.Count);
            Assert.AreEqual("test-asm", adaptor.Calls[0].AssemblyName);
            Assert.AreEqual("compat-content", adaptor.Calls[0].Content);
            Assert.AreEqual(2.5f, adaptor.Calls[0].Duration, 0.001f);
        }

        [TestMethod]
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
