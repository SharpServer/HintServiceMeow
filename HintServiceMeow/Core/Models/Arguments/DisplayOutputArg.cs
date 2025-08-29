namespace HintServiceMeow.Core.Models.Arguments
{
    using HintServiceMeow.Core.Utilities;

    public class DisplayOutputArg
    {
        internal DisplayOutputArg(PlayerDisplay playerDisplay, string content)
        {
            PlayerDisplay = playerDisplay;
            Content = content;
        }

        public PlayerDisplay PlayerDisplay { get; }

        public string Content { get; }
    }
}
