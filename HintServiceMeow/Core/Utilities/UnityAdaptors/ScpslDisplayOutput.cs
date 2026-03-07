namespace HintServiceMeow.Core.Utilities.UnityAdaptors
{
    using System;
    using HintServiceMeow.Core.Interface;
    using HintServiceMeow.Core.Models.Arguments;
    using HintServiceMeow.Core.Utilities.Tools;
    using Mirror;

    internal class ScpslDisplayOutput(NetworkConnection connectionToPlayer) : IDisplayOutput
    {
        private readonly NetworkConnection? connectionToPlayer = connectionToPlayer;

        public void ShowHint(DisplayOutputArg ev)
        {
            try
            {
                if (connectionToPlayer is not { isReady: true })
                    return;

                Hints.HintMessage hintMessageTemplate = new(new Hints.TextHint(ev.Content, [new Hints.StringHintParameter(string.Empty)], [new Hints.AlphaEffect(1)], 99999f));
                connectionToPlayer.Send(hintMessageTemplate);
            }
            catch (Exception ex)
            {
                Logger.Instance.Error(ex);
            }
        }
    }
}
