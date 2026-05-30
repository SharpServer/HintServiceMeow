namespace HintServiceMeow.Core.Utilities.Image
{
    using System;
    using System.Drawing;
    using System.Drawing.Imaging;
    using System.Runtime.InteropServices;
    using System.Text;
    using System.Threading;

    /// <summary>
    /// Converts a <see cref="System.Drawing.Image"/> into per-frame SCP:SL-compatible rich-text
    /// strings made of coloured block characters (█).
    ///
    /// Key performance features:
    /// <list type="bullet">
    ///   <item>Uses <c>Bitmap.LockBits</c> + <see cref="Marshal.Copy"/> for O(1) bulk pixel reads,
    ///         dramatically faster than the per-pixel <c>GetPixel()</c> approach.</item>
    ///   <item>Applies progressive colour-compression (merging adjacent similar-hued pixels) to
    ///         keep rich-text payloads within Mirror's network limits.</item>
    ///   <item>Runs synchronously so callers control threading (called from <see cref="ImageRenderCache"/>
    ///         which wraps it in a <c>Task.Run</c>).</item>
    /// </list>
    /// </summary>
    internal static class ImageFrameRenderer
    {
        /// <summary>
        /// Renders all frames of <paramref name="image"/> synchronously, invoking
        /// <paramref name="onFrame"/> for each successfully-rendered frame and
        /// <paramref name="onComplete"/> exactly once when finished.
        ///
        /// Intended to be called inside a <c>Task.Run</c> by <see cref="ImageRenderCache"/>.
        /// </summary>
        /// <param name="image">Source image; may contain multiple frames (e.g. GIF).</param>
        /// <param name="scale">Font size as a percentage (0 = auto-calculate).</param>
        /// <param name="shapeCorrection">Stretch bitmap horizontally to compensate for non-square glyphs.</param>
        /// <param name="compress">Merge adjacent pixels whose colours are within the threshold.</param>
        /// <param name="onFrame">Called with the rich-text string for each rendered frame.</param>
        /// <param name="onComplete">Called once with <see langword="null"/> on success or the exception on error.</param>
        /// <param name="ct">Cancellation token.</param>
        internal static void Render(
            System.Drawing.Image image,
            float scale,
            bool shapeCorrection,
            bool compress,
            Action<string> onFrame,
            Action<Exception?> onComplete,
            CancellationToken ct = default)
        {
            if (image == null)
            {
                onComplete(new ArgumentNullException(nameof(image)));
                return;
            }

            try
            {
                RenderCore(image, scale, shapeCorrection, compress, onFrame, onComplete, ct);
            }
            catch (OperationCanceledException)
            {
                onComplete(null); // Clean stop — not an error.
            }
            catch (Exception ex)
            {
                onComplete(ex);
            }
        }

        // ------------------------------------------------------------------ //
        // Core frame loop                                                     //
        // ------------------------------------------------------------------ //
        private static void RenderCore(
            System.Drawing.Image image,
            float scale,
            bool shapeCorrection,
            bool compress,
            Action<string> onFrame,
            Action<Exception?> onComplete,
            CancellationToken ct)
        {
            var dim = new FrameDimension(image.FrameDimensionsList[0]);
            int frameCount = image.GetFrameCount(dim);
            int droppedFrames = 0;
            float resolvedSize = 0f;

            for (int index = 0; index < frameCount; index++)
            {
                ct.ThrowIfCancellationRequested();

                image.SelectActiveFrame(dim, index);

                int totalPixels = image.Size.Width * image.Size.Height;
                if (totalPixels > ImageRenderSettings.MaxPixels)
                {
                    onComplete(new InvalidOperationException(
                        $"Image is too large ({image.Size.Width}×{image.Size.Height} = {totalPixels} px). " +
                        $"Maximum is {ImageRenderSettings.MaxPixels} px."));
                    return;
                }

                // Calculate scale once (uses average of width+height, same formula as original).
                if (resolvedSize == 0f)
                {
                    float avg = (image.Size.Width + image.Size.Height) / 2f;
                    resolvedSize = scale != 0f
                        ? scale
                        : (float)Math.Floor((-0.47 * (avg > 60 ? 45 : avg)) + 28.72);
                }

                float size = resolvedSize;
                float lineHeight = 100f - size;
                string sizePrefix = $"<size={size}%><line-height={lineHeight}%>";

                using Bitmap bitmap = shapeCorrection
                    ? new Bitmap(
                        image,
                        new Size(
                            (int)(image.Size.Width * (1 + (0.03 * size))),
                            image.Size.Height))
                    : new Bitmap(image);

                string? frameText = TryBuildFrameText(bitmap, sizePrefix, compress);

                if (frameText == null)
                {
                    droppedFrames++;
                    continue;
                }

                ct.ThrowIfCancellationRequested();
                onFrame(frameText);
            }

            // Build completion report.
            Exception? warning = null;
            if (frameCount == 1 && droppedFrames > 0)
            {
                warning = new InvalidOperationException("Image is too complex to display (frame dropped).");
            }
            else if (droppedFrames > 0)
            {
                warning = new InvalidOperationException($"{droppedFrames} frame(s) dropped during rendering.");
            }

            onComplete(warning);
        }

        // ------------------------------------------------------------------ //
        // Per-frame rendering with LockBits fast pixel access                 //
        // ------------------------------------------------------------------ //

        /// <summary>
        /// Attempts to compress the frame until it fits within <see cref="ImageRenderSettings.MaxFrameUtf8Bytes"/>.
        /// Returns <see langword="null"/> if no compression level achieves the target size.
        /// </summary>
        private static string? TryBuildFrameText(Bitmap bitmap, string sizePrefix, bool compress)
        {
            float maxThreshold = ImageRenderSettings.MaxCompressionThreshold;
            float step = ImageRenderSettings.CompressionThresholdStep;

            for (float threshold = 0f; threshold <= maxThreshold; threshold += step)
            {
                string candidate = BuildFrameText(bitmap, sizePrefix, compress, threshold);
                if (Encoding.UTF8.GetByteCount(candidate) <= ImageRenderSettings.MaxFrameUtf8Bytes)
                {
                    return candidate;
                }
            }

            return null;
        }

        /// <summary>
        /// Renders one bitmap frame into a rich-text string.
        ///
        /// Uses <see cref="Bitmap.LockBits"/> to bulk-copy the pixel array into a managed
        /// <c>byte[]</c> via <see cref="Marshal.Copy"/>, then reads BGRA values directly by index.
        /// This avoids the per-pixel lock overhead of <c>GetPixel()</c>, making it orders of
        /// magnitude faster for large images.
        /// </summary>
        private static string BuildFrameText(Bitmap bitmap, string sizePrefix, bool compress, float threshold)
        {
            var rect = new Rectangle(0, 0, bitmap.Width, bitmap.Height);
            BitmapData bmpData = bitmap.LockBits(rect, ImageLockMode.ReadOnly, PixelFormat.Format32bppArgb);

            byte[] pixels;
            int stride;
            try
            {
                stride = Math.Abs(bmpData.Stride);
                pixels = new byte[stride * bitmap.Height];
                Marshal.Copy(bmpData.Scan0, pixels, 0, pixels.Length);
            }
            finally
            {
                bitmap.UnlockBits(bmpData);
            }

            // Build the rich-text string.
            var sb = new StringBuilder(sizePrefix);
            Color pastPixel = Color.Empty;
            bool firstPixel = true;

            for (int row = 0; row < bitmap.Height; row++)
            {
                for (int col = 0; col < bitmap.Width; col++)
                {
                    int offset = (row * stride) + (col * 4);
                    byte b = pixels[offset];
                    byte g = pixels[offset + 1];
                    byte r = pixels[offset + 2];
                    byte a = pixels[offset + 3];
                    Color pixel = Color.FromArgb(a, r, g, b);

                    if (firstPixel)
                    {
                        sb.Append("<color=").Append(ToHex(pixel)).Append(">█");
                        firstPixel = false;
                    }
                    else if (!pixel.Equals(pastPixel))
                    {
                        if (!compress || threshold == 0f)
                        {
                            sb.Append("</color><color=").Append(ToHex(pixel)).Append(">█");
                        }
                        else
                        {
                            float diff = ColourDistance(pixel, pastPixel);
                            if (diff > threshold)
                            {
                                sb.Append("</color><color=").Append(ToHex(pixel)).Append(">█");
                            }
                            else
                            {
                                pixel = pastPixel; // Treat as same colour — no new tag.
                                sb.Append('█');
                            }
                        }
                    }
                    else
                    {
                        sb.Append('█');
                    }

                    pastPixel = pixel;

                    if (col == bitmap.Width - 1)
                    {
                        sb.Append("\\n");
                    }
                }
            }

            // Close any open colour tag.
            string partial = sb.ToString();
            if (!partial.EndsWith("</color>\\n", StringComparison.Ordinal)
                && !partial.EndsWith("</color>", StringComparison.Ordinal))
            {
                sb.Append("</color>");
            }

            sb.Append("</line-height></size>");
            return sb.ToString();
        }

        private static string ToHex(Color c)
            => $"#{c.R:X2}{c.G:X2}{c.B:X2}{c.A:X2}";

        /// <summary>
        /// Perceptual colour distance (original Images plugin formula).
        /// Weights hue, saturation, and brightness differences.
        /// </summary>
        private static float ColourDistance(Color a, Color b)
        {
            float d1 = Math.Abs(a.GetHue() - b.GetHue());
            float d2 = Math.Abs(a.GetSaturation() - b.GetSaturation());
            float d3 = Math.Abs(a.GetBrightness() - b.GetBrightness());

            if (d1 > 180f)
            {
                d1 = 360f - d1;
            }

            return ((d1 * 0.755f) + (d2 * 2f) + (d3 * 0.7f)) / 3f;
        }
    }
}
