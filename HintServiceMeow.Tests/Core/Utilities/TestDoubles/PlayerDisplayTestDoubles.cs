using HintServiceMeow.Core.Enum;
using HintServiceMeow.Core.Interface;
using HintServiceMeow.Core.Models;
using HintServiceMeow.Core.Models.Arguments;
using HintServiceMeow.Core.Utilities;
using System;
using System.Collections.Generic;

namespace HintServiceMeow.Tests.Core.Utilities.TestDoubles
{
    /// <summary>
    /// Minimal player context test double with controllable validity state.
    /// </summary>
    internal sealed class TestPlayerContext : IPlayerContext
    {
        public bool IsStillValid { get; set; }

        public bool IsValid() => IsStillValid;

        public bool Equals(IPlayerContext other)
        {
            return ReferenceEquals(this, other);
        }
    }

    /// <summary>
    /// Scheduler test double that records invoke requests for assertions.
    /// </summary>
    internal sealed class TestTaskScheduler : ITaskScheduler, IDestructible
    {
        private Action callback = null!;

        public TimeSpan Elapsed { get; set; } = TimeSpan.Zero;

        public bool IsReadyForNextAction { get; set; }

        public bool IsPaused { get; private set; }

        public bool IsDestructed { get; private set; }

        public List<(float Delay, DelayType DelayType)> Invokes { get; } = [];

        public void Start(TimeSpan interval, Action callback)
        {
            // Keep callback reference so tests can trigger it explicitly if needed.
            this.callback = callback ?? throw new ArgumentNullException(nameof(callback));
        }

        public void Invoke(float delay = -1f, DelayType delayType = DelayType.Override)
        {
            // Record all scheduling requests from PlayerDisplay.
            Invokes.Add((delay, delayType));
        }

        public void TriggerScheduledCallback()
        {
            // Simulate scheduler tick invoking the action registered in Start.
            callback();
        }

        public void Stop()
        {
        }

        public void Pause()
        {
            IsPaused = true;
        }

        public void Resume()
        {
            IsPaused = false;
        }

        public void Destruct()
        {
            IsDestructed = true;
        }
    }

    /// <summary>
    /// Compatibility adaptor test double that records forwarding calls.
    /// </summary>
    internal sealed class TestCompatibilityAdaptor : ICompatibilityAdaptor, IDestructible
    {
        public List<CompatibilityAdaptorArg> Calls { get; } = [];

        public bool IsDestructed { get; private set; }

        public void ShowHint(CompatibilityAdaptorArg ev)
        {
            Calls.Add(ev);
        }

        public void Destruct()
        {
            IsDestructed = true;
        }
    }

    /// <summary>
    /// Hint parser test double that returns caller-provided text.
    /// </summary>
    internal sealed class TestHintParser : IHintParser
    {
        public string ReturnText { get; set; } = string.Empty;

        public int ParseCallCount { get; private set; }

        public string ParseToMessage(HintCollection collection)
        {
            ParseCallCount++;
            return ReturnText;
        }
    }

    /// <summary>
    /// Display output test double that can optionally throw for resilience tests.
    /// </summary>
    internal sealed class TestDisplayOutput : IDisplayOutput
    {
        public List<DisplayOutputArg> Calls { get; } = [];

        public bool ThrowOnShow { get; set; }

        public void ShowHint(DisplayOutputArg ev)
        {
            if (ThrowOnShow)
                throw new InvalidOperationException("Display output test exception");

            Calls.Add(ev);
        }
    }

    /// <summary>
    /// Update analyzer test double with fixed prediction value.
    /// </summary>
    internal sealed class FixedUpdateAnalyser : IUpdateAnalyser
    {
        public DateTime NextUpdateTime { get; set; } = DateTime.MaxValue;

        public int OnUpdateCallCount { get; private set; }

        public void OnUpdate()
        {
            OnUpdateCallCount++;
        }

        public DateTime EstimateNextUpdate()
        {
            return NextUpdateTime;
        }
    }
}
