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

                Hints.HintMessage hintMessage = ChunkedHintMessageFactory.Create(ev.Content);
                connectionToPlayer.Send(hintMessage);
            }
            catch (Exception ex)
            {
                Logger.Instance.Error(ex);
            }
        }
    }
}
