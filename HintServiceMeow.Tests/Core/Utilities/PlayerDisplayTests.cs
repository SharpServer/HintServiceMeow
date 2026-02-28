using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading;
using HintServiceMeow.Core.Enum;
using HintServiceMeow.Core.Interface;
using HintServiceMeow.Core.Models.Hints;
using HintServiceMeow.Core.Utilities;
using HintServiceMeow.Tests.Core.Utilities.TestDoubles;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace HintServiceMeow.Tests.Core.Utilities
{
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
            ((IDestructible)display).Destruct();
        }

        [TestMethod]
        public void Constructor_ShouldThrow_WhenPlayerContextNull()
        {
            Assert.ThrowsExactly<ArgumentNullException>(() => _ = new PlayerDisplay(null!));
        }

        [TestMethod]
        public void Constructor_ShouldStartCoroutine_WhenRunnerInjected()
        {
            Assert.AreEqual(1, coroutineRunner.StartedRoutines.Count);
            Assert.IsTrue(coroutineRunner.LastCoroutine.IsRunning);
            Assert.IsFalse(coroutineRunner.LastCoroutine.IsKilled);
        }

        [TestMethod]
        public void Destruct_ShouldKillStartedCoroutine()
        {
            ((IDestructible)display).Destruct();

            Assert.IsTrue(coroutineRunner.LastCoroutine.IsKilled);
            Assert.IsFalse(coroutineRunner.LastCoroutine.IsRunning);
        }

        [TestMethod]
        public void Destruct_ShouldDestructInjectedSchedulerAndAdaptor()
        {
            ((IDestructible)display).Destruct();

            Assert.IsTrue(scheduler.IsDestructed);
            Assert.IsTrue(adaptor.IsDestructed);
        }

        [TestMethod]
        public void ForceUpdate_ShouldScheduleExpectedDelay()
        {
            display.ForceUpdate();
            display.ForceUpdate(useFastUpdate: true);

            Assert.AreEqual(2, scheduler.Invokes.Count);
            Assert.IsTrue(scheduler.Invokes[0].Delay < 0.3f);
            Assert.IsTrue(scheduler.Invokes[1].Delay <= 0);
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

            display.RemoveHint(first.Guid);
            Assert.IsFalse(display.HasHint(first.Guid));
            Assert.IsTrue(display.HasHint(second.Guid));

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
            display.AddHint((IEnumerable<AbstractHint>?)null);

            Assert.AreEqual(0, display.GetHints().Count());
        }

        [TestMethod]
        public void AddHint_CollectionChange_ShouldTriggerScheduleUpdate()
        {
            int before = scheduler.Invokes.Count;

            display.AddHint(new Hint { Id = "collection-changed" });

            Assert.IsTrue(scheduler.Invokes.Count > before);
            Assert.IsTrue(scheduler.Invokes[^1].Delay <= 0);
        }

        [TestMethod]
        public void HintPropertyUpdate_ShouldRespectSyncSpeedAndHideRules()
        {
            Hint unSyncHint = new() { Id = "unsync", SyncSpeed = HintSyncSpeed.UnSync };
            display.AddHint(unSyncHint);
            int beforeUnSyncUpdate = scheduler.Invokes.Count;

            unSyncHint.FontSize++;
            Assert.AreEqual(beforeUnSyncUpdate, scheduler.Invokes.Count);

            Hint hiddenHint = new() { Id = "hidden", SyncSpeed = HintSyncSpeed.Fast, Hide = true };
            display.AddHint(hiddenHint);
            int beforeHiddenUpdate = scheduler.Invokes.Count;
            hiddenHint.FontSize++;
            Assert.AreEqual(beforeHiddenUpdate, scheduler.Invokes.Count);

            hiddenHint.Hide = false;
            Assert.IsTrue(scheduler.Invokes.Count > beforeHiddenUpdate);

            (float Delay, DelayType DelayType) lastInvoke = scheduler.Invokes[^1];
            Assert.IsTrue(lastInvoke.Delay <= 0.1f);
            Assert.AreEqual(DelayType.KeepFastest, lastInvoke.DelayType);
        }

        [DataTestMethod]
        [DataRow(HintSyncSpeed.Fastest, 0f)]
        [DataRow(HintSyncSpeed.Fast, 0.1f)]
        [DataRow(HintSyncSpeed.Normal, 0.3f)]
        [DataRow(HintSyncSpeed.Slow, 1f)]
        [DataRow(HintSyncSpeed.Slowest, 3f)]
        public void OnHintUpdate_SyncSpeedBranch_ShouldMapToExpectedSchedule(HintSyncSpeed speed, float expectedMaxWait)
        {
            Hint hint = new() { Id = "sync-map", SyncSpeed = speed };
            display.AddHint(hint);
            int before = scheduler.Invokes.Count;

            hint.FontSize++;

            Assert.IsTrue(scheduler.Invokes.Count > before);
            (float Delay, DelayType delayType) invoke = scheduler.Invokes[^1];

            if (speed == HintSyncSpeed.Fastest)
            {
                Assert.IsTrue(invoke.Delay <= 0f);
                Assert.AreEqual(DelayType.Override, invoke.delayType);
            }
            else
            {
                Assert.AreEqual(DelayType.KeepFastest, invoke.delayType);
                Assert.IsTrue(invoke.Delay >= 0f);
                Assert.IsTrue(invoke.Delay <= expectedMaxWait + 0.05f);
            }
        }

        [TestMethod]
        public void ScheduleUpdate_PredictionInWindow_ShouldUsePredictedDelayAndKeepFastest()
        {
            Hint updatingHint = new() { Id = "updating", SyncSpeed = HintSyncSpeed.Normal, UpdateAnalyser = new FixedUpdateAnalyser { NextUpdateTime = DateTime.Now.AddSeconds(10) } };
            Hint predictingHint = new() { Id = "predict", SyncSpeed = HintSyncSpeed.Slowest, UpdateAnalyser = new FixedUpdateAnalyser { NextUpdateTime = DateTime.Now.AddSeconds(0.4) } };

            display.AddHint(updatingHint, predictingHint);
            int before = scheduler.Invokes.Count;

            updatingHint.FontSize++;

            Assert.IsTrue(scheduler.Invokes.Count > before);
            (float Delay, DelayType DelayType) invoke = scheduler.Invokes[^1];
            Assert.AreEqual(DelayType.KeepFastest, invoke.DelayType);
            Assert.IsTrue(invoke.Delay > 0.2f && invoke.Delay < 0.7f);
        }

        [TestMethod]
        public void ScheduleUpdate_PredictionAfterWindow_ShouldClampToMaxWaitingTime()
        {
            Hint updatingHint = new() { Id = "updating", SyncSpeed = HintSyncSpeed.Fast, UpdateAnalyser = new FixedUpdateAnalyser { NextUpdateTime = DateTime.Now.AddSeconds(10) } };
            Hint predictingHint = new() { Id = "predict", SyncSpeed = HintSyncSpeed.Slowest, UpdateAnalyser = new FixedUpdateAnalyser { NextUpdateTime = DateTime.Now.AddSeconds(5) } };

            display.AddHint(updatingHint, predictingHint);

            updatingHint.Hide = true;

            (float Delay, DelayType DelayType) invoke = scheduler.Invokes[^1];
            Assert.AreEqual(DelayType.KeepFastest, invoke.DelayType);
            Assert.IsTrue(invoke.Delay >= 0f);
            Assert.IsTrue(invoke.Delay <= 0.02f);
        }

        [TestMethod]
        public void ScheduleUpdate_DateTimeMaxValueAndUpdatingHint_ShouldBeIgnored()
        {
            Hint updatingHint = new() { Id = "updating", SyncSpeed = HintSyncSpeed.Slow, UpdateAnalyser = new FixedUpdateAnalyser { NextUpdateTime = DateTime.Now.AddSeconds(0.5) } };
            Hint ignoredMaxHint = new() { Id = "max", SyncSpeed = HintSyncSpeed.Slowest, UpdateAnalyser = new FixedUpdateAnalyser { NextUpdateTime = DateTime.MaxValue } };

            display.AddHint(updatingHint, ignoredMaxHint);

            updatingHint.FontSize++;

            (float Delay, DelayType DelayType) invoke = scheduler.Invokes[^1];
            Assert.AreEqual(DelayType.KeepFastest, invoke.DelayType);
            Assert.IsTrue(invoke.Delay <= 0.02f);
        }

        [TestMethod]
        public void RemoveHint_AfterRemoval_PropertyChangesShouldNotSchedule()
        {
            Hint hint = new() { Id = "remove", SyncSpeed = HintSyncSpeed.Normal };
            display.AddHint(hint);
            scheduler.Invokes.Clear();

            display.RemoveHint(hint);
            hint.FontSize++;

            Assert.AreEqual(0, scheduler.Invokes.Count);
        }

        [TestMethod]
        public void ClearHint_AfterClear_PropertyChangesShouldNotSchedule()
        {
            Hint hint = new() { Id = "clear", SyncSpeed = HintSyncSpeed.Normal };
            display.AddHint(hint);
            scheduler.Invokes.Clear();

            display.ClearHint();
            hint.FontSize++;

            Assert.AreEqual(0, scheduler.Invokes.Count);
        }

        [TestMethod]
        public void Destruct_AfterDestruct_PropertyChangesShouldNotScheduleOrThrow()
        {
            Hint hint = new() { Id = "destruct", SyncSpeed = HintSyncSpeed.Normal };
            display.AddHint(hint);
            scheduler.Invokes.Clear();

            ((IDestructible)display).Destruct();
            hint.FontSize++;

            Assert.AreEqual(0, scheduler.Invokes.Count);
        }

        [TestMethod]
        public void RemoveDisplayOutputOfType_ShouldRemoveAllMatchedOutputs()
        {
            TestDisplayOutput first = new();
            TestDisplayOutput second = new();
            display.AddDisplayOutput(first);
            display.AddDisplayOutput(second);

            display.RemoveDisplayOutput<TestDisplayOutput>();
            InvokeSendHint(display, "after-remove-type");

            Assert.AreEqual(0, first.Calls.Count);
            Assert.AreEqual(0, second.Calls.Count);
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

            InvokeSendHint(display, "hello");

            Assert.AreEqual(1, keepOutput.Calls.Count);
            Assert.AreEqual("hello", keepOutput.Calls[0].Content);
            Assert.AreEqual(0, removeOutput.Calls.Count);
        }

        [TestMethod]
        public void SchedulerCallback_ShouldPauseParseSendAndResume_WhenPipelineSuccess()
        {
            TestMainThreadDispatcher dispatcher = new();
            TestDisplayOutput output = new();
            TestTaskScheduler localScheduler = new();
            PlayerDisplay localDisplay = new(
                new TestPlayerContext { IsStillValid = false },
                updateScheduler: localScheduler,
                adaptor: new TestCompatibilityAdaptor(),
                hintParser: new DelegateHintParser(_ => "pipeline-success"),
                coroutineRunner: new TestCoroutineRunner(),
                dispatcher: dispatcher,
                displayOutputs: new[] { output });

            try
            {
                localScheduler.TriggerScheduledCallback();

                AssertEventually(() => output.Calls.Count == 1 && !localScheduler.IsPaused, 1000, "Parser pipeline did not complete in time");
                Assert.AreEqual("pipeline-success", output.Calls[0].Content);
                Assert.IsTrue(dispatcher.DispatchCallCount >= 1);
            }
            finally
            {
                ((IDestructible)localDisplay).Destruct();
            }
        }

        [TestMethod]
        public void SchedulerCallback_WhenParserThrows_ShouldResumeWithoutSending()
        {
            TestMainThreadDispatcher dispatcher = new();
            TestDisplayOutput output = new();
            TestTaskScheduler localScheduler = new();
            PlayerDisplay localDisplay = new(
                new TestPlayerContext { IsStillValid = false },
                updateScheduler: localScheduler,
                adaptor: new TestCompatibilityAdaptor(),
                hintParser: new DelegateHintParser(_ => throw new InvalidOperationException("parser failure")),
                coroutineRunner: new TestCoroutineRunner(),
                dispatcher: dispatcher,
                displayOutputs: new[] { output });

            try
            {
                localScheduler.TriggerScheduledCallback();

                AssertEventually(() => !localScheduler.IsPaused, 1000, "Scheduler should resume after parser exception");
                Assert.AreEqual(0, output.Calls.Count);
            }
            finally
            {
                ((IDestructible)localDisplay).Destruct();
            }
        }

        [TestMethod]
        public void SchedulerCallback_WhenParserTaskRunning_ShouldNotStartParallelParserTask()
        {
            TestTaskScheduler localScheduler = new();
            ManualResetEventSlim entered = new(false);
            ManualResetEventSlim release = new(false);
            DelegateHintParser blockingParser = new(_ =>
            {
                entered.Set();
                release.Wait(1000);
                return "done";
            });

            PlayerDisplay localDisplay = new(
                new TestPlayerContext { IsStillValid = false },
                updateScheduler: localScheduler,
                adaptor: new TestCompatibilityAdaptor(),
                hintParser: blockingParser,
                coroutineRunner: new TestCoroutineRunner(),
                dispatcher: new TestMainThreadDispatcher());

            try
            {
                localScheduler.TriggerScheduledCallback();
                Assert.IsTrue(entered.Wait(500));
                Assert.IsTrue(localScheduler.IsPaused);

                localScheduler.TriggerScheduledCallback();
                Thread.Sleep(30);

                Assert.AreEqual(1, blockingParser.ParseCallCount);

                release.Set();
                AssertEventually(() => !localScheduler.IsPaused, 1000, "Scheduler should resume after blocking parser released");
            }
            finally
            {
                ((IDestructible)localDisplay).Destruct();
            }
        }

        [TestMethod]
        public void CoroutineMethod_ShouldStop_WhenPlayerContextInvalid()
        {
            IEnumerator<float> routine = coroutineRunner.StartedRoutines[0];

            Assert.IsTrue(routine.MoveNext());
            Assert.AreEqual(-1f, routine.Current);
            Assert.IsFalse(routine.MoveNext());
        }

        [TestMethod]
        public void CoroutineMethod_WhenElapsedOverFiveSeconds_ShouldSchedulePeriodicUpdate()
        {
            context.IsStillValid = true;
            scheduler.Elapsed = TimeSpan.FromSeconds(6);
            IEnumerator<float> routine = coroutineRunner.StartedRoutines[0];

            Assert.IsTrue(routine.MoveNext());
            Assert.AreEqual(-1f, routine.Current);

            Assert.IsTrue(routine.MoveNext());
            Assert.IsTrue(scheduler.Invokes.Any());
            Assert.IsTrue(scheduler.Invokes[^1].Delay <= 0);
        }

        [TestMethod]
        public void CoroutineMethod_WhenSchedulerReady_ShouldInvokeUpdateAvailable()
        {
            context.IsStillValid = true;
            scheduler.IsReadyForNextAction = true;
            int callCount = 0;
            display.UpdateAvailable += _ => callCount++;

            IEnumerator<float> routine = coroutineRunner.StartedRoutines[0];
            Assert.IsTrue(routine.MoveNext());
            Assert.IsTrue(routine.MoveNext());

            Assert.AreEqual(1, callCount);
        }

        [TestMethod]
        public void CoroutineMethod_WhenUpdateAvailableThrows_ShouldYieldBackoffDelay()
        {
            context.IsStillValid = true;
            scheduler.IsReadyForNextAction = true;
            display.UpdateAvailable += _ => throw new InvalidOperationException("update callback failed");
            IEnumerator<float> routine = coroutineRunner.StartedRoutines[0];

            Assert.IsTrue(routine.MoveNext());
            Assert.AreEqual(-1f, routine.Current);

            Assert.IsTrue(routine.MoveNext());
            Assert.AreEqual(1f, routine.Current);
        }

        [DataTestMethod]
        [DataRow(null)]
        [DataRow("")]
        public void RemoveHint_StringGuards_ShouldThrow(string? id)
        {
            if (id is null)
                Assert.ThrowsExactly<ArgumentNullException>(() => display.RemoveHint(id!));
            else
                Assert.ThrowsExactly<ArgumentException>(() => display.RemoveHint(id));
        }

        [DataTestMethod]
        [DataRow(null)]
        [DataRow("")]
        public void StringQueryGuards_ShouldThrow(string? id)
        {
            if (id is null)
            {
                Assert.ThrowsExactly<ArgumentNullException>(() => display.GetHint(id));
                Assert.ThrowsExactly<ArgumentNullException>(() => display.GetHints(id!));
                Assert.ThrowsExactly<ArgumentNullException>(() => display.HasHint(id!));
                Assert.ThrowsExactly<ArgumentNullException>(() => display.TryGetHint(id!, out _));
                Assert.ThrowsExactly<ArgumentNullException>(() => display.TryGetHints(id, out _));
            }
            else
            {
                Assert.ThrowsExactly<ArgumentException>(() => display.GetHint(id));
                Assert.ThrowsExactly<ArgumentException>(() => display.GetHints(id));
                Assert.ThrowsExactly<ArgumentException>(() => display.HasHint(id));
                Assert.ThrowsExactly<ArgumentException>(() => display.TryGetHint(id, out _));
                Assert.ThrowsExactly<ArgumentException>(() => display.TryGetHints(id, out _));
            }
        }

        [TestMethod]
        public void HintParserSetter_AndCompatibilityAdaptorSetter_ShouldGuardNull()
        {
            Assert.ThrowsExactly<ArgumentNullException>(() => display.HintParser = null!);
            Assert.ThrowsExactly<ArgumentNullException>(() => display.CompatibilityAdaptor = null!);
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

        private static void AssertEventually(Func<bool> condition, int timeoutMs, string message)
        {
            DateTime end = DateTime.UtcNow.AddMilliseconds(timeoutMs);
            while (DateTime.UtcNow < end)
            {
                if (condition())
                    return;

                Thread.Sleep(10);
            }

            Assert.Fail(message);
        }

        private static void InvokeSendHint(PlayerDisplay pd, string text)
        {
            MethodInfo method = typeof(PlayerDisplay).GetMethod("SendHint", BindingFlags.NonPublic | BindingFlags.Instance)!;
            method.Invoke(pd, [text]);
        }
    }
}
