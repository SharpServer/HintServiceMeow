namespace HintServiceMeow.Core.Utilities.Image
{
    using System;
    using System.Collections.Concurrent;
    using System.Collections.Generic;
    using HintServiceMeow.Core.Enum;
    using HintServiceMeow.Core.Models.HintContent;
    using HintServiceMeow.Core.Models.Hints;

    /// <summary>
    /// Provides a high-level API for playing image animations on a <see cref="PlayerDisplay"/>.
    ///
    /// <para><b>Usage:</b></para>
    /// <code>
    /// // Start a GIF on one player (file):
    /// ImagePlayback pb = ImageHintPlayer.PlayFile(display, "/path/to/image.gif", yCoordinate: 400f);
    ///
    /// // Start from a URL:
    /// ImagePlayback pb = ImageHintPlayer.PlayUrl(display, "https://example.com/img.gif");
    ///
    /// // Stop later:
    /// pb.Dispose();
    ///
    /// // Stop all image hints on a display:
    /// ImageHintPlayer.StopAll(display);
    /// </code>
    ///
    /// <para>Large images are handled transparently via <c>ChunkedHintMessageFactory</c>, so images
    /// that produce more than 65 KB of rich-text per frame are still delivered without hitting
    /// Mirror's per-string size limit.</para>
    ///
    /// <para>Rendering is cached globally: the same file/URL rendered with the same parameters is
    /// converted exactly once; subsequent calls (even for different players) reuse the cached frames.</para>
    /// </summary>
    public static class ImageHintPlayer
    {
        // Group name used when registering hints on PlayerDisplay.
        internal const string GroupName = "HintServiceMeow.ImageHintPlayer";

        // Tracks all active playbacks per display so StopAll() can clean up.
        private static readonly ConcurrentDictionary<PlayerDisplay, List<ImagePlayback>> ActivePlaybacks
            = new ConcurrentDictionary<PlayerDisplay, List<ImagePlayback>>();

        // ------------------------------------------------------------------ //
        // Public API                                                          //
        // ------------------------------------------------------------------ //

        /// <summary>
        /// Starts playing an image from a local file on the specified player's display.
        /// </summary>
        /// <param name="display">The target player display.</param>
        /// <param name="filePath">Absolute path to the image file (.png, .jpg, .gif, etc.).</param>
        /// <param name="yCoordinate">Vertical position (0–1080; higher value = lower on screen).</param>
        /// <param name="fps">Animation speed in frames per second.</param>
        /// <param name="loop">Whether to loop the animation after the last frame.</param>
        /// <param name="xCoordinate">Horizontal offset from centre.</param>
        /// <param name="alignment">Horizontal alignment of the hint.</param>
        /// <param name="scale">Font-size percentage baked into each frame (0 = auto-calculate).</param>
        /// <param name="shapeCorrection">Stretch the bitmap to correct for non-square block glyphs.</param>
        /// <param name="compress">Merge adjacent similar-coloured pixels to reduce packet size.</param>
        /// <returns>An <see cref="ImagePlayback"/> handle; dispose it to stop playback.</returns>
        public static ImagePlayback PlayFile(
            PlayerDisplay display,
            string filePath,
            float yCoordinate = 400f,
            float fps = 10f,
            bool loop = true,
            float xCoordinate = 0f,
            HintAlignment alignment = HintAlignment.Center,
            float scale = 0f,
            bool shapeCorrection = true,
            bool compress = true)
        {
            if (display == null)
            {
                throw new ArgumentNullException(nameof(display));
            }

            if (filePath == null)
            {
                throw new ArgumentNullException(nameof(filePath));
            }

            var content = ImageContent.LoadFromFile(filePath, fps, loop, scale, shapeCorrection, compress);
            return CreatePlayback(display, content, yCoordinate, xCoordinate, alignment);
        }

        /// <summary>
        /// Starts playing an image downloaded from a URL on the specified player's display.
        /// </summary>
        /// <param name="display">The target player display.</param>
        /// <param name="url">HTTP/HTTPS URL of the image.</param>
        /// <param name="yCoordinate">Vertical position (0–1080; higher value = lower on screen).</param>
        /// <param name="fps">Animation speed in frames per second.</param>
        /// <param name="loop">Whether to loop the animation after the last frame.</param>
        /// <param name="xCoordinate">Horizontal offset from centre.</param>
        /// <param name="alignment">Horizontal alignment of the hint.</param>
        /// <param name="scale">Font-size percentage baked into each frame (0 = auto-calculate).</param>
        /// <param name="shapeCorrection">Stretch the bitmap to correct for non-square block glyphs.</param>
        /// <param name="compress">Merge adjacent similar-coloured pixels to reduce packet size.</param>
        /// <returns>An <see cref="ImagePlayback"/> handle; dispose it to stop playback.</returns>
        public static ImagePlayback PlayUrl(
            PlayerDisplay display,
            string url,
            float yCoordinate = 400f,
            float fps = 10f,
            bool loop = true,
            float xCoordinate = 0f,
            HintAlignment alignment = HintAlignment.Center,
            float scale = 0f,
            bool shapeCorrection = true,
            bool compress = true)
        {
            if (display == null)
            {
                throw new ArgumentNullException(nameof(display));
            }

            if (url == null)
            {
                throw new ArgumentNullException(nameof(url));
            }

            var content = ImageContent.LoadFromUrl(url, fps, loop, scale, shapeCorrection, compress);
            return CreatePlayback(display, content, yCoordinate, xCoordinate, alignment);
        }

        /// <summary>
        /// Starts playing an image from either a local file path or URL on the specified player's display.
        /// </summary>
        /// <param name="display">The target player display.</param>
        /// <param name="location">File path or HTTP/HTTPS URL.</param>
        /// <param name="isUrl">Whether <paramref name="location"/> is a URL.</param>
        /// <param name="yCoordinate">Vertical position (0â€“1080; higher value = lower on screen).</param>
        /// <param name="fps">Animation speed in frames per second.</param>
        /// <param name="loop">Whether to loop the animation after the last frame.</param>
        /// <param name="xCoordinate">Horizontal offset from centre.</param>
        /// <param name="alignment">Horizontal alignment of the hint.</param>
        /// <param name="scale">Font-size percentage baked into each frame (0 = auto-calculate).</param>
        /// <param name="shapeCorrection">Stretch the bitmap to correct for non-square block glyphs.</param>
        /// <param name="compress">Merge adjacent similar-coloured pixels to reduce packet size.</param>
        /// <returns>An <see cref="ImagePlayback"/> handle; dispose it to stop playback.</returns>
        public static ImagePlayback PlayLocation(
            PlayerDisplay display,
            string location,
            bool isUrl = false,
            float yCoordinate = 400f,
            float fps = 10f,
            bool loop = true,
            float xCoordinate = 0f,
            HintAlignment alignment = HintAlignment.Center,
            float scale = 0f,
            bool shapeCorrection = true,
            bool compress = true)
        {
            if (display == null)
            {
                throw new ArgumentNullException(nameof(display));
            }

            if (location == null)
            {
                throw new ArgumentNullException(nameof(location));
            }

            var content = ImageContent.LoadFromLocation(location, isUrl, fps, loop, scale, shapeCorrection, compress);
            return CreatePlayback(display, content, yCoordinate, xCoordinate, alignment);
        }

        /// <summary>
        /// Starts playing a pre-rendered image from a list of frame strings.
        /// Ideal for sharing already-cached frames across multiple players without any overhead.
        /// </summary>
        /// <param name="display">The target player display.</param>
        /// <param name="frames">Pre-rendered rich-text frame strings.</param>
        /// <param name="yCoordinate">Vertical position (0–1080; higher value = lower on screen).</param>
        /// <param name="fps">Animation speed in frames per second.</param>
        /// <param name="loop">Whether to loop the animation after the last frame.</param>
        /// <param name="xCoordinate">Horizontal offset from centre.</param>
        /// <param name="alignment">Horizontal alignment of the hint.</param>
        /// <returns>An <see cref="ImagePlayback"/> handle; dispose it to stop playback.</returns>
        public static ImagePlayback PlayFrames(
            PlayerDisplay display,
            IEnumerable<string> frames,
            float yCoordinate = 400f,
            float fps = 10f,
            bool loop = true,
            float xCoordinate = 0f,
            HintAlignment alignment = HintAlignment.Center)
        {
            if (display == null)
            {
                throw new ArgumentNullException(nameof(display));
            }

            if (frames == null)
            {
                throw new ArgumentNullException(nameof(frames));
            }

            var content = ImageContent.FromFrames(frames, fps, loop);
            return CreatePlayback(display, content, yCoordinate, xCoordinate, alignment);
        }

        /// <summary>
        /// Stops the given playback and removes it from its display.
        /// Equivalent to calling <see cref="ImagePlayback.Dispose"/>.
        /// </summary>
        /// <param name="playback">The playback to stop.  Ignored if <see langword="null"/>.</param>
        public static void Stop(ImagePlayback playback)
        {
            if (playback == null)
            {
                return;
            }

            playback.Dispose();

            if (ActivePlaybacks.TryGetValue(playback.Display, out var list))
            {
                lock (list)
                {
                    list.Remove(playback);
                }
            }
        }

        /// <summary>
        /// Stops and removes all active image animations on the specified display.
        /// </summary>
        /// <param name="display">The display whose image hints should be stopped.  Ignored if <see langword="null"/>.</param>
        public static void StopAll(PlayerDisplay display)
        {
            if (display == null)
            {
                return;
            }

            if (ActivePlaybacks.TryRemove(display, out var list))
            {
                lock (list)
                {
                    foreach (var pb in list)
                    {
                        pb.Dispose();
                    }

                    list.Clear();
                }
            }
        }

        // ------------------------------------------------------------------ //
        // Private helpers                                                     //
        // ------------------------------------------------------------------ //
        private static ImagePlayback CreatePlayback(
            PlayerDisplay display,
            ImageContent content,
            float yCoordinate,
            float xCoordinate,
            HintAlignment alignment)
        {
            var hint = new Hint
            {
                YCoordinate = yCoordinate,
                XCoordinate = xCoordinate,
                Alignment = alignment,

                // FontSize = 0: the renderer bakes the size tag into each frame string directly.
                FontSize = 0,
                Content = content,
            };

            display.AddHint(hint, GroupName);

            var playback = new ImagePlayback(display, hint, content);

            var list = ActivePlaybacks.GetOrAdd(display, _ => new List<ImagePlayback>());
            lock (list)
            {
                list.Add(playback);
            }

            return playback;
        }
    }
}
