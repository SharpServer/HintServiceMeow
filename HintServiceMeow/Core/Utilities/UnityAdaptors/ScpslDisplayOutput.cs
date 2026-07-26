namespace HintServiceMeow.Core.Utilities.UnityAdaptors
{
    using System;
    using CentralAuth;
    using HintServiceMeow.Core.Interface;
    using HintServiceMeow.Core.Models.Arguments;
    using HintServiceMeow.Core.Utilities.Tools;

    internal class ScpslDisplayOutput(ReferenceHub referenceHub) : IDisplayOutput
    {
        private readonly ReferenceHub? referenceHub = referenceHub;

        public void ShowHint(DisplayOutputArg ev)
        {
            try
            {
                if (referenceHub is not { Mode: ClientInstanceMode.ReadyClient } ||
                    referenceHub.connectionToClient is not { isReady: true } connectionToPlayer)
                {
                    if (HintTrace.IsEnabled)
                        HintTrace.Log("output skip-not-verified-or-ready");

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
