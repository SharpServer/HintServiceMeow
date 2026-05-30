namespace HintServiceMeow.Core.Utilities.Image
{
    using System;
    using System.Collections.Generic;
    using System.Drawing;
    using System.IO;
    using System.Net;
    using System.Threading;
    using System.Threading.Tasks;
    using DrawingImage = System.Drawing.Image;

    /// <summary>
    /// Public image conversion helpers modelled after the standalone Images plugin API.
    /// </summary>
    public static class ImageApi
    {
        /// <summary>
        /// Converts an image from a file path or URL to SCP:SL rich-text frames.
        /// </summary>
        /// <param name="location">The file path or URL of the image.</param>
        /// <param name="handle">Callback invoked for each frame and once more with <see cref="ImageFrameData.Last"/>.</param>
        /// <param name="isUrl">Whether <paramref name="location"/> is a URL.</param>
        /// <param name="scale">Font-size percentage. Use 0 to auto-calculate.</param>
        /// <param name="shapeCorrection">Whether to horizontally stretch pixels to compensate for block glyph shape.</param>
        /// <param name="waitTime">Seconds to wait after each frame callback. Use 0 for no delay.</param>
        /// <param name="compress">Whether to merge similar adjacent colours to reduce frame size.</param>
        /// <param name="ct">Cancellation token.</param>
        /// <returns>A task that completes after all frames and the final callback are delivered.</returns>
        public static Task LocationToTextAsync(
            string location,
            Action<ImageFrameData> handle,
            bool isUrl = false,
            float scale = 0f,
            bool shapeCorrection = true,
            float waitTime = 0.1f,
            bool compress = true,
            CancellationToken ct = default)
        {
            return isUrl
                ? UrlToTextAsync(location, handle, scale, shapeCorrection, waitTime, compress, ct)
                : FileToTextAsync(location, handle, scale, shapeCorrection, waitTime, compress, ct);
        }

        /// <summary>
        /// Converts an image from a local file path to SCP:SL rich-text frames.
        /// </summary>
        public static Task FileToTextAsync(
            string filePath,
            Action<ImageFrameData> handle,
            float scale = 0f,
            bool shapeCorrection = true,
            float waitTime = 0.1f,
            bool compress = true,
            CancellationToken ct = default)
        {
            if (filePath == null)
                throw new ArgumentNullException(nameof(filePath));

            return ConvertLoadedImageAsync(
                () => LoadFromFile(filePath),
                handle,
                scale,
                shapeCorrection,
                waitTime,
                compress,
                ct);
        }

        /// <summary>
        /// Downloads an image from a URL and converts it to SCP:SL rich-text frames.
        /// </summary>
        public static Task UrlToTextAsync(
            string url,
            Action<ImageFrameData> handle,
            float scale = 0f,
            bool shapeCorrection = true,
            float waitTime = 0.1f,
            bool compress = true,
            CancellationToken ct = default)
        {
            if (url == null)
                throw new ArgumentNullException(nameof(url));

            return ConvertLoadedImageAsync(
                () => LoadFromUrl(url),
                handle,
                scale,
                shapeCorrection,
                waitTime,
                compress,
                ct);
        }

        /// <summary>
        /// Converts an already loaded <see cref="DrawingImage"/> to SCP:SL rich-text frames.
        /// The caller keeps ownership of the supplied image.
        /// </summary>
        public static Task BitmapToTextAsync(
            DrawingImage image,
            Action<ImageFrameData> handle,
            float scale = 0f,
            bool shapeCorrection = true,
            float waitTime = 0.1f,
            bool compress = true,
            CancellationToken ct = default)
        {
            if (image == null)
                throw new ArgumentNullException(nameof(image));

            return Task.Run(
                () => ConvertImage(image, handle, scale, shapeCorrection, waitTime, compress, ct),
                ct);
        }

        /// <summary>
        /// Converts an image location and returns all generated frame strings.
        /// </summary>
        public static async Task<IReadOnlyList<string>> LocationToFramesAsync(
            string location,
            bool isUrl = false,
            float scale = 0f,
            bool shapeCorrection = true,
            bool compress = true,
            CancellationToken ct = default)
        {
            var frames = new List<string>();

            await LocationToTextAsync(
                location,
                frame =>
                {
                    if (!frame.Last && frame.Data != null)
                        frames.Add(frame.Data);
                },
                isUrl,
                scale,
                shapeCorrection,
                waitTime: 0f,
                compress,
                ct).ConfigureAwait(false);

            return frames;
        }

        private static Task ConvertLoadedImageAsync(
            Func<DrawingImage> loadImage,
            Action<ImageFrameData> handle,
            float scale,
            bool shapeCorrection,
            float waitTime,
            bool compress,
            CancellationToken ct)
        {
            if (handle == null)
                throw new ArgumentNullException(nameof(handle));

            return Task.Run(
                () =>
                {
                    using DrawingImage image = loadImage();
                    ConvertImage(image, handle, scale, shapeCorrection, waitTime, compress, ct);
                },
                ct);
        }

        private static void ConvertImage(
            DrawingImage image,
            Action<ImageFrameData> handle,
            float scale,
            bool shapeCorrection,
            float waitTime,
            bool compress,
            CancellationToken ct)
        {
            if (handle == null)
                throw new ArgumentNullException(nameof(handle));

            Exception? completionError = null;
            int waitMilliseconds = waitTime <= 0f ? 0 : (int)Math.Round(waitTime * 1000f);

            ImageFrameRenderer.Render(
                image,
                scale,
                shapeCorrection,
                compress,
                frame =>
                {
                    ct.ThrowIfCancellationRequested();
                    handle(new ImageFrameData(frame));

                    if (waitMilliseconds > 0)
                        Thread.Sleep(waitMilliseconds);
                },
                error => completionError = error,
                ct);

            handle(new ImageFrameData(null)
            {
                Last = true,
                Error = completionError,
            });
        }

        private static DrawingImage LoadFromFile(string path)
        {
            if (!File.Exists(path))
                throw new FileNotFoundException("Image file not found.", path);

            return DrawingImage.FromFile(path);
        }

        private static DrawingImage LoadFromUrl(string url)
        {
#pragma warning disable SYSLIB0014 // WebClient is used for net48 game-plugin compatibility.
            using var client = new WebClient();
            byte[] data = client.DownloadData(url);
#pragma warning restore SYSLIB0014

            var stream = new MemoryStream(data);
            return DrawingImage.FromStream(stream);
        }
    }
}
