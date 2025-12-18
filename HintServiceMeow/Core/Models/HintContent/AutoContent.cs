namespace HintServiceMeow.Core.Models.HintContent
{
    using System;
    using HintServiceMeow.Core.Models.Arguments;
    using HintServiceMeow.Core.Utilities.Tools;

    public class AutoContent : AbstractHintContent
    {
        private DateTime nextUpdateTime;
        private TimeSpan defaultUpdateTime = TimeSpan.FromSeconds(0.1);

        private string? text;

        private TextUpdateHandler? autoText;

        public AutoContent(TextUpdateHandler? autoText)
        {
            this.autoText = autoText;
        }

        public delegate string TextUpdateHandler(AutoContentUpdateArg ev);

        public TextUpdateHandler? AutoText
        {
            get => autoText;
            set
            {
                autoText = value;
                nextUpdateTime = DateTime.MinValue;// Reset Update Time
            }
        }

        public override string? GetText() => text;

        public override void TryUpdate(ContentUpdateArg ev)
        {
            if (nextUpdateTime > DateTime.Now)
                return;

            AutoContentUpdateArg autoContentUpdateArg = new(ev.Hint, ev.PlayerDisplay, defaultUpdateTime);

            try
            {
                string? newText = autoText?.Invoke(autoContentUpdateArg);

                if (text != newText)
                {
                    text = newText;
                    OnUpdated();
                }

                nextUpdateTime = DateTime.Now.Add(autoContentUpdateArg.NextUpdateDelay);
                defaultUpdateTime = autoContentUpdateArg.DefaultUpdateDelay;
            }
            catch (Exception ex)
            {
                text = string.Empty;
                Logger.Instance.Error(ex);
            }
        }
    }
}
