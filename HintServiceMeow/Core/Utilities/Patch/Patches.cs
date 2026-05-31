namespace HintServiceMeow.Core.Utilities.Patch
{
    using System;
    using System.Diagnostics;
    using System.Linq.Expressions;
    using System.Reflection;
    using HarmonyLib;
    using Hints;
    using HintServiceMeow.Core.Extension;
    using HintServiceMeow.Plugin;
    using LabApi.Features.Wrappers;
    using Mirror;
    using Logger = HintServiceMeow.Core.Utilities.Tools.Logger;

    internal static class Patches
    {
        private static readonly Func<TextHint, string> TextGetter = (Func<TextHint, string>)GetTextGetter();

#pragma warning disable SA1313
        public static bool HintDisplayPrefix(Hint hint, HintDisplay __instance)
        {
            try
            {
                if (!Plugin.Instance.Config.UseHintCompatibilityAdapter)
                    return true;

                if (hint is not TextHint textHint)
                    return true;

                if (!TryGetTargetHub(__instance, out ReferenceHub referenceHub))
                    return true;

                string assemblyName = GetExternalCallingAssemblyName();
                string content = TextGetter(textHint) ?? string.Empty;

                if (!CanUseCompatibilityAdapter(assemblyName, content))
                    return true;

                PlayerDisplay.Get(referenceHub).ShowCompatibilityHint(assemblyName, content, textHint.DurationScalar);
                return false;
            }
            catch (Exception ex)
            {
                Logger.Instance.Error(ex);
                return true;
            }
        }

        public static void HintDisplayPostfix(Hint hint, HintDisplay __instance, bool __runOriginal)
        {
            try
            {
                if (!__runOriginal || hint is null)
                    return;

                if (!TryGetTargetHub(__instance, out ReferenceHub referenceHub))
                    return;

                PlayerDisplay.Get(referenceHub).ForceUpdateAfter(hint.DurationScalar + 0.05f);
            }
            catch (Exception ex)
            {
                Logger.Instance.Error(ex);
            }
        }

        public static bool SendHintPatch1(ref string text, ref float duration, ref Player __instance)
        {
            try
            {
                if (!Plugin.Instance.Config.UseHintCompatibilityAdapter)
                    return true;

                string assemblyName = GetExternalCallingAssemblyName();
                if (!CanUseCompatibilityAdapter(assemblyName, text))
                    return true;

                __instance.GetPlayerDisplay().ShowCompatibilityHint(assemblyName, text, duration);
                return false;
            }
            catch (Exception ex)
            {
                Logger.Instance.Error(ex);
                return true;
            }
        }

#pragma warning disable IDE0060 // Remove unused parameter
        public static bool SendHintPatch2(ref string text, ref HintEffect[] effects, ref float duration, ref Player __instance)
        {
            try
            {
                if (!Plugin.Instance.Config.UseHintCompatibilityAdapter)
                    return true;

                string assemblyName = GetExternalCallingAssemblyName();
                if (!CanUseCompatibilityAdapter(assemblyName, text))
                    return true;

                __instance.GetPlayerDisplay().ShowCompatibilityHint(assemblyName, text, duration);
                return false;
            }
            catch (Exception ex)
            {
                Logger.Instance.Error(ex);
                return true;
            }
        }
#pragma warning restore IDE0060 // Remove unused parameter

#if EXILED
        public static bool ExiledHintPatch1(ref string message, ref float duration, ref Exiled.API.Features.Player __instance)
        {
            try
            {
                if (!Plugin.Instance.Config.UseHintCompatibilityAdapter)
                    return true;

                string assemblyName = GetExternalCallingAssemblyName();
                if (!CanUseCompatibilityAdapter(assemblyName, message))
                    return true;

                __instance.GetPlayerDisplay().ShowCompatibilityHint(assemblyName, message, duration);
                return false;
            }
            catch (Exception ex)
            {
                Logger.Instance.Error(ex);
                return true;
            }
        }

        public static bool ExiledHintPatch2(ref Exiled.API.Features.Hint hint, ref Exiled.API.Features.Player __instance)
#pragma warning restore SA1313
        {
            try
            {
                if (!Plugin.Instance.Config.UseHintCompatibilityAdapter)
                    return true;

                if (!hint.Show)
                    return true;

                string assemblyName = GetExternalCallingAssemblyName();
                if (!CanUseCompatibilityAdapter(assemblyName, hint.Content))
                    return true;

                __instance.GetPlayerDisplay().ShowCompatibilityHint(assemblyName, hint.Content, hint.Duration);
                return false;
            }
            catch (Exception ex)
            {
                Logger.Instance.Error(ex);
                return true;
            }
        }
#endif

        private static bool TryGetTargetHub(HintDisplay hintDisplay, out ReferenceHub referenceHub)
        {
            referenceHub = null!;

            if (hintDisplay is null || hintDisplay.isLocalPlayer || !NetworkServer.active)
                return false;

            NetworkConnection connection = hintDisplay.connectionToClient;
            if (connection is null || HintDisplay.SuppressedReceivers.Contains(connection))
                return false;

            return ReferenceHub.TryGetHub(connection, out referenceHub);
        }

        private static bool CanUseCompatibilityAdapter(string assemblyName, string? content)
        {
            return !Plugin.Instance.Config.DisabledCompatAdapter.Contains(assemblyName)
                   && (content?.Length ?? 0) <= ushort.MaxValue;
        }

        private static string GetExternalCallingAssemblyName()
        {
            Assembly fallback = Assembly.GetCallingAssembly();
            StackFrame[]? frames = new StackTrace().GetFrames();

            if (frames is null)
                return fallback.FullName;

            foreach (StackFrame frame in frames)
            {
                MethodBase? method = frame.GetMethod();
                Assembly? assembly = method?.DeclaringType?.Assembly ?? method?.Module.Assembly;

                if (assembly is null || ShouldSkipAssemblyFrame(assembly, method))
                    continue;

                return assembly.FullName;
            }

            return fallback.FullName;
        }

        private static bool ShouldSkipAssemblyFrame(Assembly assembly, MethodBase? method)
        {
            if (assembly == typeof(Patches).Assembly || assembly == typeof(Harmony).Assembly)
                return true;

            Type? declaringType = method?.DeclaringType;
            if (declaringType == typeof(HintDisplay) || declaringType == typeof(Player))
                return true;

#if EXILED
            if (declaringType == typeof(Exiled.API.Features.Player))
                return true;
#endif

            string assemblyName = assembly.GetName().Name ?? string.Empty;
            return assemblyName.StartsWith("System", StringComparison.Ordinal)
                   || assemblyName == "mscorlib"
                   || assemblyName == "netstandard";
        }

        private static Delegate GetTextGetter()
        {
            var prop = typeof(TextHint).GetProperty(
                        "Text",
                        BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);

            if (prop == null)
                throw new MissingMemberException(typeof(TextHint).FullName, "Text");

            var getMethod = prop.GetGetMethod(nonPublic: true);
            if (getMethod == null)
                throw new InvalidOperationException($"Property 'Text' has no getter.");

            var objParam = Expression.Parameter(typeof(TextHint), "obj");
            var call = Expression.Call(objParam, getMethod);
            var body = Expression.Convert(call, typeof(string));

            return Expression.Lambda<Func<TextHint, string>>(body, objParam).Compile();
        }
    }
}
