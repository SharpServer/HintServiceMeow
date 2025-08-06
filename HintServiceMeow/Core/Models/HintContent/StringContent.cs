namespace HintServiceMeow.Core.Models.HintContent
{
    using HintServiceMeow.Core.Models.Arguments;

    public class StringContent : AbstractHintContent
    {
        private string? text = string.Empty;

        public StringContent(string? content)
        {
            Text = content;
        }

        public string? Text
        {
            get => text;
            set
            {
                if (text == value)
                    return;

                text = value;

                OnUpdated();
            }
        }

        public override string? GetText() => Text;

        public override void TryUpdate(ContentUpdateArg ev)
        {
        }
    }
}
