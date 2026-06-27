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
                {
                    if (HintTrace.IsEnabled)
                        HintTrace.Log("output skip-not-ready");

                    return;
                }

                if (HintTrace.IsEnabled)
                    HintTrace.Log($"output send-to-client params={ev.Parameters.Length} {HintTrace.Describe(ev.Content)}");

                ChunkedHintMessageFactory.Send(connectionToPlayer, ev.Content, ev.Parameters);
            }
            catch (Exception ex)
            {
                Logger.Instance.Error(ex);
            }
        }
    }
}
