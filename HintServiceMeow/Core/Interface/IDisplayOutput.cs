namespace HintServiceMeow.Core.Interface
{
    using HintServiceMeow.Core.Models.Arguments;

    /// <summary>
    /// Defines a display output target that can render hint messages to a player.
    /// </summary>
    public interface IDisplayOutput
    {
        /// <summary>
        /// Displays the hint described by the specified output arguments.
        /// </summary>
        /// <param name="ev">The arguments containing the hint content and display settings.</param>
        void ShowHint(DisplayOutputArg ev);
    }
}
