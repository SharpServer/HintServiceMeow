namespace HintServiceMeow.Core.Utilities.Patch
{
    using System;
    using System.Diagnostics;
    using System.Linq.Expressions;
    using System.Reflection;
    using HarmonyLib;
    using Hints;
    using HintServiceMeow.Core.Extension;
    using HintServiceMeow.Core.Utilities.Tools;
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
                {
                    Trace("HintDisplay.Show", "pass-config-disabled", hint, null, null);
                    return true;
                }

                if (hint is not TextHint textHint)
                {
                    Trace("HintDisplay.Show", "pass-non-text", hint, null, null);
                    return true;
                }

                if (!TryGetTargetHub(__instance, out ReferenceHub referenceHub))
                {
                    Trace("HintDisplay.Show", "pass-no-target", hint, null, null);
                    return true;
                }

                (string assemblyName, string sourceKey) = GetExternalCallerInfo();
                string content = TextGetter(textHint) ?? string.Empty;

                if (!CanUseCompatibilityAdapter(assemblyName, content))
                {
                    Trace("HintDisplay.Show", "pass-disabled-assembly", hint, assemblyName, content);
                    return true;
                }

                Trace("HintDisplay.Show", "absorb", hint, assemblyName, content);
                PlayerDisplay.Get(referenceHub).ShowCompatibilityHint(assemblyName, sourceKey, content, textHint.DurationScalar);
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

                Trace("HintDisplay.Show", "original-ran", hint, null, null);

                float restoreDelay = Plugin.Instance.Config.PreferHsmOverVanillaHints
                    ? 0.1f
                    : hint.DurationScalar + 0.05f;

                PlayerDisplay.Get(referenceHub).ForceUpdateAfter(restoreDelay);
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
                {
                    Trace("LabApi.SendHint(string)", "pass-config-disabled", null, null, text);
                    return true;
                }

                (string assemblyName, string sourceKey) = GetExternalCallerInfo();
                if (!CanUseCompatibilityAdapter(assemblyName, text))
                {
                    Trace("LabApi.SendHint(string)", "pass-disabled-assembly", null, assemblyName, text);
                    return true;
                }

                Trace("LabApi.SendHint(string)", "absorb", null, assemblyName, text);
                __instance.GetPlayerDisplay().ShowCompatibilityHint(assemblyName, sourceKey, text, duration);
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
                {
                    Trace("LabApi.SendHint(effects)", "pass-config-disabled", null, null, text);
                    return true;
                }

                (string assemblyName, string sourceKey) = GetExternalCallerInfo();
                if (!CanUseCompatibilityAdapter(assemblyName, text))
                {
                    Trace("LabApi.SendHint(effects)", "pass-disabled-assembly", null, assemblyName, text);
                    return true;
                }

                Trace("LabApi.SendHint(effects)", "absorb", null, assemblyName, text);
                __instance.GetPlayerDisplay().ShowCompatibilityHint(assemblyName, sourceKey, text, duration);
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
                {
                    Trace("Exiled.ShowHint(string)", "pass-config-disabled", null, null, message);
                    return true;
                }

                (string assemblyName, string sourceKey) = GetExternalCallerInfo();
                if (!CanUseCompatibilityAdapter(assemblyName, message))
                {
                    Trace("Exiled.ShowHint(string)", "pass-disabled-assembly", null, assemblyName, message);
                    return true;
                }

                Trace("Exiled.ShowHint(string)", "absorb", null, assemblyName, message);
                __instance.GetPlayerDisplay().ShowCompatibilityHint(assemblyName, sourceKey, message, duration);
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
                {
                    Trace("Exiled.ShowHint(Hint)", "pass-config-disabled", null, null, hint?.Content);
                    return true;
                }

                if (!hint.Show)
                {
                    Trace("Exiled.ShowHint(Hint)", "pass-hidden", null, null, hint.Content);
                    return true;
                }

                (string assemblyName, string sourceKey) = GetExternalCallerInfo();
                if (!CanUseCompatibilityAdapter(assemblyName, hint.Content))
                {
                    Trace("Exiled.ShowHint(Hint)", "pass-disabled-assembly", null, assemblyName, hint.Content);
                    return true;
                }

                Trace("Exiled.ShowHint(Hint)", "absorb", null, assemblyName, hint.Content);
                __instance.GetPlayerDisplay().ShowCompatibilityHint(assemblyName, sourceKey, hint.Content, hint.Duration);
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
            return !Plugin.Instance.Config.DisabledCompatAdapter.Contains(assemblyName);
        }

        private static void Trace(string source, string action, Hint? hint, string? assemblyName, string? content)
        {
            if (!HintTrace.IsEnabled)
                return;

            string hintInfo = hint is null
                ? string.Empty
                : $" type={hint.GetType().Name} duration={hint.DurationScalar:0.###}";
            string assemblyInfo = assemblyName is null ? string.Empty : $" assembly=\"{assemblyName}\"";
            string contentInfo = content is null ? string.Empty : $" {HintTrace.Describe(content)}";

            HintTrace.Log($"{source} {action}{hintInfo}{assemblyInfo}{contentInfo}");
        }

        private static (string AssemblyName, string SourceKey) GetExternalCallerInfo()
        {
            Assembly fallback = Assembly.GetCallingAssembly();
            StackFrame[]? frames = new StackTrace().GetFrames();

            if (frames is null)
            {
                string fallbackName = GetAssemblyName(fallback);
                return (fallbackName, fallbackName);
            }

            foreach (StackFrame frame in frames)
            {
                MethodBase? method = frame.GetMethod();
                Assembly? assembly = method?.DeclaringType?.Assembly ?? method?.Module.Assembly;

                if (assembly is null || ShouldSkipAssemblyFrame(assembly, method))
                    continue;

                string assemblyName = GetAssemblyName(assembly);
                return (assemblyName, BuildSourceKey(assemblyName, method));
            }

            string fallbackAssemblyName = GetAssemblyName(fallback);
            return (fallbackAssemblyName, fallbackAssemblyName);
        }

        private static string BuildSourceKey(string assemblyName, MethodBase? method)
        {
            if (method is null)
                return assemblyName;

            string declaringType = method.DeclaringType?.FullName ?? method.Module.Name;
            string methodName = method.Name;
            string moduleId = GetModuleId(method);
            int metadataToken = GetMetadataToken(method);

            return $"{assemblyName}|{declaringType}.{methodName}|{moduleId}:{metadataToken:X8}";
        }

        private static string GetAssemblyName(Assembly assembly)
        {
            return assembly.FullName ?? assembly.GetName().Name ?? "UnknownAssembly";
        }

        private static string GetModuleId(MethodBase method)
        {
            try
            {
                return method.Module.ModuleVersionId.ToString("N");
            }
            catch
            {
                return "unknown-module";
            }
        }

        private static int GetMetadataToken(MethodBase method)
        {
            try
            {
                return method.MetadataToken;
            }
            catch
            {
                return 0;
            }
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
