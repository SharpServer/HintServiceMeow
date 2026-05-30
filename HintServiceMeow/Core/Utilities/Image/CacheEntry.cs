namespace HintServiceMeow.Core.Utilities.Image
{
    using System;
    using System.Collections.Generic;
    using System.Threading.Tasks;

    /// <summary>
    /// Holds the rendered frames for a single <see cref="CacheKey"/> and manages a list of
    /// per-<see cref="HintServiceMeow.Core.Models.HintContent.ImageContent"/> subscriber callbacks.
    ///
    /// Thread-safety contract:
    /// <list type="bullet">
    ///   <item><see cref="SubscribeAndGetSnapshot"/> is atomic with <see cref="AddFrame"/>:
    ///         a frame is guaranteed to appear in exactly one of the snapshot or the future callbacks.</item>
    ///   <item>All callbacks are invoked outside internal locks to prevent deadlocks.</item>
    /// </list>
    /// </summary>
    internal sealed class CacheEntry
    {
        private readonly List<string> frames = new List<string>();
        private readonly List<Action<string>> frameSubscribers = new List<Action<string>>();
        private readonly List<Action<Exception?>> completeSubscribers = new List<Action<Exception?>>();
        private readonly object syncLock = new object();

        // ------------------------------------------------------------------ //
        // Properties                                                          //
        // ------------------------------------------------------------------ //

        /// <summary>Gets whether all frames have been rendered (or a fatal error occurred).</summary>
        internal bool IsComplete { get; private set; }

        /// <summary>Gets any fatal error reported by the renderer.</summary>
        internal Exception? Error { get; private set; }

        /// <summary>Gets the background render task (for diagnostics).</summary>
        internal Task? RenderTask { get; set; }

        /// <summary>Gets the number of frames currently stored in this entry.</summary>
        internal int FrameCount
        {
            get
            {
                lock (syncLock)
                {
                    return frames.Count;
                }
            }
        }

        // ------------------------------------------------------------------ //
        // Public / internal methods                                           //
        // ------------------------------------------------------------------ //

        /// <summary>
        /// Atomically registers <paramref name="onFrame"/> for all future frames and returns a
        /// snapshot of the frames that have already arrived.
        ///
        /// Callers should replay the snapshot and then rely on the subscriber for anything that
        /// arrives afterwards.  If rendering is already complete, no subscription is registered.
        /// </summary>
        internal List<string> SubscribeAndGetSnapshot(
            Action<string> onFrame,
            Action<Exception?> onComplete)
        {
            lock (syncLock)
            {
                if (IsComplete)
                {
                    if (Error != null)
                    {
                        onComplete(Error);
                    }

                    return new List<string>(frames);
                }

                // Register BEFORE taking the snapshot so no frame slips between the two.
                frameSubscribers.Add(onFrame);
                completeSubscribers.Add(onComplete);
                return new List<string>(frames);
            }
        }

        /// <summary>
        /// Returns a snapshot of all currently available frames without registering a subscriber.
        /// Use this on the fast path when <see cref="IsComplete"/> is already <see langword="true"/>.
        /// </summary>
        internal List<string> GetFrameSnapshot()
        {
            lock (syncLock)
            {
                return new List<string>(frames);
            }
        }

        /// <summary>Called by the renderer to store a new frame and notify all subscribers.</summary>
        internal void AddFrame(string frame)
        {
            Action<string>[] subs;
            lock (syncLock)
            {
                frames.Add(frame);
                subs = frameSubscribers.ToArray();
            }

            // Invoke callbacks outside the lock.
            foreach (var sub in subs)
            {
                sub(frame);
            }
        }

        /// <summary>Called by the renderer when all frames have been delivered (or on fatal error).</summary>
        internal void Complete(Exception? error)
        {
            Action<Exception?>[] subs;
            lock (syncLock)
            {
                IsComplete = true;
                Error = error;
                subs = completeSubscribers.ToArray();
                frameSubscribers.Clear();
                completeSubscribers.Clear();
            }

            foreach (var sub in subs)
            {
                sub(error);
            }
        }
    }
}
