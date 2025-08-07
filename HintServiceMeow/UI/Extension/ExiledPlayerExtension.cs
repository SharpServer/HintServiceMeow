namespace HintServiceMeow.UI.Extension
{
    using HintServiceMeow.UI.Utilities;

    #if EXILED
    public static class ExiledPlayerExtension
    {
        public static PlayerUI GetPlayerUi(this Exiled.API.Features.Player player)
        {
            return PlayerUI.Get(player);
        }
    }
    #endif
}
