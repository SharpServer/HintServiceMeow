namespace HintServiceMeow.Core.Interface
{
    using HintServiceMeow.Core.Models;

    public interface IHintParser
    {
        string ParseToMessage(HintCollection collection);
    }
}
