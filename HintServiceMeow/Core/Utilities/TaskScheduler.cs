namespace HintServiceMeow.Core.Utilities
{
    using System;
    using System.Threading;
    using System.Threading.Tasks;
    using HintServiceMeow.Core.Enum;
    using HintServiceMeow.Core.Utilities.Tools;

    internal class TaskScheduler : Interface.ITaskScheduler, Interface.IDestructible
    {
        private readonly ReaderWriterLockSlim actionTimeLock = new();

        private readonly PeriodicRunner runner;

        private Action action;
        private DateTime scheduledActionTime; // Indicate when the timer will begin trying invoking the action
        private TimeSpan interval; // Minimum time between two actions
        private DateTime startTimeStamp; // Used to calculate elapsed time since last action, = DateTime.MinValue if there's no last action.
        private TimeSpan elapsed; // Time elapsed since last action, does not include the time when the scheduler is paused.
        private bool paused;

        public TaskScheduler(int tickRate = 30)
        {
            interval = TimeSpan.FromSeconds(0);
            action = () => { }; // Default empty action
            startTimeStamp = DateTime.MinValue; // Default zero interval

            runner = PeriodicRunner.Start(PeriodicRunnerMethod, TimeSpan.FromSeconds(1.0 / tickRate));
        }

        public bool IsPaused => paused;

        /// <summary>
        /// Gets the time elapsed since last action. Does not include the time when the scheduler is paused.
        /// If there's no last action, it is DateTime.Now - DateTime.MinValue.
        /// </summary>
        public TimeSpan Elapsed
        {
            get
            {
                actionTimeLock.EnterWriteLock();
                try
                {
                    if (IsPaused)
                        return elapsed; // Do not calculate elapsed time during paused period

                    CalculateElapsedTime();
                    return elapsed;
                }
                finally
                {
                    actionTimeLock.ExitWriteLock();
                }
            }

            private set
            {
                actionTimeLock.EnterWriteLock();
                try
                {
                    elapsed = value; // Set elapsed time
                    startTimeStamp = DateTime.Now; // Reset time stamp
                }
                finally
                {
                    actionTimeLock.ExitWriteLock();
                }
            }
        }

        public bool IsReadyForNextAction => Elapsed >= interval;

        private DateTime ScheduledActionTime
        {
            get
            {
                actionTimeLock.EnterReadLock();
                try
                {
                    return scheduledActionTime;
                }
                finally
                {
                    actionTimeLock.ExitReadLock();
                }
            }

            set
            {
                actionTimeLock.EnterWriteLock();
                try
                {
                    scheduledActionTime = value;
                }
                finally
                {
                    actionTimeLock.ExitWriteLock();
                }
            }
        }

        /// <summary>
        /// Not thread safe.
        /// </summary>
        void Interface.IDestructible.Destruct()
        {
            runner.Dispose();
        }

        /// <summary>
        /// Start the scheduler with a specified interval and action.
        /// </summary>
        /// <param name="newInterval">Minimum interval between each action.</param>
        /// <param name="newAction">The action to invoke.</param>
        /// <exception cref="ArgumentNullException">newAction is null.</exception>
        public void Start(TimeSpan newInterval, Action newAction)
        {
            actionTimeLock.EnterWriteLock();
            try
            {
                if (newInterval <= TimeSpan.Zero)
                    newInterval = TimeSpan.Zero;
                interval = newInterval;
                action = newAction ?? throw new ArgumentNullException(nameof(newAction), "Action cannot be null.");
            }
            finally
            {
                actionTimeLock.ExitWriteLock();
            }

            // Reset the timer
            Elapsed = TimeSpan.Zero;
            ScheduledActionTime = DateTime.MaxValue; // Reset scheduled action time
        }

        /// <summary>
        /// Schedule an action to be invoked after a specified delay. The action will be invoked when the elapsed time reaches the interval limit and when scheduled action time is reached.
        /// </summary>
        /// <param name="delay">How long scheduler should wait before trying to invoke the action.</param>
        /// <param name="delayType">What to do if there's already a scheduled action.</param>
        public void Invoke(float delay = -1f, DelayType delayType = DelayType.Override)
        {
            actionTimeLock.EnterWriteLock();

            try
            {
                // If there's not scheduled time, then set it to the current time plus delay
                if (scheduledActionTime == DateTime.MaxValue)
                {
                    scheduledActionTime = DateTime.Now.AddSeconds(delay);
                    return;
                }

                // If there is a scheduled time, set based on the DelayType passed in
                switch (delayType)
                {
                    case DelayType.KeepFastest:
                        if (scheduledActionTime > DateTime.Now.AddSeconds(delay))
                            scheduledActionTime = DateTime.Now.AddSeconds(delay);
                        break;
                    case DelayType.KeepSlowest:
                        if (scheduledActionTime < DateTime.Now.AddSeconds(delay))
                            scheduledActionTime = DateTime.Now.AddSeconds(delay);
                        break;
                    case DelayType.Override:
                        scheduledActionTime = DateTime.Now.AddSeconds(delay);
                        break;
                    default:
                        throw new ArgumentOutOfRangeException(nameof(delayType), delayType, null);
                }
            }
            finally
            {
                actionTimeLock.ExitWriteLock();
            }
        }

        /// <summary>
        /// Stop the scheduler and reset the action and interval.
        /// </summary>
        public void Stop()
        {
            actionTimeLock.EnterWriteLock();
            try
            {
                // Reset the action and interval
                action = () => { }; // Default empty action
                interval = TimeSpan.FromSeconds(0);
                scheduledActionTime = DateTime.MaxValue; // Reset scheduled action time
            }
            finally
            {
                actionTimeLock.ExitWriteLock();
            }

            Elapsed = TimeSpan.Zero; // Reset elapsed time
            ScheduledActionTime = DateTime.MaxValue; // Reset scheduled action time
        }

        public void Pause()
        {
            actionTimeLock.EnterWriteLock();
            try
            {
                if (paused)
                    return;

                CalculateElapsedTime(); // Add time to the timer before pausing

                paused = true;
            }
            finally
            {
                actionTimeLock.ExitWriteLock();
            }
        }

        public void Resume()
        {
            actionTimeLock.EnterWriteLock();
            try
            {
                if (!paused)
                    return;

                paused = false;
                startTimeStamp = DateTime.Now; // Reset time stamp
            }
            finally
            {
                actionTimeLock.ExitWriteLock();
            }
        }

        /// <summary>
        /// Invoke the action and reset the timer and scheduled action time.
        /// </summary>
        private void InvokeAction()
        {
            try
            {
                // Reset timer
                Elapsed = TimeSpan.Zero; // Reset Elapsed Time
                ScheduledActionTime = DateTime.MaxValue; // Reset scheduled action time

                // start action
                action.Invoke();
            }
            catch (Exception ex)
            {
                Logger.Instance.Error(ex);
            }
        }

        private void CalculateElapsedTime()
        {
            // If the scheduled action time is in the future, skip
            if (startTimeStamp > DateTime.Now)
                return;

            elapsed += DateTime.Now - startTimeStamp; // Calculate elapsed time
            startTimeStamp = DateTime.Now; // Reset time stamp
        }

        private Task PeriodicRunnerMethod()
        {
            // Check if the action should be executed, if not, continue, else, break the loop
            try
            {
                if (!IsReadyForNextAction || ScheduledActionTime == DateTime.MaxValue || DateTime.Now < ScheduledActionTime || IsPaused)
                    return Task.CompletedTask;
            }
            catch (Exception ex)
            {
                Logger.Instance.Error(ex);
            }

            InvokeAction();
            return Task.CompletedTask;
        }
    }
}
