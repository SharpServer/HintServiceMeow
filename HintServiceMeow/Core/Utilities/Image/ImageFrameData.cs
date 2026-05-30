namespace HintServiceMeow.Core.Utilities.Image
{
    using System;

    /// <summary>
    /// Describes one frame produced by the image rich-text converter.
    /// </summary>
    public sealed class ImageFrameData
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ImageFrameData"/> class.
        /// </summary>
        /// <param name="data">The generated rich-text frame, or <see langword="null"/> for a completion event.</param>
        public ImageFrameData(string? data)
        {
            Data = data;
        }

        /// <summary>
        /// Gets the generated rich-text frame. This is <see langword="null"/> when <see cref="Last"/> is true.
        /// </summary>
        public string? Data { get; }

        /// <summary>
        /// Gets or sets a value indicating whether this callback marks the end of conversion.
        /// </summary>
        public bool Last { get; set; }

        /// <summary>
        /// Gets or sets the conversion error or warning, if any.
        /// </summary>
        public Exception? Error { get; set; }
    }
}
