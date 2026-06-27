namespace HintServiceMeow.Core.Models
{
    using HintParameter = global::Hints.HintParameter;

    internal sealed class ParsedHintMessage(string content, HintParameter[]? parameters = null)
    {
        public string Content { get; } = content;

        public HintParameter[] Parameters { get; } = parameters ?? [];
    }
}
