namespace HintServiceMeow.Core.Extension
{
    using System.Reflection;

    using HintServiceMeow.Core.Models.Hints;
    using HintServiceMeow.Core.Utilities;

    public static class NWPlayerExtension
    {
        public static PlayerDisplay GetPlayerDisplay(this LabApi.Features.Wrappers.Player player) => PlayerDisplay.Get(player);

        public static void AddHint(this LabApi.Features.Wrappers.Player player, AbstractHint hint) => PlayerDisplay.Get(player).InternalAddHint(Assembly.GetCallingAssembly().FullName, hint);

        public static void RemoveHint(this LabApi.Features.Wrappers.Player player, AbstractHint hint) => PlayerDisplay.Get(player).InternalRemoveHint(Assembly.GetCallingAssembly().FullName, hint);
    }
}