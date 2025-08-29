namespace HintServiceMeow.Core.Models.Arguments
{
    using HintServiceMeow.Core.Utilities;

    /// <summary>
    /// Argument for UpdateAvailable Event.
    /// </summary>
    public class UpdateAvailableEventArg
    {
        internal UpdateAvailableEventArg(PlayerDisplay playerDisplay)
        {
            PlayerDisplay = playerDisplay;
        }

        public PlayerDisplay PlayerDisplay { get; set; }
    }
}
