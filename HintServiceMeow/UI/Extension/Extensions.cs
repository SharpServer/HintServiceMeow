namespace HintServiceMeow.UI.Extension
{
    using HintServiceMeow.UI.Utilities;

    public static class Extensions
    {
        #if EXILED
        public static PlayerUI GetPlayerUi(this Exiled.API.Features.Player player)
        {
            return PlayerUI.Get(player);
        }
        #endif

        public static PlayerUI GetPlayerUi(this LabApi.Features.Wrappers.Player player)
        {
            return PlayerUI.Get(player.ReferenceHub);
        }
    }
}
