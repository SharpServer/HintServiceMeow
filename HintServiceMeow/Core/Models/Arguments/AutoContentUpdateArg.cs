namespace HintServiceMeow.Core.Models.Arguments
{
    using System;

    using HintServiceMeow.Core.Models.Hints;
    using HintServiceMeow.Core.Utilities;

    public class AutoContentUpdateArg
    {
        internal AutoContentUpdateArg(AbstractHint hint, PlayerDisplay playerDisplay, TimeSpan defaultUpdateDelay)
        {
            Hint = hint;
            PlayerDisplay = playerDisplay;
            NextUpdateDelay = defaultUpdateDelay;
            DefaultUpdateDelay = defaultUpdateDelay;
        }

        public AbstractHint Hint { get; }

        public PlayerDisplay PlayerDisplay { get; }

        /// <summary>
        /// Gets or sets the delay before the next update. Count in seconds.
        /// </summary>
        public TimeSpan NextUpdateDelay { get; set; }

        public TimeSpan DefaultUpdateDelay { get; set; }
    }
}
