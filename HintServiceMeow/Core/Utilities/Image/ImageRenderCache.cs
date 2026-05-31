namespace HintServiceMeow.Core.Utilities.Image
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Net;
    using System.Threading;
    using System.Threading.Tasks;
    using HintServiceMeow.Core.Utilities.Tools;

    /// <summary>
    /// Global LRU-style render cache.  Maps a <see cref="CacheKey"/> to a <see cref="CacheEntry"/>
    /// and guarantees that the same image + parameters are rendered at most once, regardless of
    /// how many <see cref="HintServiceMeow.Core.Models.HintContent.ImageContent"/> instances request it.
    /// </summary>
    public static class ImageRenderCache
    {
        // Insertion-ordered dictionary for simple FIFO eviction.
        private static readonly Dictionary<CacheKey, CacheEntry> Cache
            = new Dictionary<CacheKey, CacheEntry>();

        private static readonly object CacheLock = new object();

        // ------------------------------------------------------------------ //
        // Properties                                                          //
        // ------------------------------------------------------------------ //

        /// <summary>Gets or sets the maximum number of distinct images kept in memory.  Default: 30.</summary>
        public static int MaxEntries { get; set; } = 30;

        // ------------------------------------------------------------------ //
        // Public methods                                                      //
        // ------------------------------------------------------------------ //

        /// <summary>Removes all cached entries.</summary>
        public static void Clear()
        {
            lock (CacheLock)
            {
                Cache.Clear();
            }
        }

        /// <summary>
        /// Starts rendering a file-backed image into the shared cache.
        /// </summary>
        public static void PreloadFile(
            string filePath,
            float scale = 0f,
            bool shapeCorrection = true,
            bool compress = true,
            CancellationToken ct = default)
        {
            if (filePath == null)
            {
                throw new ArgumentNullException(nameof(filePath));
            }

            GetOrCreate(new CacheKey(filePath, isUrl: false, scale, shapeCorrection, compress), ct);
        }

        /// <summary>
        /// Starts downloading and rendering a URL-backed image into the shared cache.
        /// </summary>
        public static void PreloadUrl(
            string url,
            float scale = 0f,
            bool shapeCorrection = true,
            bool compress = true,
            CancellationToken ct = default)
        {
            if (url == null)
            {
                throw new ArgumentNullException(nameof(url));
            }

            GetOrCreate(new CacheKey(url, isUrl: true, scale, shapeCorrection, compress), ct);
        }

        // ------------------------------------------------------------------ //
        // Internal methods                                                    //
        // ------------------------------------------------------------------ //

        /// <summary>
        /// Returns the <see cref="CacheEntry"/> for <paramref name="key"/>, starting a background
        /// render task if this is the first request for that image.
        /// </summary>
        internal static CacheEntry GetOrCreate(CacheKey key, CancellationToken ct = default)
        {
            lock (CacheLock)
            {
                if (Cache.TryGetValue(key, out var existing))
                {
                    if (HintTrace.IsEnabled)
                        HintTrace.Log($"image-cache hit {Describe(key)} frames={existing.FrameCount} complete={existing.IsComplete}");

                    return existing;
                }

                if (Cache.Count >= MaxEntries)
                {
                    EvictOldest();
                }

                var entry = new CacheEntry();
                Cache[key] = entry;

                if (HintTrace.IsEnabled)
                    HintTrace.Log($"image-cache miss {Describe(key)}");

                // Task.Run returns immediately; actual I/O and rendering run on a thread-pool thread.
                entry.RenderTask = Task.Run(() => StartRender(key, entry, ct), ct);

                return entry;
            }
        }

        // ------------------------------------------------------------------ //
        // Private helpers                                                     //
        // ------------------------------------------------------------------ //
        private static void StartRender(CacheKey key, CacheEntry entry, CancellationToken ct)
        {
            System.Drawing.Image? img = null;

            try
            {
                if (HintTrace.IsEnabled)
                    HintTrace.Log($"image-render load-start {Describe(key)}");

                img = key.IsUrl ? LoadFromUrl(key.Location) : LoadFromFile(key.Location);
            }
            catch (Exception ex)
            {
                if (HintTrace.IsEnabled)
                    HintTrace.Log($"image-render load-error {Describe(key)} error=\"{ex.Message}\"");

                entry.Complete(ex);
                return;
            }

            using (img)
            {
                if (HintTrace.IsEnabled)
                    HintTrace.Log($"image-render start {Describe(key)} size={img.Width}x{img.Height}");

                ImageFrameRenderer.Render(
                    img,
                    key.Scale,
                    key.ShapeCorrection,
                    key.Compress,
                    entry.AddFrame,
                    entry.Complete,
                    ct);
            }
        }

        private static System.Drawing.Image LoadFromFile(string path)
        {
            if (!File.Exists(path))
            {
                throw new FileNotFoundException("Image file not found.", path);
            }

            return System.Drawing.Image.FromFile(path);
        }

        private static System.Drawing.Image LoadFromUrl(string url)
        {
#pragma warning disable SYSLIB0014 // WebClient — acceptable for server-side game plugins
            using var client = new WebClient();
            byte[] data = client.DownloadData(url);
#pragma warning restore SYSLIB0014

            // Do NOT dispose this stream: Image.FromStream keeps the stream open for multi-frame GIFs.
            var stream = new MemoryStream(data);
            return System.Drawing.Image.FromStream(stream);
        }

        private static void EvictOldest()
        {
            // Dictionary preserves insertion order in modern .NET; Keys.First() is the oldest entry.
            if (Cache.Count > 0)
            {
                Cache.Remove(Cache.Keys.First());
            }
        }

        private static string Describe(CacheKey key)
            => $"location=\"{key.Location}\" isUrl={key.IsUrl} scale={key.Scale:0.###} shape={key.ShapeCorrection} compress={key.Compress}";
    }
}
