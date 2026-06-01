namespace HintServiceMeow.Core.Utilities.Patch
{
    using System;
    using System.Diagnostics;
    using System.Globalization;
    using System.Linq;
    using System.Reflection;
    using Hints;
    using HintServiceMeow.Core.Utilities.Tools;
    using HintServiceMeow.Plugin;
    using Mirror;
    using Logger = HintServiceMeow.Core.Utilities.Tools.Logger;

    internal static class HintCompatibilityPipeline
    {
        private const float PreferHsmRestoreDelay = 0.1f;
        private const float VanillaRestorePadding = 0.05f;

        private static readonly Func<TextHint, string> TextGetter = (Func<TextHint, string>)GetTextGetter();

        internal static bool TryAbsorbHintDisplay(Hint hint, HintDisplay hintDisplay)
        {
            if (hint is not TextHint textHint)
            {
                Trace("HintDisplay.Show", "pass-non-text", hint, null, null);
                return false;
            }

            if (!TryGetTargetHub(hintDisplay, out ReferenceHub referenceHub))
            {
                Trace("HintDisplay.Show", "pass-no-target", hint, null, null);
                return false;
            }

            string rawText = TextGetter(textHint) ?? string.Empty;
            float duration = textHint.DurationScalar;

            return TryAbsorb(referenceHub, "HintDisplay.Show", hint, rawText, () => RenderText(rawText, textHint.Parameters), duration);
        }

        internal static bool TryAbsorbWrapperHint(ReferenceHub? referenceHub, string source, string? text, HintParameter[]? parameters, float duration)
        {
            if (referenceHub is null)
            {
                Trace(source, "pass-no-target", null, null, text);
                return false;
            }

            string rawText = text ?? string.Empty;
            return TryAbsorb(referenceHub, source, null, rawText, () => RenderText(rawText, parameters), duration);
        }

        internal static void AfterOriginalHint(Hint hint, HintDisplay hintDisplay, bool runOriginal)
        {
            if (!runOriginal || hint is null)
                return;

            if (!TryGetTargetHub(hintDisplay, out ReferenceHub referenceHub))
                return;

            if (!PlayerDisplay.TryGet(referenceHub, out PlayerDisplay playerDisplay))
            {
                Trace("HintDisplay.Show", "original-ran-no-hsm-display", hint, null, null);
                return;
            }

            if (IsClearRequest(hint))
            {
                Trace("HintDisplay.Show", "original-clear", hint, null, null);
                playerDisplay.ResumeExternalHintImmediately();
                return;
            }

            if (!playerDisplay.ShouldCoordinateExternalHint)
            {
                Trace("HintDisplay.Show", "original-ran-no-hsm-content", hint, null, null);
                return;
            }

            float restoreDelay = ShouldPreferHsmOverVanillaHints()
                ? PreferHsmRestoreDelay
                : NormalizeRestoreDelay(hint.DurationScalar);

            Trace("HintDisplay.Show", "original-ran", hint, null, null, $"restore={restoreDelay:0.###}s");
            playerDisplay.ForceUpdateAfter(restoreDelay);
        }

        internal static void Trace(string source, string action, Hint? hint, string? assemblyName, string? content, string? extra = null)
        {
            if (!HintTrace.IsEnabled)
                return;

            string hintInfo = hint is null
                ? string.Empty
                : $" type={hint.GetType().Name} duration={hint.DurationScalar:0.###}";
            string assemblyInfo = assemblyName is null ? string.Empty : $" assembly=\"{assemblyName}\"";
            string contentInfo = content is null ? string.Empty : $" {HintTrace.Describe(content)}";
            string extraInfo = extra is null ? string.Empty : " " + extra;

            HintTrace.Log($"{source} {action}{hintInfo}{assemblyInfo}{contentInfo}{extraInfo}");
        }

        private static bool TryAbsorb(ReferenceHub referenceHub, string source, Hint? hint, string rawContent, Func<string> contentFactory, float duration)
        {
            if (!IsAdapterEnabled())
            {
                Trace(source, "pass-config-disabled", hint, null, rawContent);
                return false;
            }

            if (!TryGetExternalCallingAssemblyName(out string assemblyName))
            {
                Trace(source, "pass-no-external-assembly", hint, null, rawContent);
                return false;
            }

            if (CompatibilityAssembly.IsFrameworkAssemblyName(CompatibilityAssembly.GetSimpleName(assemblyName)))
            {
                Trace(source, "pass-framework-assembly", hint, assemblyName, rawContent);
                return false;
            }

            CompatibilityAdaptor.RegisterAssembly(assemblyName);

            if (CompatibilityAssembly.IsDisabled(assemblyName))
            {
                Trace(source, "pass-disabled-assembly", hint, assemblyName, rawContent);
                return false;
            }

            string content = contentFactory();
            bool isClearRequest = IsClearRequest(content, duration);
            PlayerDisplay playerDisplay;
            if (isClearRequest)
            {
                if (!PlayerDisplay.TryGet(referenceHub, out playerDisplay))
                {
                    Trace(source, "pass-clear-no-hsm-display", hint, assemblyName, content);
                    return false;
                }
            }
            else
            {
                playerDisplay = PlayerDisplay.Get(referenceHub);
            }

            Trace(source, isClearRequest ? "clear" : "absorb", hint, assemblyName, content);
            playerDisplay.ShowCompatibilityHint(assemblyName, content, duration);
            return true;
        }

        private static bool IsAdapterEnabled()
        {
            try
            {
                return Plugin.Instance?.Config?.UseHintCompatibilityAdapter ?? false;
            }
            catch
            {
                return false;
            }
        }

        private static bool ShouldPreferHsmOverVanillaHints()
        {
            try
            {
                return Plugin.Instance?.Config?.PreferHsmOverVanillaHints ?? false;
            }
            catch
            {
                return false;
            }
        }

        private static float NormalizeRestoreDelay(float duration)
        {
            if (float.IsNaN(duration) || duration <= 0f)
                return 0f;

            if (float.IsInfinity(duration))
                return CompatibilityAdaptor.MaxCompatibilityDuration;

            return Math.Min(duration, CompatibilityAdaptor.MaxCompatibilityDuration) + VanillaRestorePadding;
        }

        private static bool IsClearRequest(Hint hint)
        {
            if (hint is null)
                return false;

            if (float.IsNaN(hint.DurationScalar) || hint.DurationScalar <= 0f)
                return true;

            if (hint is not TextHint textHint)
                return false;

            string rawText = TextGetter(textHint) ?? string.Empty;
            return string.IsNullOrEmpty(rawText) || string.IsNullOrEmpty(RenderText(rawText, textHint.Parameters));
        }

        private static bool IsClearRequest(string content, float duration)
        {
            return float.IsNaN(duration) || duration <= 0f || string.IsNullOrEmpty(content);
        }

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

        private static string RenderText(string? text, HintParameter[]? parameters)
        {
            text ??= string.Empty;

            if (parameters is null || parameters.Length == 0)
                return text;

            object[] formattedParameters = parameters
                .Select(GetFormattedParameter)
                .Cast<object>()
                .ToArray();

            try
            {
                return string.Format(CultureInfo.InvariantCulture, text, formattedParameters);
            }
            catch (FormatException)
            {
                return text;
            }
        }

        private static string GetFormattedParameter(HintParameter? parameter)
        {
            if (parameter is null)
                return string.Empty;

            try
            {
                if (parameter.Formatted is null)
                    parameter.Update(0f);

                if (parameter.Formatted is not null)
                    return parameter.Formatted;
            }
            catch (Exception ex)
            {
                Logger.Instance.Debug($"Failed to update hint parameter '{parameter.GetType().FullName}': {ex}");
            }

            try
            {
                PropertyInfo? valueProperty = parameter.GetType().GetProperty(
                    "Value",
                    BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);

                object? value = valueProperty?.GetValue(parameter);
                return value?.ToString() ?? string.Empty;
            }
            catch
            {
                return string.Empty;
            }
        }

        private static bool TryGetExternalCallingAssemblyName(out string assemblyName)
        {
            assemblyName = CompatibilityAssembly.UnknownAssemblyName;

            StackFrame[]? frames = new StackTrace().GetFrames();
            if (frames is null)
                return false;

            foreach (StackFrame frame in frames)
            {
                MethodBase? method = frame.GetMethod();
                Assembly? assembly = method?.DeclaringType?.Assembly ?? method?.Module.Assembly;

                if (assembly is null || ShouldSkipAssemblyFrame(assembly, method))
                    continue;

                assemblyName = assembly.FullName;
                return true;
            }

            return false;
        }

        private static bool ShouldSkipAssemblyFrame(Assembly assembly, MethodBase? method)
        {
            if (CompatibilityAssembly.IsFrameworkAssembly(assembly))
                return true;

            Type? declaringType = method?.DeclaringType;
            if (declaringType == typeof(HintDisplay) || declaringType == typeof(LabApi.Features.Wrappers.Player))
                return true;

#if EXILED
            if (declaringType == typeof(Exiled.API.Features.Player))
                return true;
#endif

            return false;
        }

        private static Delegate GetTextGetter()
        {
            PropertyInfo? prop = typeof(TextHint).GetProperty(
                "Text",
                BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);

            if (prop is null)
                throw new MissingMemberException(typeof(TextHint).FullName, "Text");

            MethodInfo? getMethod = prop.GetGetMethod(nonPublic: true);
            if (getMethod is null)
                throw new InvalidOperationException("Property 'Text' has no getter.");

            return (Func<TextHint, string>)Delegate.CreateDelegate(typeof(Func<TextHint, string>), getMethod);
        }
    }
}
