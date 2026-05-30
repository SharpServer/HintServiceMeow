namespace HintServiceMeow.Core.Utilities.Image
{
    using System;

    /// <summary>
    /// Represents a single active image animation on a player's display.
    /// Call <see cref="Dispose"/> (or use a <c>using</c> block) to stop playback and remove the
    /// hint from the display.
    /// </summary>
    public sealed class ImagePlayback : IDisposable
    {
        private bool disposed = false;

        internal ImagePlayback(PlayerDisplay display, Models.Hints.Hint hint, Models.HintContent.ImageContent content)
        {
            Display = display;
            Hint = hint;
            Content = content;
        }

        // ------------------------------------------------------------------ //
        // Properties                                                          //
        // ------------------------------------------------------------------ //

        /// <summary>Gets the player display this playback is attached to.</summary>
        public PlayerDisplay Display { get; }

        /// <summary>Gets the <see cref="Models.Hints.Hint"/> used to render image frames.</summary>
        public Models.Hints.Hint Hint { get; }

        /// <summary>Gets the <see cref="Models.HintContent.ImageContent"/> driving this playback.</summary>
        public Models.HintContent.ImageContent Content { get; }

        /// <summary>Gets whether the renderer has finished loading all frames.</summary>
        public bool IsRenderComplete => Content.IsRenderComplete;

        /// <summary>Gets the render error, if any.</summary>
        public Exception? RenderError => Content.RenderError;

        // ------------------------------------------------------------------ //
        // Methods                                                             //
        // ------------------------------------------------------------------ //

        /// <summary>
        /// Stops playback and removes the image hint from the player's display.
        /// The shared <see cref="ImageRenderCache"/> is not affected — cached frames remain
        /// available for other players.
        /// </summary>
        public void Dispose()
        {
            if (disposed)
            {
                return;
            }

            disposed = true;

            Content.Stop();
            Display.RemoveHint(Hint, ImageHintPlayer.GroupName);
        }
    }
}
