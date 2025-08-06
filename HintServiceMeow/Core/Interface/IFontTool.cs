namespace HintServiceMeow.Core.Interface
{
    using HintServiceMeow.Core.Enum;

    internal interface IFontTool
    {
        float GetCharWidth(char c, float fontSize, TextStyle style);
    }
}
