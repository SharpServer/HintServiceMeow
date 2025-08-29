namespace HintServiceMeow.Core.Interface
{
    using HintServiceMeow.Core.Models.Arguments;

    public interface ICompatibilityAdaptor
    {
        void ShowHint(CompatibilityAdaptorArg ev);
    }
}
