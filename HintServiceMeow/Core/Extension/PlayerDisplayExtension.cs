namespace HintServiceMeow.Core.Extension
{
    using System;
    using System.Runtime.CompilerServices;

    using HintServiceMeow.Core.Enum;
    using HintServiceMeow.Core.Models.Hints;
    using HintServiceMeow.Core.Utilities;

    public static class PlayerDisplayExtension
    {
        private static readonly ConditionalWeakTable<PlayerDisplay, ConditionalWeakTable<AbstractHint, TaskScheduler>> RemoveTimers = new();

        /// <summary>
        /// Remove a hint after a delay. If a removal task is in progress, it will be reset.
        /// </summary>
        /// <param name="playerDisplay">The PlayerDisplay owning the hint.</param>
        /// <param name="hint">The hint to remove.</param>
        /// <param name="delay">How long until the hint is removed.</param>
        public static void RemoveAfter(this PlayerDisplay playerDisplay, AbstractHint hint, float delay)
        {
            if (!RemoveTimers.TryGetValue(playerDisplay, out ConditionalWeakTable<AbstractHint, TaskScheduler> hintDict))
            {
                hintDict = new ConditionalWeakTable<AbstractHint, TaskScheduler>();

                RemoveTimers.Add(playerDisplay, hintDict);
            }

            if (!hintDict.TryGetValue(hint, out TaskScheduler scheduler))
            {
                scheduler = new TaskScheduler();
                scheduler.Start(TimeSpan.Zero, () => playerDisplay.InternalRemoveHint(null, hint));

                hintDict.Add(hint, scheduler);
            }

            scheduler.Invoke(delay, DelayType.Override);
        }
    }
}