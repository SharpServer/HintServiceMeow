namespace HintServiceMeow.Core.Interface
{
    using HintServiceMeow.Core.Models.Arguments;

    /// <summary>
    /// Defines an adaptor for displaying hints through a compatibility layer.
    /// </summary>
    public interface ICompatibilityAdaptor
    {
        /// <summary>
        /// Displays a hint using the provided compatibility adaptor arguments.
        /// </summary>
        /// <param name="ev">The arguments containing hint display data.</param>
        void ShowHint(CompatibilityAdaptorArg ev);
    }
}
