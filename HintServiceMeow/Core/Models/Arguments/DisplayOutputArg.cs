namespace HintServiceMeow.Core.Models.Arguments
{
    using HintServiceMeow.Core.Utilities;
    using HintParameter = global::Hints.HintParameter;

    /// <summary>
    /// Provides the data passed to a display output when rendering hints for a player.
    /// </summary>
    public class DisplayOutputArg
    {
        internal DisplayOutputArg(PlayerDisplay playerDisplay, string content, HintParameter[]? parameters = null)
        {
            PlayerDisplay = playerDisplay;
            Content = content;
            Parameters = parameters ?? [];
        }

        /// <summary>
        /// Gets the player display that triggered the output.
        /// </summary>
        public PlayerDisplay PlayerDisplay { get; }

        /// <summary>
        /// Gets the formatted hint content string to be rendered.
        /// </summary>
        public string Content { get; }

        /// <summary>
        /// Gets native SCP:SL hint parameters referenced by <see cref="Content"/>.
        /// </summary>
        public HintParameter[] Parameters { get; }
    }
}
