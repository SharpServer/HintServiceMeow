namespace HintServiceMeow.Core.Utilities.Image
{
    using System;

    /// <summary>
    /// Controls image-to-rich-text rendering quality and safety limits.
    /// </summary>
    public static class ImageRenderSettings
    {
        private static int maxPixels = 40000;
        private static int maxFrameUtf8Bytes = 240000;
        private static float compressionThresholdStep = 0.5f;
        private static float maxCompressionThreshold = 5f;

        /// <summary>
        /// Gets or sets the maximum source pixels accepted per image frame.
        /// </summary>
        public static int MaxPixels
        {
            get => maxPixels;
            set => maxPixels = Math.Max(1, value);
        }

        /// <summary>
        /// Gets or sets the maximum UTF-8 bytes allowed per rendered frame before more compression is attempted.
        /// </summary>
        public static int MaxFrameUtf8Bytes
        {
            get => maxFrameUtf8Bytes;
            set => maxFrameUtf8Bytes = Math.Max(1024, value);
        }

        /// <summary>
        /// Gets or sets the colour-distance increment used while searching for a fitting compressed frame.
        /// </summary>
        public static float CompressionThresholdStep
        {
            get => compressionThresholdStep;
            set => compressionThresholdStep = Math.Max(0.05f, value);
        }

        /// <summary>
        /// Gets or sets the highest colour-distance threshold the renderer may use.
        /// Lower values preserve detail; higher values fit harder images by merging more colours.
        /// </summary>
        public static float MaxCompressionThreshold
        {
            get => maxCompressionThreshold;
            set => maxCompressionThreshold = Math.Max(0f, value);
        }
    }
}
