namespace HintServiceMeow.Core.Models.Arguments
{
    using HintServiceMeow.Core.Models.Hints;
    using HintServiceMeow.Core.Utilities;

    public class ContentUpdateArg
    {
        internal ContentUpdateArg(AbstractHint hint, PlayerDisplay playerDisplay)
        {
            Hint = hint;
            PlayerDisplay = playerDisplay;
        }

        public AbstractHint Hint { get; }

        public PlayerDisplay PlayerDisplay { get; }
    }
}
