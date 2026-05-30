namespace HintServiceMeow.Core.Utilities.Image
{
    using System;

    /// <summary>
    /// Identifies a unique render configuration.
    /// Two requests with the same location, URL flag, and render parameters share the same
    /// <see cref="CacheEntry"/> in <see cref="ImageRenderCache"/>.
    /// </summary>
    internal sealed class CacheKey : IEquatable<CacheKey>
    {
        internal CacheKey(string location, bool isUrl, float scale, bool shapeCorrection, bool compress)
        {
            Location = location;
            IsUrl = isUrl;
            Scale = scale;
            ShapeCorrection = shapeCorrection;
            Compress = compress;
        }

        internal string Location { get; }

        internal bool IsUrl { get; }

        internal float Scale { get; }

        internal bool ShapeCorrection { get; }

        internal bool Compress { get; }

        /// <inheritdoc/>
        public bool Equals(CacheKey? other)
        {
            if (other is null)
            {
                return false;
            }

            return Location == other.Location
                && IsUrl == other.IsUrl
                && Math.Abs(Scale - other.Scale) < 0.001f
                && ShapeCorrection == other.ShapeCorrection
                && Compress == other.Compress;
        }

        /// <inheritdoc/>
        public override bool Equals(object? obj) => Equals(obj as CacheKey);

        /// <inheritdoc/>
        public override int GetHashCode()
        {
            // Round scale to 1 dp to keep equal keys equal under float imprecision.
            int scaleHash = ((int)Math.Round(Scale, 1)).GetHashCode();
            return (Location, IsUrl, scaleHash, ShapeCorrection, Compress).GetHashCode();
        }
    }
}
