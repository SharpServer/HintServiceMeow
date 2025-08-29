namespace HintServiceMeow.Core.Interface
{
    using System;

    using HintServiceMeow.Core.Enum;

    internal interface ITaskScheduler
    {
        public TimeSpan Elapsed { get; }

        public bool IsReadyForNextAction { get; }

        void Start(TimeSpan interval, Action callback);

        void Invoke(float delay = -1f, DelayType delayType = DelayType.Override);

        void Stop();

        void Pause();

        void Resume();
    }
}
