namespace HintServiceMeow.Core.Utilities.Image
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Net;
    using System.Threading;
    using System.Threading.Tasks;

    /// <summary>
    /// Global LRU-style render cache.  Maps a <see cref="CacheKey"/> to a <see cref="CacheEntry"/>
    /// and guarantees that the same image + parameters are rendered at most once, regardless of
    /// how many <see cref="HintServiceMeow.Core.Models.HintContent.ImageContent"/> instances request it.
    /// </summary>
    internal static class ImageRenderCache
    {
        // Insertion-ordered dictionary for simple FIFO eviction.
        private static readonly Dictionary<CacheKey, CacheEntry> Cache
            = new Dictionary<CacheKey, CacheEntry>();

        private static readonly object CacheLock = new object();

        // ------------------------------------------------------------------ //
        // Properties                                                          //
        // ------------------------------------------------------------------ //

        /// <summary>Gets or sets the maximum number of distinct images kept in memory.  Default: 30.</summary>
        internal static int MaxEntries { get; set; } = 30;

        // ------------------------------------------------------------------ //
        // Public methods                                                      //
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
                    return existing;
                }

                if (Cache.Count >= MaxEntries)
                {
                    EvictOldest();
                }

                var entry = new CacheEntry();
                Cache[key] = entry;

                // Task.Run returns immediately; actual I/O and rendering run on a thread-pool thread.
                entry.RenderTask = Task.Run(() => StartRender(key, entry, ct), ct);

                return entry;
            }
        }

        /// <summary>Removes all cached entries.</summary>
        internal static void Clear()
        {
            lock (CacheLock)
            {
                Cache.Clear();
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
                img = key.IsUrl ? LoadFromUrl(key.Location) : LoadFromFile(key.Location);
            }
            catch (Exception ex)
            {
                entry.Complete(ex);
                return;
            }

            using (img)
            {
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
    }
}
