namespace HintServiceMeow.Core.Utilities.Patch
{
    using System;
    using System.Reflection;
    using HarmonyLib;
    using HintServiceMeow.Core.Utilities.Tools;

    /// <summary>
    /// Provides methods to apply and remove Harmony patches used by HintServiceMeow.
    /// </summary>
    public static class Patcher
    {
        private const string HarmonyId = "HintServiceMeowHarmony";

        /// <summary>
        /// Gets the active <see cref="HarmonyLib.Harmony"/> instance used to manage patches, or <see langword="null"/> if patching has not been applied.
        /// </summary>
        public static Harmony? Harmony { get; private set; }

        /// <summary>
        /// Applies all Harmony patches required by HintServiceMeow, including patches for hint display and hint sending methods.
        /// </summary>
        public static void Patch()
        {
            Unpatch();

            Harmony = new Harmony(HarmonyId);
            PatchHintDisplay();
            PatchPrefix(
                typeof(LabApi.Features.Wrappers.Player),
                nameof(LabApi.Features.Wrappers.Player.SendHint),
                [typeof(string), typeof(float)],
                nameof(Patches.SendHintPatch1));
            PatchPrefix(
                typeof(LabApi.Features.Wrappers.Player),
                nameof(LabApi.Features.Wrappers.Player.SendHint),
                [typeof(string), typeof(Hints.HintEffect[]), typeof(float)],
                nameof(Patches.SendHintPatch2));
            PatchPrefix(
                typeof(LabApi.Features.Wrappers.Player),
                nameof(LabApi.Features.Wrappers.Player.SendHint),
                [typeof(string), typeof(Hints.HintParameter[]), typeof(Hints.HintEffect[]), typeof(float)],
                nameof(Patches.SendHintPatch3));

#if EXILED
            PatchPrefix(
                typeof(Exiled.API.Features.Player),
                nameof(Exiled.API.Features.Player.ShowHint),
                [typeof(string), typeof(float)],
                nameof(Patches.ExiledHintPatch1));
            PatchPrefix(
                typeof(Exiled.API.Features.Player),
                nameof(Exiled.API.Features.Player.ShowHint),
                [typeof(string), typeof(Hints.HintEffect[]), typeof(float)],
                nameof(Patches.ExiledHintPatch2));
            PatchPrefix(
                typeof(Exiled.API.Features.Player),
                nameof(Exiled.API.Features.Player.ShowHint),
                [typeof(string), typeof(Hints.HintParameter[]), typeof(Hints.HintEffect[]), typeof(float)],
                nameof(Patches.ExiledHintPatch3));
            PatchPrefix(
                typeof(Exiled.API.Features.Player),
                nameof(Exiled.API.Features.Player.ShowHint),
                [typeof(Exiled.API.Features.Hint)],
                nameof(Patches.ExiledHintPatch4));
#endif
        }

        /// <summary>
        /// Removes all Harmony patches applied by this patcher.
        /// </summary>
        public static void Unpatch()
        {
            try
            {
                (Harmony ?? new Harmony(HarmonyId)).UnpatchAll(HarmonyId);
            }
            catch (Exception ex)
            {
                Logger.Instance.Error($"Failed to unpatch HintServiceMeow compatibility patches: {ex}");
            }

            Harmony = null;
        }

        private static void PatchHintDisplay()
        {
            MethodInfo? hintDisplayMethod = typeof(Hints.HintDisplay).GetMethod(
                nameof(Hints.HintDisplay.Show),
                BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic,
                null,
                [typeof(Hints.Hint)],
                null);
            MethodInfo? prefix = typeof(Patches).GetMethod(nameof(Patches.HintDisplayPrefix));
            MethodInfo? postfix = typeof(Patches).GetMethod(nameof(Patches.HintDisplayPostfix));

            if (hintDisplayMethod is null || prefix is null || postfix is null)
            {
                Logger.Instance.Error("Failed to find HintDisplay.Show compatibility patch target.");
                return;
            }

            TryPatch(() => Harmony!.Patch(hintDisplayMethod, new HarmonyMethod(prefix), new HarmonyMethod(postfix)), hintDisplayMethod);
        }

        private static void PatchPrefix(Type targetType, string methodName, Type[] parameterTypes, string patchMethodName)
        {
            MethodInfo? target = targetType.GetMethod(
                methodName,
                BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic,
                null,
                parameterTypes,
                null);
            MethodInfo? prefix = typeof(Patches).GetMethod(patchMethodName);

            if (target is null || prefix is null)
            {
                Logger.Instance.Debug($"Skipping missing compatibility patch target: {targetType.FullName}.{methodName} -> {patchMethodName}");
                return;
            }

            TryPatch(() => Harmony!.Patch(target, new HarmonyMethod(prefix)), target);
        }

        private static void TryPatch(Action patchAction, MethodInfo target)
        {
            try
            {
                patchAction();
            }
            catch (Exception ex)
            {
                Logger.Instance.Error($"Failed to patch {target.DeclaringType?.FullName}.{target.Name}: {ex}");
            }
        }
    }
}
