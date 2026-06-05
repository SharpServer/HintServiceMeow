namespace HintServiceMeow.Core.Interface
{
    using HintServiceMeow.Core.Models;

    /// <summary>
    /// Defines a parser that converts a <see cref="HintCollection"/> into a displayable message string.
    /// </summary>
    public interface IHintParser
    {
        /// <summary>
        /// Parses the specified hint collection into a formatted message string.
        /// </summary>
        /// <param name="collection">The collection of hints to parse.</param>
        /// <param name="aspectRatio">The player's screen aspect ratio used for resolution-based horizontal alignment.</param>
        /// <returns>A formatted string representing the hints for display.</returns>
        string ParseToMessage(HintCollection collection, float aspectRatio = 1.777777f);
    }
}
