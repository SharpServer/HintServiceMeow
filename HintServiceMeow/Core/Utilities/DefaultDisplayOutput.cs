namespace HintServiceMeow.Core.Utilities
{
    using System;

    using HintServiceMeow.Core.Interface;
    using HintServiceMeow.Core.Models.Arguments;
    using HintServiceMeow.Core.Utilities.Tools;

    using Mirror;

    internal class DefaultDisplayOutput : IDisplayOutput
    {
        private readonly NetworkConnection? connectionToPlayer;
        private readonly Hints.HintMessage hintMessageTemplate = new(new Hints.TextHint(string.Empty, [new Hints.StringHintParameter(string.Empty)], [Hints.HintEffectPresets.TrailingPulseAlpha(1, 1, 1)], 99999f));

        public DefaultDisplayOutput(NetworkConnection connectionToPlayer)
        {
            this.connectionToPlayer = connectionToPlayer ?? throw new ArgumentNullException(nameof(connectionToPlayer), "NetworkConnection cannot be null");
        }

        public void ShowHint(DisplayOutputArg ev)
        {
            try
            {
                if (connectionToPlayer is not { isReady: true })
                    return;

                ((Hints.TextHint)hintMessageTemplate.Content).Text = ev.Content;
                connectionToPlayer.Send(hintMessageTemplate);
            }
            catch (Exception ex)
            {
                Logger.Instance.Error(ex);
            }
        }
    }
}
