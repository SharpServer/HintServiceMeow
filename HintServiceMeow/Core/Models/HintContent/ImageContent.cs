namespace HintServiceMeow.Core.Models.HintContent
{
    using System;
    using System.Collections.Generic;
    using System.Threading;
    using HintServiceMeow.Core.Models.Arguments;
    using HintServiceMeow.Core.Utilities.Image;
    using HintServiceMeow.Core.Utilities.Tools;

    /// <summary>
    /// An <see cref="AbstractHintContent"/> that plays an animated image (GIF or static bitmap)
    /// as coloured block-character rich text on the player's screen.
    ///
    /// <para><b>Performance:</b></para>
    /// <list type="bullet">
    ///   <item>Rendering uses <c>LockBits</c> bulk pixel reads — far faster than <c>GetPixel()</c>.</item>
    ///   <item>All rendered frames are stored in <see cref="ImageRenderCache"/>; requesting the same
    ///         image a second time (any player, any configuration) returns cached frames instantly.</item>
    ///   <item>Multiple players requesting the same image concurrently share a single render task —
    ///         the image is never rendered more than once per unique path/URL + parameter set.</item>
    ///   <item>Frames are shown progressively: the first frame appears as soon as it is ready,
    ///         without waiting for the entire image to be decoded.</item>
    /// </list>
    ///
    /// <para><b>Usage:</b></para>
    /// <code>
    /// var content = ImageContent.LoadFromFile("/path/to/image.gif", fps: 15, loop: true);
    /// var hint    = new Hint { YCoordinate = 400, Content = content };
    /// PlayerDisplay.Get(player).AddHint(hint);
    /// </code>
    /// </summary>
    public sealed class ImageContent : AbstractHintContent
    {
        // ------------------------------------------------------------------ //
        // Fields                                                              //
        // ------------------------------------------------------------------ //
        private readonly ReaderWriterLockSlim frameLock = new ReaderWriterLockSlim();
        private readonly List<string> frames = new List<string>();

        private float fps;
        private bool loop;

        private int currentFrameIndex = -1;   // -1 = no frame available yet
        private DateTime nextFrameTime = DateTime.MinValue;

        private bool renderComplete = false;
        private Exception? renderError = null;

        // ------------------------------------------------------------------ //
        // Constructor (private — use static factory methods)                  //
        // ------------------------------------------------------------------ //
        private ImageContent(float fps, bool loop)
        {
            this.fps = fps > 0f ? fps : 10f;
            this.loop = loop;
        }

        // ------------------------------------------------------------------ //
        // Properties                                                          //
        // ------------------------------------------------------------------ //

        /// <summary>Gets or sets the playback speed in frames per second.</summary>
        public float Fps
        {
            get => fps;
            set => fps = value > 0f ? value : 10f;
        }

        /// <summary>Gets or sets whether the animation loops after the last frame.</summary>
        public bool Loop
        {
            get => loop;
            set => loop = value;
        }

        /// <summary>Gets whether the background render task has finished.</summary>
        public bool IsRenderComplete => renderComplete;

        /// <summary>Gets the render error reported by the renderer, if any.</summary>
        public Exception? RenderError => renderError;

        /// <summary>Gets the number of frames currently available in the local store.</summary>
        public int FrameCount
        {
            get
            {
                frameLock.EnterReadLock();
                try
                {
                    return frames.Count;
                }
                finally
                {
                    frameLock.ExitReadLock();
                }
            }
        }

        // ------------------------------------------------------------------ //
        // Static factory methods                                              //
        // ------------------------------------------------------------------ //

        /// <summary>
        /// Creates an <see cref="ImageContent"/> that loads and renders an image from a local file.
        ///
        /// If the same file has been requested before (with the same render parameters), the cached
        /// frames are used immediately — no I/O or rendering occurs.  If rendering is already in
        /// progress for this file, this instance subscribes to receive frames as they arrive.
        /// </summary>
        /// <param name="filePath">Absolute path to the image file (.png, .jpg, .gif, etc.).</param>
        /// <param name="fps">Playback speed in frames per second.</param>
        /// <param name="loop">Whether the animation loops after the last frame.</param>
        /// <param name="scale">Font-size percentage baked into each frame (0 = auto-calculate).</param>
        /// <param name="shapeCorrection">Stretch the bitmap to correct for non-square block glyphs.</param>
        /// <param name="compress">Merge adjacent similar-coloured pixels to reduce packet size.</param>
        /// <returns>A new <see cref="ImageContent"/> ready for use (frames may still be arriving).</returns>
        public static ImageContent LoadFromFile(
            string filePath,
            float fps = 10f,
            bool loop = true,
            float scale = 0f,
            bool shapeCorrection = true,
            bool compress = true)
        {
            if (filePath == null)
            {
                throw new ArgumentNullException(nameof(filePath));
            }

            var key = new CacheKey(filePath, isUrl: false, scale, shapeCorrection, compress);
            return FromCacheEntry(ImageRenderCache.GetOrCreate(key), fps, loop);
        }

        /// <summary>
        /// Creates an <see cref="ImageContent"/> that downloads and renders an image from a URL.
        ///
        /// Caching behaviour is identical to <see cref="LoadFromFile"/>: the same URL is downloaded
        /// and rendered at most once per unique parameter set.
        /// </summary>
        /// <param name="url">HTTP/HTTPS URL of the image.</param>
        /// <param name="fps">Playback speed in frames per second.</param>
        /// <param name="loop">Whether the animation loops after the last frame.</param>
        /// <param name="scale">Font-size percentage baked into each frame (0 = auto-calculate).</param>
        /// <param name="shapeCorrection">Stretch the bitmap to correct for non-square block glyphs.</param>
        /// <param name="compress">Merge adjacent similar-coloured pixels to reduce packet size.</param>
        /// <returns>A new <see cref="ImageContent"/> ready for use (frames may still be arriving).</returns>
        public static ImageContent LoadFromUrl(
            string url,
            float fps = 10f,
            bool loop = true,
            float scale = 0f,
            bool shapeCorrection = true,
            bool compress = true)
        {
            if (url == null)
            {
                throw new ArgumentNullException(nameof(url));
            }

            var key = new CacheKey(url, isUrl: true, scale, shapeCorrection, compress);
            return FromCacheEntry(ImageRenderCache.GetOrCreate(key), fps, loop);
        }

        /// <summary>
        /// Creates an <see cref="ImageContent"/> from a pre-rendered list of frame strings.
        /// Frames are available immediately — no background task is started.
        /// Ideal for sharing already-cached frames across multiple players without any overhead.
        /// </summary>
        /// <param name="prerenderedFrames">Rich-text frame strings (output of the renderer).</param>
        /// <param name="fps">Playback speed in frames per second.</param>
        /// <param name="loop">Whether the animation loops.</param>
        /// <returns>A new <see cref="ImageContent"/> ready to display immediately.</returns>
        public static ImageContent FromFrames(
            IEnumerable<string> prerenderedFrames,
            float fps = 10f,
            bool loop = true)
        {
            if (prerenderedFrames == null)
            {
                throw new ArgumentNullException(nameof(prerenderedFrames));
            }

            var content = new ImageContent(fps, loop);

            content.frameLock.EnterWriteLock();
            try
            {
                content.frames.AddRange(prerenderedFrames);
                if (content.frames.Count > 0)
                {
                    content.currentFrameIndex = 0;
                }
            }
            finally
            {
                content.frameLock.ExitWriteLock();
            }

            content.renderComplete = true;
            return content;
        }

        // ------------------------------------------------------------------ //
        // AbstractHintContent implementation                                  //
        // ------------------------------------------------------------------ //

        /// <summary>
        /// Returns the rich-text string for the current frame, or <see langword="null"/> if no
        /// frames are available yet.
        /// </summary>
        public override string? GetText()
        {
            frameLock.EnterReadLock();
            try
            {
                if (currentFrameIndex < 0 || currentFrameIndex >= frames.Count)
                {
                    return null;
                }

                return frames[currentFrameIndex];
            }
            finally
            {
                frameLock.ExitReadLock();
            }
        }

        /// <summary>
        /// Advances to the next frame when the configured FPS interval has elapsed.
        /// Fires <see cref="AbstractHintContent.ContentUpdated"/> on each frame advance so the
        /// display pipeline sends the new frame to the player immediately.
        /// </summary>
        public override void TryUpdate(ContentUpdateArg ev)
        {
            if (DateTime.Now < nextFrameTime)
            {
                return;
            }

            frameLock.EnterReadLock();
            int count;
            int index;
            try
            {
                count = frames.Count;
                index = currentFrameIndex;
            }
            finally
            {
                frameLock.ExitReadLock();
            }

            if (count == 0)
            {
                return; // No frames yet — wait for the renderer.
            }

            // First activation.
            if (index < 0)
            {
                SetFrameIndex(0);
                ScheduleNextFrame();
                OnUpdated();
                return;
            }

            int nextIndex = index + 1;

            if (nextIndex >= count)
            {
                if (loop)
                {
                    nextIndex = 0;
                }
                else
                {
                    return; // Animation done; keep last frame visible.
                }
            }

            SetFrameIndex(nextIndex);
            ScheduleNextFrame();
            OnUpdated();
        }

        // ------------------------------------------------------------------ //
        // Lifecycle                                                           //
        // ------------------------------------------------------------------ //

        /// <summary>
        /// Marks this instance as stopped.
        /// The shared <see cref="ImageRenderCache"/> is not affected — cached frames remain
        /// available for other <see cref="ImageContent"/> instances.
        /// </summary>
        public void Stop()
        {
            renderComplete = true;
        }

        // ------------------------------------------------------------------ //
        // Internal callbacks (called from CacheEntry subscriber)             //
        // ------------------------------------------------------------------ //

        /// <summary>Called when a new frame arrives from the shared render task or snapshot replay.</summary>
        internal void OnFrameArrived(string frameText)
        {
            frameLock.EnterWriteLock();
            try
            {
                frames.Add(frameText);

                // Activate on the very first frame so the player sees something immediately.
                if (currentFrameIndex < 0)
                {
                    currentFrameIndex = 0;
                }
            }
            finally
            {
                frameLock.ExitWriteLock();
            }

            // Reset timer so TryUpdate pushes the new frame on its next cycle.
            nextFrameTime = DateTime.MinValue;
            OnUpdated();
        }

        /// <summary>Called once when the shared render task finishes (success or error).</summary>
        internal void OnRenderComplete(Exception? error)
        {
            renderError = error;
            renderComplete = true;

            if (error != null)
            {
                Logger.Instance.Error($"[ImageContent] Render error: {error.Message}");
            }
        }

        // ------------------------------------------------------------------ //
        // Private helpers                                                     //
        // ------------------------------------------------------------------ //

        /// <summary>
        /// Builds an <see cref="ImageContent"/> backed by the given <see cref="CacheEntry"/>.
        ///
        /// <b>Fast path</b>: rendering already complete → frames copied in bulk, no subscription.
        ///
        /// <b>Slow path</b>: rendering in progress → <see cref="CacheEntry.SubscribeAndGetSnapshot"/>
        /// atomically registers this instance as a subscriber AND returns all frames rendered so far.
        /// The snapshot is replayed, and all subsequent frames arrive via callback.
        /// </summary>
        private static ImageContent FromCacheEntry(CacheEntry entry, float fps, bool loop)
        {
            var content = new ImageContent(fps, loop);

            if (entry.IsComplete)
            {
                // Fast path: all frames already in cache.
                List<string> cached = entry.GetFrameSnapshot();

                content.frameLock.EnterWriteLock();
                try
                {
                    content.frames.AddRange(cached);
                    if (content.frames.Count > 0)
                    {
                        content.currentFrameIndex = 0;
                    }
                }
                finally
                {
                    content.frameLock.ExitWriteLock();
                }

                content.renderComplete = true;

                if (entry.Error != null)
                {
                    content.renderError = entry.Error;
                    Logger.Instance.Error($"[ImageContent] Cached render error: {entry.Error.Message}");
                }

                return content;
            }

            // Slow path: subscribe + replay snapshot.
            // SubscribeAndGetSnapshot is atomic: every frame is in exactly one of {snapshot, future callbacks}.
            List<string> snapshot = entry.SubscribeAndGetSnapshot(
                content.OnFrameArrived,
                content.OnRenderComplete);

            foreach (string frame in snapshot)
            {
                content.OnFrameArrived(frame);
            }

            return content;
        }

        private void SetFrameIndex(int index)
        {
            frameLock.EnterWriteLock();
            try
            {
                currentFrameIndex = index;
            }
            finally
            {
                frameLock.ExitWriteLock();
            }
        }

        private void ScheduleNextFrame()
        {
            nextFrameTime = DateTime.Now.AddSeconds(fps > 0f ? 1.0 / fps : 0.1);
        }
    }
}
