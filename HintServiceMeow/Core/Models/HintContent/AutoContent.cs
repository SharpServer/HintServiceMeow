namespace HintServiceMeow.Core.Models.HintContent
{
    using System;
    using HintServiceMeow.Core.Models.Arguments;
    using HintServiceMeow.Core.Utilities.Tools;

    public class AutoContent : AbstractHintContent
    {
        private DateTime nextUpdateTime;
        private TimeSpan defaultUpdateInterval;

        private string? text;

        private TextUpdateHandler? autoText;

        public AutoContent(TextUpdateHandler? autoText, float defaultUpdateInterval = -1)
        {
            this.autoText = autoText;
            if (defaultUpdateInterval >= 0)
                this.defaultUpdateInterval = TimeSpan.FromSeconds(defaultUpdateInterval);
            else
                this.defaultUpdateInterval = TimeSpan.FromSeconds(0.1);
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

            AutoContentUpdateArg autoContentUpdateArg = new(ev.Hint, ev.PlayerDisplay, defaultUpdateInterval);
            string? newText;

            try
            {
                newText = autoText?.Invoke(autoContentUpdateArg);
            }
            catch (Exception ex)
            {
                newText = string.Empty;
                Logger.Instance.Error(ex);
            }

            if (text != newText)
            {
                text = newText;
                OnUpdated();
            }

            nextUpdateTime = DateTime.Now.Add(autoContentUpdateArg.NextUpdateDelay);
            defaultUpdateInterval = autoContentUpdateArg.DefaultUpdateDelay;
        }
    }
}
