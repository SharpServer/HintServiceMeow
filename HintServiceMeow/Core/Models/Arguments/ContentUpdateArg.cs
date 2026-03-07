namespace HintServiceMeow.Core.Models.Arguments
{
    using HintServiceMeow.Core.Models.Hints;
    using HintServiceMeow.Core.Utilities;

    /// <summary>
    /// Provides contextual data for a content update operation on an <see cref="AbstractHintContent"/>.
    /// </summary>
    public class ContentUpdateArg
    {
        internal ContentUpdateArg(AbstractHint hint, PlayerDisplay playerDisplay)
        {
            Hint = hint;
            PlayerDisplay = playerDisplay;
        }

        /// <summary>
        /// Gets the hint whose content is being updated.
        /// </summary>
        public AbstractHint Hint { get; }

        /// <summary>
        /// Gets the player display associated with this content update.
        /// </summary>
        public PlayerDisplay PlayerDisplay { get; }
    }
}
