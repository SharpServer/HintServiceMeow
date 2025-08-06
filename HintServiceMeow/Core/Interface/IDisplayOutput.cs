namespace HintServiceMeow.Core.Interface
{
    using HintServiceMeow.Core.Models.Arguments;

    public interface IDisplayOutput
    {
        void ShowHint(DisplayOutputArg ev);
    }
}
