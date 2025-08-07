namespace HintServiceMeow.UI.Extension
{
    using HintServiceMeow.UI.Utilities;

    public static class NWPlayerExtension
    {
        public static PlayerUI GetPlayerUi(this LabApi.Features.Wrappers.Player player)
        {
            return PlayerUI.Get(player.ReferenceHub);
        }
    }
}